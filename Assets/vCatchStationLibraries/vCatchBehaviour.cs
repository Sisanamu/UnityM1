/****************************************************************************
*                                                                           *
* vCatchBehaviour.cs                                                        *
*                                                                           *
* made by Willy.Lee                                                         *
*                                                                           *
*    Kee-Wan Lee, 2022-          e-mail : wiljwilj@hotmail.com              *
*                                                                           *
****************************************************************************/
// 2022-01-28 - 
// 2022-08-12 - footstep_lrfb 추가. certification 추가. face 없는 경우 처리
// 2022-09-06 - remove DequeueSimplifiedDrag and add util
// 2022-10-12 - GetProtocolState 개선, vScreen 개선
// 2022-10-28 - GetScreen(true) 개선
// 2023-10-12 - Separate protocol codes
// 2024-01-10 - support the face [ALL]

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace vCatchStation
{
	public class vCatchBehaviour : MonoBehaviour, vCatchClientSocket.Event
	{
		public vCatchBehaviour()
		{
			Log = new Logger(this);
		}

        protected vCatchClientSocket _sock = null;

        private string _face = null;
		private string _faceDriver = null;
		private JArray _aryDetectionTypes = new JArray();
		private enum DoState { edsStop, edsDo };
		private List<DoState> _aryDoState = new List<DoState>();
		private List<string> _aryDoOption = new List<string>();
		private List<DoState> _aryWantDoState = new List<DoState>();
		private List<string> _aryWantDoOption = new List<string>();

		private JObject _aryInitializedTypes = new JObject();

		private JObject _aryFaceRect = new JObject();
		private bool _bHaveBeenConnected = false;
		private bool _bInitialized = false;

        public string DetectionTypes
        {
            get { return _aryDetectionTypes.ToString(Formatting.None); }
        }
		public string Face
        {
			get { return _face; }
			set { SetFace(value); }
        }
		public Dictionary<string, string> AllProtocolState
		{
			get {
				if (_sock == null || !_sock.Connected)
					return null;

				Dictionary<string, string> dict = new Dictionary<string, string>();

				foreach (JObject dt in _aryDetectionTypes)
				{
					string type = (string)dt["type"];
					if (_aryInitializedTypes[type] == null)
					{
						dict.Add(type, _bInitialized ? "no sensor" : "initializing");
					}
					else
					{
						dict.Add(type, "initialized");
					}
				}

				return dict;
			}
		}

		public void AddDetectionType(Dictionary<string, JToken> detectionType)
        {
            JObject json = new JObject();
            foreach (KeyValuePair<string, JToken> item in detectionType)
            {
                if (item.Key == "face")
                    continue;
                json.Add(item.Key, item.Value);
            }
			if (_face != null)
	            json["face"] = _face;
            _aryDetectionTypes.Add(json);
			_aryDoState.Add(DoState.edsStop);
			_aryDoOption.Add(null);
			_aryWantDoState.Add(DoState.edsStop);
			_aryWantDoOption.Add(null);

			if (_face != null)
            {
				if (_sock == null)
					_sock = new vCatchClientSocket();
				else
					_sock.Close();
				_aryInitializedTypes = new JObject();
				_bInitialized = false;
				_aryFaceRect = new JObject();
				_sock.Connect(DetectionTypes, this);
            }
        }
        private void SetFace(string face)
        {
            _face = _faceDriver = face;
			if (_face != null)
            {
				foreach (JObject json in _aryDetectionTypes)
				{
					json["face"] = _face;
				}
            }

            if (_aryDetectionTypes.Count > 0)
            {
				if (_sock == null)
					_sock = new vCatchClientSocket();
				else
					_sock.Close();
				_aryInitializedTypes = new JObject();
				_bInitialized = false;
				_aryFaceRect = new JObject();
				_sock.Connect(DetectionTypes, this);
            }
        }
        public bool DetectionTypeTurnOn(string type, string options = null)
        {
			for (int idx = _aryDetectionTypes.Count - 1; idx >= 0; idx--)
            {
				JObject dt = (JObject)_aryDetectionTypes[idx];
				if ((string)dt["type"] != type)
					continue;
				_aryWantDoState[idx] = DoState.edsDo;
				_aryWantDoOption[idx] = options;
			}

			if (!_bInitialized)
				return false;

			UpdateDoState();
			return true;
        }
		public bool DetectionTypeTurnOff(string type)
		{
			for (int idx = _aryDetectionTypes.Count - 1; idx >= 0; idx--)
			{
				JObject dt = (JObject)_aryDetectionTypes[idx];
				if ((string)dt["type"] != type)
					continue;
				_aryWantDoState[idx] = DoState.edsStop;
				_aryWantDoOption[idx] = null;
			}

			if (!_bInitialized)
				return false;

			UpdateDoState();
			return true;
		}

		private bool _face_new = false;
		private bool _face_read = false;
		private int _face_left = 0;
		private int _face_top = 0;
		private int _face_width = 0;
		private int _face_height = 0;
		private int _face_unityscreen = 0;
		private int? _face_mm_width = null;
		private int? _face_mm_height = null;
		private int? _face_mm_bottom = null;
		public vScreen GetScreen(bool bGetOnlyNew = false)
        {
			if (bGetOnlyNew)
            {
				if (!_face_new)
                {
					return null;
                }
            }
			_face_read = true;
			return new vScreen(_face_left, _face_top, _face_width, _face_height, _face_unityscreen,
				_face_mm_width, _face_mm_height, _face_mm_bottom);
        }

		public enum SensorProtocolState
		{
			Standby,      // 시작
			Connecting,   // 연결중
			NotConnected, // 연결안됨
			Disconnected, // 연결 끊김
			NotSupported, // 프로토콜 지원안함
			Initializing, Initialized
		};
		public SensorProtocolState GetProtocolState(string type)
		{
			if (_sock == null)
				return SensorProtocolState.Standby;
			if (!_sock.Connected)
            {
				if (!_sock.Closed)
					return SensorProtocolState.Connecting;
				return _bHaveBeenConnected ? SensorProtocolState.Disconnected : SensorProtocolState.NotConnected;
			}

			if (_aryInitializedTypes[type] == null)
			{
				return _bInitialized ? SensorProtocolState.NotSupported : SensorProtocolState.Initializing;
			}
			else
			{
				return SensorProtocolState.Initialized;
			}
		}

		private bool UpdateDoState()
        {
			if (_sock == null || !_sock.Connected)
				return false;

			bool failure = false;
			for (int idx = _aryDetectionTypes.Count - 1; idx >= 0; idx--)
            {
				if (_aryDoState[idx] == _aryWantDoState[idx] &&
					(_aryDoOption[idx] == null || _aryDoOption[idx].Length == 0) &&
					(_aryWantDoOption[idx] == null || _aryWantDoOption[idx].Length == 0))
					continue;

				JObject jobj = (JObject)_aryDetectionTypes[idx];
				string type = (string)jobj["type"];

				bool bInited = _aryInitializedTypes[type] != null;
				if (!bInited)
                {
					Log.i(TAG, "{0} can't active {1} protocol yet", _faceDriver, type);
					failure |= true;
					continue;
                }

				switch (_aryWantDoState[idx])
                {
					case DoState.edsDo:
						_aryDoState[idx] = DoState.edsDo;
						_aryDoOption[idx] = _aryWantDoOption[idx];
						failure |= _sock != null && !_sock.Do_vCatch(type + "|" + _faceDriver, _aryWantDoOption[idx]);
						break;

					case DoState.edsStop:
						if (_sock != null)
							_sock.Do_vCatch(type + "|" + _faceDriver, "cmd:\"stop\"");
						_aryDoState[idx] = DoState.edsStop;
						_aryDoOption[idx] = _aryWantDoOption[idx];
						break;
				}
			}

			return !failure;
		}

		protected void Update()
        {
            if (_face_read)
            {
				_face_new = false;
				_face_read = false;
			}
        }

        protected void OnEnable()
        {
			//if (_aryDetectionTypes.Count > 0 && _sock.Closed)
            //{
			//	Log.w(TAG, "{0} can't connect to vCatchStaton", _face);
            //}
        }

        protected void OnDisable()
        {
            if (_sock != null && _sock.Connected)
            {
                _sock.Close();
			}
			_aryInitializedTypes = new JObject();
			_bInitialized = false;
		}

        protected void OnApplicationFocus(bool hasFocus)
        {
            if (enabled && isActiveAndEnabled && _sock != null && _sock.Closed)
            {
				//Middleware - 연결이 끊긴 경우 복구
				if (_aryDetectionTypes.Count > 0 && !_sock.Connect(DetectionTypes, this))
                {
					Log.w(TAG, "{0} can't connect to vCatchStaton", _face);
				}
			}
        }

        public void vCatchClientSocket_OnPacket(JObject json)
        {
			//Middleware - 사용하는 FACE에 대한 screen 정보를 받음.
			JObject jpckt = (JObject)json["screen"];
            if (jpckt != null)
            {
				foreach (var jface in jpckt)
                {
					if (jface.Key == null)
						continue;
					_aryFaceRect[jface.Key] = jface.Value;
					if (jface.Key != Face)
						continue;

					JObject rc = (JObject)jface.Value;

					int left, top, width, height;
                    try
                    {
						left = (int)rc["left"];
						top = (int)rc["top"];
						width = (int)rc["width"];
						height = (int)rc["height"];

						_face_new = true;
						_face_read = false;
						_face_left = left;
						_face_top = top;
						_face_width = width;
						_face_height = height;
					}
					catch
                    {
						left = top = width = height = 0;
					}
					try
					{
						_face_mm_width = (int)rc["mm-width"];
					}
					catch
					{
						_face_mm_width = null;
					}
					try
					{
						_face_mm_height = (int)rc["mm-height"];
					}
					catch
					{
						_face_mm_height = null;
					}
					try
					{
						_face_mm_bottom = (int)rc["mm-bottom"];
					}
					catch
					{
						_face_mm_bottom = null;
					}
					int unityscreen;
					try
					{
						unityscreen = (int)rc["unityscreen"];

						_face_unityscreen = unityscreen;
					}
					catch
					{
						unityscreen = -1;
					}

					Log.d(TAG, "received - face:{0} ({1},{2},{3},{4}) unityscreen:{5}", _face, left, top, width, height, unityscreen);
				}
			}

			if (_bInitialized)
            {
				jpckt = (JObject)json["data"];
				if (jpckt == null)
					return;

				foreach (var item in jpckt)
                {
					int d = item.Key.IndexOf('|');
					if (d < 0)
						continue;
					string face = item.Key.Substring(d + 1);
					if (face != _faceDriver)
						continue;
					string protocol = item.Key.Substring(0, d);

					bool bInited = _aryInitializedTypes[protocol] != null;
					if (!bInited)
						continue;

                    try
                    {
						JArray jdata = (JArray)item.Value;
						if (!OnData(protocol, jdata))
                        {
							Log.w(TAG, "uninitialized protocol data detected - {0}", protocol);
						}
                    }
                    catch
                    {
						Log.w(TAG, "broken data detected - {0}", protocol);
					}
				}
			}
			else
            {
				//Middleware - Middleware에 접속이 성공하여 정보를 받음
				foreach (JObject jtype in _aryDetectionTypes)
                {
					string protocol;
					if (_faceDriver != null && _faceDriver != "")
                    {
						if ((string)jtype["face"] != _faceDriver)
							continue;
						protocol = (string)jtype["type"];
						jpckt = (JObject)json[protocol];
						if (jpckt == null)
							continue;
					}
					else
                    {
						protocol = (string)jtype["type"];
						jpckt = (JObject)json[protocol];
						if (jpckt == null)
							continue;
						var enumerator = jpckt.GetEnumerator();
						enumerator.MoveNext();
						var kv = enumerator.Current;
						_face = kv.Key;
					}
					JObject jpcktD = (JObject)jpckt[_faceDriver];
					if (jpcktD == null)
                    {
						jpcktD = (JObject)jpckt["[ALL]"];
						if (jpcktD == null)
							continue;
						_faceDriver = "[ALL]";
					}

					string sensorname = (string)jpcktD["sensor"];
					string revision = (string)jpcktD["revision"];
					string sensitivity = (string)jpcktD["sensitivity"];

					_aryInitializedTypes.Add(protocol, jpcktD);
					Log.d(TAG, "received - face:{0} sensor:{1} rev:{2} sensitivity:{3}", _face, sensorname, revision, sensitivity);
				}

				//Middleware - 접속과 동시에 입력을 원하면 아래 주석제거
				_bHaveBeenConnected = true;
				_bInitialized = true;

				UpdateDoState();
			}
		}

		protected virtual bool OnData(string protocol, JArray jdata)
        {
			Log.w(TAG, "no inplemented protocol - {0}", protocol);
			return true;
        }

		public void vCatchClientSocket_OnClose()
        {
			for (int idx = _aryDoState.Count - 1; idx >= 0; idx--)
            {
				_aryDoState[idx] = DoState.edsStop;
				_aryDoOption[idx] = _aryWantDoOption[idx];
			}
			_aryInitializedTypes = new JObject();
			_bInitialized = false;
			_aryFaceRect = new JObject();

			ClearDataAll();
		}

		protected virtual void ClearDataAll()
		{
		}

		static long s_tmLast = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
		public class Logger
		{
			public Logger(vCatchBehaviour pthis)
            {
				_pthis = pthis;
			}

			vCatchBehaviour _pthis;

			public void i(string tag, string format, params object[] args)
            {
				StackFrame sf = new StackFrame(1, true);
				long tmCur = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
				string head = string.Format("{0}] ", (int)(tmCur - s_tmLast));
				string msg = string.Format(format, args);
				s_tmLast = tmCur;
#if DEBUG
				UnityEngine.Debug.Log(head + msg);
#endif //DEBUG
				try
                {
					if (_pthis._sock == null || !_pthis._sock.Connected)
						return;
					_pthis._sock.AddLog(tmCur, vCatchStation.TraceLevel_Information,
						tag, head + msg, sf.GetFileName(), sf.GetFileLineNumber());
				}
                catch
                {
                }
            }
			public void w(string tag, string format, params object[] args)
			{
				StackFrame sf = new StackFrame(1, true);
				long tmCur = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
				int dtm = (int)(tmCur - s_tmLast);
				string head = string.Format("W{0}] ", dtm); 
				string msg = string.Format(format, args);
				s_tmLast = tmCur;
#if DEBUG
				UnityEngine.Debug.LogError(head + msg);
#endif //DEBUG
				try
				{
					if (_pthis._sock == null || !_pthis._sock.Connected)
						return;
					_pthis._sock.AddLog(tmCur, vCatchStation.TraceLevel_Warnning,
						tag, head + msg, sf.GetFileName(), sf.GetFileLineNumber());
				}
				catch
				{
				}
			}
			public void d(string tag, string format, params object[] args)
			{
#if DEBUG
				StackFrame sf = new StackFrame(1, true);
				long tmCur = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
				string head = string.Format("D{0}] ", (int)(tmCur - s_tmLast));
				string msg = string.Format(format, args);
				s_tmLast = tmCur;

				UnityEngine.Debug.LogWarning(head + msg);
				try
				{
					if (_pthis._sock == null || !_pthis._sock.Connected)
						return;
					_pthis._sock.AddLog(tmCur, vCatchStation.TraceLevel_Debug,
						tag, head + msg, sf.GetFileName(), sf.GetFileLineNumber());
				}
				catch
				{
				}
#endif //DEBUG
			}
		}
		public Logger Log;
		const string TAG = "vCatchBehaviour";
	}
}
