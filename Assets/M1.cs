using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using vCatchStation;

public class M1 : vCatchDisplay, vCatchInput_iMmPlayer
{
    private enum MotionState
    {
        Moving,
        Braking
    }

    private enum MotionLogTarget
    {
        VelocityLocal,
        InitialVelocityVsAccelerationObject,
        AccelerationLocal,
        EstimatedDistance
    }

    [SerializeField] GameObject directionA = null;
    [SerializeField] GameObject directionG = null;
    [SerializeField] GameObject[] dots = null;
    [SerializeField] private MotionLogTarget motionLogTarget = MotionLogTarget.EstimatedDistance;
    [SerializeField] private float motionVelocityThreshold = 0.01f;
    [SerializeField] private float motionAccelerationObjectThreshold = 0.3f;
    [SerializeField] private float motionAccelerationLocalThreshold = 0.01f;
    [SerializeField] private float motionBrakeAngleThreshold = 60.0f;

    new protected void Awake()
    {
        // 사용할 Face목록
        string[] aryFace = { "W1" };
        Faces = aryFace;

        base.Awake();

        vCatchInput_MmPlayer.AddEventListener(this);
    }

    new protected void Update()
    {
        base.Update();

        SensorProtocolState state = GetProtocolState(sensorProtocolType);
        switch (state)
        {
            case SensorProtocolState.NotConnected: // no vCatchStation
                //enabled = false;
                //vCatchUnityUtil.MessageAndQuit("Middleware를 찾지 못합니다.");
                break;
            case SensorProtocolState.NotSupported:
                //enabled = false;
                //vCatchUnityUtil.MessageAndQuit("CodeReach Contents Certification를 설치하세요.");
                break; // no protocol
            case SensorProtocolState.Initialized:
                break;
        }
        //Trace.WriteLine("ProtocolState:" + state);

        vMmAGB[] AGBs = vCatchInput_MmAGB.vMmAGBs(targetDisplay);
        if (AGBs.Length > 0)
        {
            //Trace.WriteLine("AGBs len:" + AGBs.Length);

            foreach (vMmAGB agb in AGBs)
            {
                OnAGB(agb);
            }
        }
        vMmAGJB[] AGJBs = vCatchInput_MmAGJB.vMmAGJBs(targetDisplay);
        if (AGJBs.Length > 0)
        {
            //Trace.WriteLine("AGJBs len:" + AGJBs.Length);

            foreach (vMmAGJB agjb in AGJBs)
            {
                OnAGJB(agjb);
            }
        }
    }

    vCatchUtil_AG _ag = new vCatchUtil_AG();
    private Vector3 _previousVelocityVector = Vector3.zero;
    private Vector3 _initialVelocityDirection = Vector3.zero;
    private Vector3 _previousAccelerationLocalVector = Vector3.zero;
    [SerializeField] private float MotionDistanceStopHoldMs = 200.0f;
    [SerializeField] private float MotionDistanceMaxSampleDtMs = 250.0f;
    [SerializeField] private float MotionDistanceRearmAccelerationMps2 = 0.05f;
    private bool _motionDistanceWaitingForZero = false;
    private bool _motionDistanceMoving = false;
    private Vector3 _motionDistanceVelocity = Vector3.zero;
    private readonly List<Vector3> _motionDistanceVelocities = new List<Vector3>();
    private readonly List<float> _motionDistanceSampleDtSec = new List<float>();
    private float _motionDistanceDurationSec = 0.0f;
    private float _motionDistanceStillMs = 0.0f;

    public void vCatchInput_MmPlayer_OnMmPlayer() // 디바이스 연결상태 변화되면...
    {
        _motionDistanceWaitingForZero = false;
        ResetMotionDistance();
        JArray ids = new JArray();

        vMmPlayer[] players = vCatchInput_MmPlayer.vMmPlayers(targetDisplay);
        string log = "";
        foreach (var p in players)
        {
            ids.Add(p.id);

            int check = 0;

            log += "  player:" + p.id;
            if (p.parts != null)
            {
                log += " " + p.parts;
                check++;
            }

            if (check >= 1) // 상태 정보를 받은 XRGearM1 확인
            {
                JObject cmdC = new JObject();
                JArray idsC = new JArray();
                idsC.Add(p.id); // 처음 하나의 플레이어만 접속을 유지하고 나머지는 연결끊고, 추가연결 막음
                cmdC.Add("pids", idsC); // 해당 플레이어에게 data 요청
                cmdC.Add("connect", "data");
                DetectionTypeTurnOn(SensorProtocolTypeName(sensorProtocolType),
                    "\"cmd\":" + cmdC.ToString(Formatting.None) + ",\"player-ids\":" + idsC.ToString(Formatting.None));

                // 좌표계산 도우미
                _ag = new vCatchUtil_AG(gameObject.transform.rotation);
                _previousVelocityVector = Vector3.zero;
                _initialVelocityDirection = Vector3.zero;
                _previousAccelerationLocalVector = Vector3.zero;

                break; // 하나의 디바이스만 접속 예제
            }
        }

        if (players.Length == 0) // 연결된 디바이스 없음
        {
            DetectionTypeTurnOn(SensorProtocolTypeName(sensorProtocolType),
                "\"player-ids\":[]"); // 연결이 끊였으면 새로운 연결 허용
        }
        Trace.WriteLine(log);
    }

    Pitching _p = new Pitching();

    void OnAGB(vMmAGB agb)
    {
        if (_ag != null)
        {
            Vector3 aimOrigin = new Vector3(0, 0, 1);
            Quaternion rotationBefore = _ag.rotation();
            Vector3 aimOld = rotationBefore * aimOrigin;
            float angleXOld = Mathf.Atan2(aimOld.z, aimOld.x);

            _ag.ApplyData(agb.ax, agb.ay, agb.az, agb.gx, agb.gy, agb.gz, agb.dtime); // 가속도, 자이로값 적용

            Vector3 velocityVector = _ag.velocityLocal();
            Vector3 accelerationObjectVector = _ag.accelerationObject();
            UpdateMotionDistance(agb, accelerationObjectVector);
            Vector3 accelerationLocalVector = _ag.accelerationLocal();
            bool hasVelocityPhase = TryCompareMotion(velocityVector, ref _previousVelocityVector, motionVelocityThreshold,
                out MotionState velocityState, out float velocityAngle, out float projectedVelocity);
            if (_initialVelocityDirection.sqrMagnitude < Mathf.Epsilon &&
                velocityVector.sqrMagnitude >= motionVelocityThreshold * motionVelocityThreshold)
            {
                _initialVelocityDirection = velocityVector.normalized;
            }

            bool hasAccelerationObjectPhase = _initialVelocityDirection.sqrMagnitude >= Mathf.Epsilon &&
                accelerationObjectVector.sqrMagnitude >= motionAccelerationObjectThreshold * motionAccelerationObjectThreshold;
            MotionState accelerationObjectState = MotionState.Moving;
            float accelerationObjectAngle = -1.0f;
            float projectedAccelerationObject = 0.0f;
            if (hasAccelerationObjectPhase == true)
            {
                projectedAccelerationObject = Vector3.Dot(accelerationObjectVector, _initialVelocityDirection);
                accelerationObjectAngle = Vector3.Angle(_initialVelocityDirection, accelerationObjectVector);
                accelerationObjectState = (projectedAccelerationObject < 0.0f) ? MotionState.Braking : MotionState.Moving;
            }


            bool hasAccelerationLocalPhase = TryCompareMotion(accelerationLocalVector, ref _previousAccelerationLocalVector, motionAccelerationLocalThreshold,
                out MotionState accelerationLocalState, out float accelerationLocalAngle, out float projectedAccelerationLocal);

            _p.Update(_ag, agb.btns); //!!!!!!!!!!!!!!1

            _ag.GravityTo(new Vector3(0.0f, -1.0f, 0.0f)); // 중력방향으로 Object회전 정렬
            //개발중 _ag.ZTo(new Vector3(0.0f, 0.0f, 1.0f));

            gameObject.transform.rotation = _ag.rotation();

            if (directionG != null)
            {
                //directionG.transform.localPosition = _ag.gravityLocal(); // 디바이스 좌표계에서 중력가속도
                directionG.transform.localPosition = _ag.gravity(); // 월드좌표계에서 중력가속도
            }

            Vector3 aimNew = _ag.rotation() * aimOrigin;
            float angleXNew = Mathf.Atan2(aimNew.z, aimNew.x);
            float angleDeltaX = angleXNew - angleXOld;
            
            // (0,0,1)이 회전된 상태 계산. 상하각도(angleYNew)는 절대값으로 신뢰해도 됨. 좌우각도변화(angleDeltaX)는 변화량임.
            //Trace.WriteLine("Aim to:" + aimNew + " angleY:" + angleYNew + " angleX:" + angleDeltaX);
            if (isCheck == true)
            {
                switch (motionLogTarget)
                {
                    case MotionLogTarget.VelocityLocal:
                    {
                        LogMotion(agb, "v", hasVelocityPhase, velocityState, velocityAngle, projectedVelocity);
                        break;
                    }
                    case MotionLogTarget.InitialVelocityVsAccelerationObject:
                    {
                        LogMotion(agb, "ivAo", hasAccelerationObjectPhase, accelerationObjectState, accelerationObjectAngle, projectedAccelerationObject);
                        break;
                    }
                    case MotionLogTarget.AccelerationLocal:
                    {
                        LogMotion(agb, "al", hasAccelerationLocalPhase, accelerationLocalState, accelerationLocalAngle, projectedAccelerationLocal);
                        break;
                    }
                    case MotionLogTarget.EstimatedDistance:
                    {
                        break;
                    }
                }
            }

            if (directionA != null)
            {
                directionA.transform.localPosition = _ag.accelerationLocal(); // 디바이스 좌표계에서 감지된 가속도
                //directionA.transform.localPosition = _ag.acceleration(); // 월드좌표계에서 감지된 가속도
            }
            if (dots != null) // 추정된 동선
            {
                Vector3 vL = _ag.velocityLocal();
                Vector3 moveNew = vL * agb.dtime / 1000.0f;
                moveNew *= 50.0f; // 표현을 위해 확대
                for (int idx = dots.Length - 1; idx > 0; idx--)
                {
                    dots[idx].transform.localPosition = dots[idx - 1].transform.localPosition + moveNew;
                }
                dots[0].transform.localPosition = moveNew;

                //Trace.WriteLine("a:" + _ag.accelerationObject() + " v:" + _ag.velocityLocal());
                //Trace.WriteLine(agb.dtime + "\t" + _ag.accelerationObject().z);


                gameObject.transform.position = moveNew;

            }

        }

        //gameObject.transform.position = (directionA.transform.position - directionG.transform.position) / 2;
    }

    private void UpdateMotionDistance(vMmAGB agb, Vector3 accelerationObjectVector)
    {
        if (isCheck == false || motionLogTarget != MotionLogTarget.EstimatedDistance)
        {
            ResetMotionDistance();
            return;
        }

        if (agb.dtime == 0)
        {
            return;
        }

        if (agb.dtime > MotionDistanceMaxSampleDtMs)
        {
            ResetMotionDistance();
            return;
        }

        float accelerationMps2 = accelerationObjectVector.magnitude;
        if (_motionDistanceWaitingForZero == true)
        {
            if (accelerationMps2 <= MotionDistanceRearmAccelerationMps2)
            {
                _motionDistanceWaitingForZero = false;
            }
            return;
        }

        float thresholdSquared = motionAccelerationObjectThreshold * motionAccelerationObjectThreshold;
        bool isAccelerationActive = accelerationObjectVector.sqrMagnitude >= thresholdSquared;
        if (_motionDistanceMoving == false && isAccelerationActive == false)
        {
            return;
        }

        if (_motionDistanceMoving == false)
        {
            ResetMotionDistance();
            _motionDistanceMoving = true;
        }

        float dt = agb.dtime * 0.001f;
        _motionDistanceVelocity += accelerationObjectVector * dt;
        _motionDistanceVelocities.Add(_motionDistanceVelocity);
        _motionDistanceSampleDtSec.Add(dt);
        _motionDistanceDurationSec += dt;
        _motionDistanceStillMs = (isAccelerationActive == true) ? 0.0f : _motionDistanceStillMs + agb.dtime;
        UnityEngine.Debug.Log($"M1DistanceAccumulated,id={agb.id},tMs={_ag.msec()},dtMs={agb.dtime},accelMps2={accelerationMps2:F2},rawSpeedCmps={_motionDistanceVelocity.magnitude * 100.0f:F1},elapsedMs={_motionDistanceDurationSec * 1000.0f:F0}");

        if (_motionDistanceStillMs >= MotionDistanceStopHoldMs)
        {
            Vector3 finalVelocity = _motionDistanceVelocity;
            Vector3 previousCorrectedVelocity = Vector3.zero;
            float elapsedSec = 0.0f;
            float correctedDistanceM = 0.0f;
            for (int idx = 0; idx < _motionDistanceVelocities.Count; idx++)
            {
                float sampleDt = _motionDistanceSampleDtSec[idx];
                elapsedSec += sampleDt;
                Vector3 correctedVelocity = _motionDistanceVelocities[idx] - finalVelocity * (elapsedSec / _motionDistanceDurationSec);
                correctedDistanceM += (previousCorrectedVelocity.magnitude + correctedVelocity.magnitude) * 0.5f * sampleDt;
                previousCorrectedVelocity = correctedVelocity;
            }

            UnityEngine.Debug.Log($"M1DistanceFinal,id={agb.id},tMs={_ag.msec()},distCm={correctedDistanceM * 100.0f:F1},durMs={_motionDistanceDurationSec * 1000.0f:F0},rawEndSpeedCmps={finalVelocity.magnitude * 100.0f:F1},samples={_motionDistanceVelocities.Count}");
            ResetMotionDistance();
            _motionDistanceWaitingForZero = true;
        }
    }

    private void ResetMotionDistance()
    {
        _motionDistanceMoving = false;
        _motionDistanceVelocity = Vector3.zero;
        _motionDistanceVelocities.Clear();
        _motionDistanceSampleDtSec.Clear();
        _motionDistanceDurationSec = 0.0f;
        _motionDistanceStillMs = 0.0f;
    }

    private void LogMotion(vMmAGB agb, string source, bool hasPhase, MotionState state, float angle, float projected)
    {
        if (hasPhase == false) return;

        UnityEngine.Debug.Log($"M1Motion,id={agb.id},tMs={_ag.msec()},{source}={state.ToString().ToUpperInvariant()}({angle:F1}/{projected:F3})");
    }

    private bool TryCompareMotion(Vector3 currentVector, ref Vector3 previousVector, float threshold,
        out MotionState state, out float angle, out float projected)
    {
        state = MotionState.Moving;
        angle = -1.0f;
        projected = 0.0f;
        float thresholdSquared = threshold * threshold;

        if (currentVector.sqrMagnitude < thresholdSquared)
        {
            return false;
        }

        if (previousVector.sqrMagnitude < thresholdSquared)
        {
            previousVector = currentVector;
            return false;
        }

        projected = Vector3.Dot(currentVector, previousVector.normalized);
        angle = Vector3.Angle(previousVector, currentVector);
        state = angle >= motionBrakeAngleThreshold ? MotionState.Braking : MotionState.Moving;
        previousVector = currentVector;
        return true;
    }

    public bool isCheck = false;

    void OnAGJB(vMmAGJB agjb)
    {
        OnAGB(new vMmAGB(agjb.id, agjb.ax, agjb.ay, agjb.az, agjb.gx, agjb.gy, agjb.gz, agjb.dtime, agjb.btns, agjb.time));
    }


}
