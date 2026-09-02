using System;
using System.Diagnostics;
using UnityEngine;
using vCatchStation;

class Pitching
{
    Vector3 _s = new Vector3(0, 0, 0);
    bool _sUp = true;
    uint _msUp = 0;

    bool _btnS = false;

    public void Update(vCatchUtil_AG ag, int btns)
    {
        float old_v = _s.magnitude;
        Vector3 vL = ag.velocityLocal();
        const float iir = 0.9f; //@@ 지속적 변화에 적응
        _s.x = _s.x * iir + vL.x * (1.0f - iir);
        _s.y = _s.y * iir + vL.y * (1.0f - iir);
        _s.z = _s.z * iir + vL.z * (1.0f - iir);
        float v = _s.magnitude;

        if (v > old_v)
            _sUp = true;
        else if (v < old_v)
        {
            if (_sUp)
            {
                uint oldms = _msUp;
                _sUp = false;
                _msUp = ag.msec();

                 if (v > 0.25f &&           //@@ 속도가 이상일 때
                    (_msUp - oldms) > 400)  //@@ 움직임 시간이 이상일 때
                {
                    onSwing(v);
                }
            }
            else
            {
                _msUp = ag.msec();
            }
        }

        bool oldBtnS = _btnS;
        _btnS = (vMmB.BtnS & btns) != 0x0;
        if (oldBtnS && !_btnS)
        {
            float a = Vector3.Angle(new Vector3(vL.x, 0.0f, vL.z), vL);
            if (vL.y > 0.0f) a *= -1.0f;
            onThrow(v, a);
        }
    }

    void onSwing(float v)
    {
        Trace.WriteLine("Swing:" + v);
    }

    void onThrow(float v, float a)
    {
        Trace.WriteLine("Throw:" + v + "  " + a);
    }
}
