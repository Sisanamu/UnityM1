/****************************************************************************
*                                                                           *
* vCatchDisplay.cs                                                          *
*                                                                           *
* made by Willy.Lee                                                         *
*                                                                           *
*    Kee-Wan Lee, 2022-          e-mail : wiljwilj@hotmail.com              *
*                                                                           *
****************************************************************************/
// 2022-10-12 - 미들웨어 없거나 프로토콜 지원안하면 종료
// 2023-10-12 - Separate protocol codes

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace vCatchStation
{
    public class vCatchDisplay : vCatchProtocol
    {
        [SerializeField] bool quitAppIfNoMiddleware = true;
        [SerializeField] protected Behaviour[] targetDisplayDependentBehaviours;

        protected int _targetDisplay = 0;
        public int targetDisplay { get { return _targetDisplay; } }

        public static vCatchDisplay[] vCatchDisplays = { null, null, null, null, null, null, null, null };

        new protected void Awake()
        {
            base.Awake();
 
            vCatchDisplays[targetDisplay] = this;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetActiveWindow();
        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x80000;
        [DllImport("user32.dll")]
        static extern uint GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
        const int LWA_ALPHA = 0x00000002;
        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        static IntPtr hwndMain = IntPtr.Zero;
        static int[] activedTargetDisplay = { 0, 0, 0, 0, 0, 0, 0, 0 };

        [SerializeField] vCatchInputModule inputModule = null;

        new protected void Start()
        {
            base.Start();

            if (inputModule == null)
            {
                UnityEngine.Debug.LogWarning("vCatchDisplayInput with empty inputModule field");
            }
        }

        new protected void Update()
        {
            base.Update();

            SensorProtocolState protocol_state = GetProtocolState(sensorProtocolType);
            if (quitAppIfNoMiddleware)
            {
                if (protocol_state == SensorProtocolState.NotConnected)
                {
                    string fc = Face;
                    if (fc == null)
                        fc = "[NoFace]";
                    vCatchUnityUtil.MessageAndQuit(fc + ": 미들웨어에 연결할 수 없습니다.");
                    enabled = false;
                }
                if (protocol_state == SensorProtocolState.NotSupported)
                {
                    string fc = Face;
                    if (fc == null)
                        fc = "[NoFace]";
                    vCatchUnityUtil.MessageAndQuit("필요한 프로토콜(" + fc + ":" + sensorProtocolType.ToString() + ")을 지원하지 않는 환경입니다.");
                    enabled = false;
                }
            }

            if (hwndMain == IntPtr.Zero)
            {
                hwndMain = GetActiveWindow();
                UnityEngine.Debug.Log("hwndMain: " + hwndMain);
            }

            // Face 좌표의 상태변화 확인
            vScreen rcScrn = GetScreen(true);
            if (rcScrn != null)
            {
                UnityEngine.Debug.Log("vCatchDisplay: " + rcScrn.ToString());
                if (rcScrn.unityscreen < 1 || rcScrn.unityscreen > 8)
                {
                    vCatchUnityUtil.MessageAndQuit("해당 화면(" + Face + ")이 없습니다.");
                    enabled = false;
                    return;
                }
                _targetDisplay = rcScrn.unityscreen - 1;

                for (int idxD = 0; idxD < 8; idxD++)
                {
                    if (vCatchDisplays[idxD] == this)
                        vCatchDisplays[idxD] = null;
                }
                vCatchDisplays[targetDisplay] = this;

                if (targetDisplay >= 0 && targetDisplay < Display.displays.Length)
                {
                    if (activedTargetDisplay[targetDisplay] == 0)
                    {
                        activedTargetDisplay[targetDisplay] = 1;
                        UnityEngine.Debug.Log("vCatchDisplay: Active TargetDisplay " + targetDisplay.ToString());
                        Display.displays[targetDisplay].Activate();
                        if (targetDisplay == 0)
                            Display.displays[0].SetParams(rcScrn.width, rcScrn.height, rcScrn.left, rcScrn.top);
                     }
                }
                OnTargetDisplayChanged(targetDisplay);
                
                /*Debug.Log("Display["+ targetDisplay + "]: " + Display.displays[targetDisplay].active +
                    " sys: " + Display.displays[targetDisplay].systemWidth + " x " + Display.displays[targetDisplay].systemHeight +
                    " rend:" + Display.displays[targetDisplay].renderingWidth + " x " + Display.displays[targetDisplay].renderingHeight);*/
            }
            else if (activedTargetDisplay[targetDisplay] == 1)
            {
                activedTargetDisplay[targetDisplay] = 2;

                // hidden if main is not used
                if (activedTargetDisplay[0] == 0)
                {
                    activedTargetDisplay[0] = 2;

                    SetWindowLong(hwndMain, GWL_EXSTYLE,
                        GetWindowLong(hwndMain, GWL_EXSTYLE) | WS_EX_LAYERED);
                    SetLayeredWindowAttributes(hwndMain, 0, 0, LWA_ALPHA);
                }

                vCatchUnityUtil.RemoveScreenRegistry();
            }
        }

        public void MakeInput(SensorProtocolType protocolType)
        {
            protocols[(int)protocolType].MakeInput(targetDisplay);
        }

        protected void LateUpdate()
        {
            vScreen vScrn = GetScreen();
            protocols[(int)sensorProtocolType].LateUpdate(targetDisplay, vScrn, inputModule);
        }

        protected virtual void OnTargetDisplayChanged( int targetDisplay )
        {
            Log.i(TAG, "OnTargetDisplayChanged face={0} Display{1}", Face, targetDisplay + 1);
            foreach (var behaviour in targetDisplayDependentBehaviours)
            {
                var type = behaviour.GetType();
                var prop = type.GetProperty("targetDisplay");
                if (prop.CanWrite)
                    prop.SetValue(behaviour, targetDisplay);
            }
        }

        void OnApplicationQuit()
        {
            vCatchUnityUtil.RemoveScreenRegistry();
        }

        const string TAG = "vCatchDisplay";
    }
}
