/****************************************************************************
*                                                                           *
* vCatchProtocol_SpeedPoint.cs                                              *
*                                                                           *
* made by Willy.Lee                                                         *
*                                                                           *
*    Kee-Wan Lee, 2022-          e-mail : wiljwilj@hotmail.com              *
*                                                                           *
****************************************************************************/

using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace vCatchStation
{
    public class vCatchProtocol_SpeedPoint : vCatchProtocol.Protocol
    {
		public vCatchProtocol_SpeedPoint(vCatchBehaviour.Logger log) : base(log)
        {
		}

		public override string name()
        {
            return "speedpoint";
        }

		ConcurrentQueue<vSpeedPoint> _queueSpeedPoint = new ConcurrentQueue<vSpeedPoint>();
		public override void OnPacket(JArray json)
		{
			if (json.Count == 0)
			{
				//Middleware - speedpoint ÁßÁöµÊ
				return;
			}

			foreach (var jitem in json)
			{
				try
				{
					// speedpoint data
					float x = (float)jitem["x"];
					float y = (float)jitem["y"];
					float km_h = (float)jitem["km_h"];
					ushort time = (ushort)jitem["t"];
					_queueSpeedPoint.Enqueue(new vSpeedPoint(x, y, km_h, time));
					if (_queueSpeedPoint.Count > 1000)
					{
						vSpeedPoint speedpoint;
						_queueSpeedPoint.TryDequeue(out speedpoint);
						Log.w(TAG, "speedpoint data overflow");
					}
				}
				catch
				{
					Log.w(TAG, "broken speedpoint data detected");
				}
			}
		}

		List<vSpeedPoint> _listSpeedPoint = new List<vSpeedPoint>();

		internal vSpeedPoint DequeueSpeedPoint()
		{
			vSpeedPoint speedpoint;
			if (_queueSpeedPoint.TryDequeue(out speedpoint))
				return speedpoint;
			return null;
		}

		public override void MakeInput(int targetDisplay)
		{
			vSpeedPoint speedpoint;
			while ((speedpoint = DequeueSpeedPoint()) != null)
				_listSpeedPoint.Add(speedpoint);
			vCatchInput_SpeedPoint._arySpeedPoints[targetDisplay] = _listSpeedPoint.ToArray();
			if (vCatchInput_SpeedPoint._arySpeedPoints[targetDisplay].Length != 0)
				_listSpeedPoint = new List<vSpeedPoint>();
		}

		public override void LateUpdate(int targetDisplay, vScreen vScrn, vCatchInputModule inputModule)
		{
			// canvas event process
			if (vScrn != null && inputModule != null && inputModule.isActiveAndEnabled)
			{
				vSpeedPoint[] speedpoints = vCatchInput_SpeedPoint.vSpeedPoints(targetDisplay);
				if (speedpoints != null)
				{
					foreach (var sp in speedpoints)
						inputModule.ProcessTouch(targetDisplay, -1, sp.x, sp.y, vScrn, true, true);
				}
			}

			vSpeedPoint speedpoint;
			while ((speedpoint = DequeueSpeedPoint()) != null)
				_listSpeedPoint.Add(speedpoint);
			vCatchInput_SpeedPoint._arySpeedPoints[targetDisplay] = null;
		}

		public override void ResetData()
        {
			vSpeedPoint speedpoint;
			while (_queueSpeedPoint.TryDequeue(out speedpoint)) ;

			_queueSpeedPoint = new ConcurrentQueue<vSpeedPoint>();
		}

		const string TAG = "vCatchProtocol_SpeedPoint";
    }

	public static class vCatchInput_SpeedPoint
	{
		public static vSpeedPoint[][] _arySpeedPoints = { null, null, null, null, null, null, null, null };

		public static vSpeedPoint[] vSpeedPoints(int targetDisplay)
        {
			if (_arySpeedPoints[targetDisplay] == null)
				vCatchDisplay.vCatchDisplays[targetDisplay].MakeInput(vCatchProtocol.SensorProtocolType.SpeedPoint);
			if (_arySpeedPoints[targetDisplay] == null)
				_arySpeedPoints[targetDisplay] = new vSpeedPoint[0];
			return _arySpeedPoints[targetDisplay];
		}
	}
}
