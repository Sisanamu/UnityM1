/****************************************************************************
*                                                                           *
* vCatchProtocol_Camera.cs                                                  *
*                                                                           *
* made by Willy.Lee                                                         *
*                                                                           *
*    Kee-Wan Lee, 2023-          e-mail : wiljwilj@hotmail.com              *
*                                                                           *
****************************************************************************/

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace vCatchStation
{
    public class vCatchProtocol_Camera : vCatchProtocol.Protocol
    {
		public vCatchProtocol_Camera(vCatchBehaviour.Logger log) : base(log)
        {
		}

		public override string name()
        {
            return "camera";
        }

		int _idxCamera = -1;
		string _nameCamera = null;
		string _aliasCamera = null;
		string _pathCamera = null;
		vCamera.Resolution[] _res = null;
		bool _eventCamera = false;
		public override void OnPacket(JArray json)
		{
			if (json.Count == 0)
			{
				//Middleware - camera ÁßÁöµÊ
				closeAndfireEvent();
				return;
			}

			foreach (var jitem in json)
			{
				try
				{
					// camera inf data
					_idxCamera = (int)jitem["idx"];
					_nameCamera = (string)jitem["name"];
					_aliasCamera = (string)jitem["alias"];
					_pathCamera = (string)jitem["path"];

					List<vCamera.Resolution> listRess = new List<vCamera.Resolution>();

					JArray ress = (JArray)jitem["res"];
					foreach (var res in ress)
                    {
						int w = (int)res["w"];
						int h = (int)res["h"];
						string f = (string)res["f"];
						listRess.Add(new vCamera.Resolution(w, h, f));
					}

					_res = listRess.ToArray();
				}
				catch
				{
					Log.w(TAG, "broken camera data detected");

					_idxCamera = -1;
					_nameCamera = null;
					_aliasCamera = null;
					_pathCamera = null;
					_res = null;
				}

				_eventCamera = true;
			}
		}

		public override void MakeInput(int targetDisplay)
		{
		}

		public override void LateUpdate(int targetDisplay, vScreen vScrn, vCatchInputModule inputModule)
		{
			if (_eventCamera)
			{
				_eventCamera = false;

				vCatchInput_Camera._aryCameras[targetDisplay] = new vCamera(_idxCamera, _nameCamera, _aliasCamera, _pathCamera, _res);
				vCatchInput_Camera.fireEvent(targetDisplay);
			}
		}

		void closeAndfireEvent()
        {
			if (_idxCamera < 0)
				return;
			_idxCamera = -1;
			_nameCamera = null;
			_aliasCamera = null;
			_pathCamera = null;
			_res = null;
			_eventCamera = true;
		}

		public override void ResetData()
        {
			//Middleware - camera ÁßÁöµÊ
			closeAndfireEvent();
		}

		const string TAG = "vCatchProtocol_Camera";
    }

	public interface vCatchInput_iCamera
	{
		int vCatchInput_iCamera_targetDisplay();
		void vCatchInput_iCamera_OnCameraDevice();
	}

	public static class vCatchInput_Camera
	{
		public static vCamera[] _aryCameras = new vCamera[8];

		public static vCamera vCamera(int targetDisplay)
		{
			return _aryCameras[targetDisplay];
		}

		static List<vCatchInput_iCamera> _listListenr = new List<vCatchInput_iCamera>();

		public static void AddEventListener(vCatchInput_iCamera listener)
		{
			_listListenr.Add(listener);
		}

		public static void fireEvent(int targetDisplay)
		{
			_listListenr.ForEach(delegate (vCatchInput_iCamera iCamera)
			{
				if (iCamera.vCatchInput_iCamera_targetDisplay() == targetDisplay)
					iCamera.vCatchInput_iCamera_OnCameraDevice();
			});
		}
	}
}
