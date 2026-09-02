/****************************************************************************
*                                                                           *
* vCatchProtocol.cs                                                         *
*                                                                           *
* made by Willy.Lee                                                         *
*                                                                           *
*    Kee-Wan Lee, 2022-          e-mail : wiljwilj@hotmail.com              *
*                                                                           *
****************************************************************************/
// 2022-10-12 - GetProtocolState
// 2023-10-12 - Separate protocol codes
// 2024-03-05 - Added SpeedPoint protocol

// define the protocols to be used by solution developers
#define USE_PROTOCOL_DRAG
#define USE_PROTOCOL_SPEEDPOINT
#define USE_PROTOCOL_CAMERA
#define USE_PROTOCOL_MULTIMODAL_AGJB
#define USE_PROTOCOL_MULTIMODAL_P

using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace vCatchStation
{
    public class vCatchProtocol : vCatchBehaviour
    {
        public static string[] Faces = { "W1" };
        [SerializeField] protected int _indexFace = 0;

        public int indexFace { get { return _indexFace; } }

		public abstract class Protocol
		{
			protected vCatchBehaviour.Logger Log;
			protected Protocol(vCatchBehaviour.Logger log)
            {
				Log = log;
			}

			public abstract string name();
			public abstract void OnPacket(JArray jdata);
			public abstract void MakeInput(int targetDisplay);
			public abstract void LateUpdate(int targetDisplay, vScreen vScrn, vCatchInputModule inputModule);
			public abstract void ResetData();
		}
		
		public enum SensorProtocolType //@@protocol
		{
			Screen = 0
			, Click
#if USE_PROTOCOL_DRAG
			, Drag
#endif //USE_PROTOCOL_DRAG
#if USE_PROTOCOL_SPEEDPOINT
			, SpeedPoint
#endif //USE_PROTOCOL_SPEEDPOINT
#if USE_PROTOCOL_CAMERA
			, Camera
#endif //USE_PROTOCOL_CAMERA
#if USE_PROTOCOL_MULTIMODAL_AGJB
			, Multimodal_B, Multimodal_JB
			, Multimodal_AGB, Multimodal_AGJB
#endif //USE_PROTOCOL_MULTIMODAL_AGJB
#if USE_PROTOCOL_MULTIMODAL_P
			, Multimodal_P
#endif //USE_PROTOCOL_MULTIMODAL_P
		};
		protected Protocol[] protocols;
		public vCatchProtocol()
        {
			Protocol[] ps = { //@@protocol
				null //!!
				, new vCatchProtocol_Click(Log)
#if USE_PROTOCOL_DRAG
				, new vCatchProtocol_Drag(Log)
#endif //USE_PROTOCOL_DRAG
#if USE_PROTOCOL_SPEEDPOINT
				, new vCatchProtocol_SpeedPoint(Log)
#endif //USE_PROTOCOL_SPEEDPOINT
#if USE_PROTOCOL_CAMERA
				, new vCatchProtocol_Camera(Log)
#endif //USE_PROTOCOL_CAMERA
#if USE_PROTOCOL_MULTIMODAL_AGJB
				, new vCatchProtocol_MM_B(Log), new vCatchProtocol_MM_JB(Log)
				, new vCatchProtocol_MM_AGB(Log), new vCatchProtocol_MM_AGJB(Log)
#endif //USE_PROTOCOL_MULTIMODAL_AGJB
#if USE_PROTOCOL_MULTIMODAL_P
				, new vCatchProtocol_MM_P(Log)
#endif //USE_PROTOCOL_MULTIMODAL_P
			};
			protocols = ps;
		}

		public string SensorProtocolTypeName(SensorProtocolType type)
        {
			return protocols[(int)type].name();
		}

		[SerializeField] SensorProtocolType _sensorProtocolType = SensorProtocolType.Click;
        protected SensorProtocolType sensorProtocolType
        {
            get { return _sensorProtocolType; }
        }

        protected void Awake()
        {
            Debug.Log("Faces: " + string.Join(",", Faces));

            if (Faces.Length <= indexFace)
            {
                vCatchUnityUtil.MessageAndQuit("사용할 디스플레이 수가 부족합니다\n - 전달 받은 디스플레이 : " + string.Join(",", Faces) + "\n - " + indexFace + "번째 디스플레이 사용");
                return;
            }

            if (indexFace >= 0)
                Face = Faces[indexFace];
            else
            {
                Face = "";
                Debug.LogWarning("target display is not selected.");
            }

			var type = new Dictionary<string, JToken>();
			type.Add("type", SensorProtocolTypeName(sensorProtocolType));
			AddDetectionType(type);
        }

        protected void Start()
        {
			if (sensorProtocolType != SensorProtocolType.Screen)
            {
				// protocol-type 요청
				DetectionTypeTurnOn(SensorProtocolTypeName(sensorProtocolType));
			}
        }

        public SensorProtocolState GetProtocolState(SensorProtocolType type)
        {
            return GetProtocolState(SensorProtocolTypeName(type));
        }

        protected override bool OnData(string protocol, JArray jdata)
        {
			bool ret = false;
			for (int idx = protocols.Length - 1; idx >= 0; idx--)
            {
				if (protocols[idx] == null)
					continue;
				if (protocols[idx].name() != protocol)
					continue;
				protocols[idx].OnPacket(jdata);
				ret = true;
			}

			return ret;
		}

		protected override void ClearDataAll()
		{
			for (int idx = protocols.Length - 1; idx >= 0; idx--)
			{
				if (protocols[idx] == null)
					continue;
				protocols[idx].ResetData();
			}
		}

		const string TAG = "vCatchProtocol";
    }
}
