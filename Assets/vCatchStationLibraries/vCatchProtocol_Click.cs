/****************************************************************************
*                                                                           *
* vCatchProtocol_Click.cs                                                   *
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
    public class vCatchProtocol_Click : vCatchProtocol.Protocol
    {
		public vCatchProtocol_Click(vCatchBehaviour.Logger log) : base(log)
        {
		}

		public override string name()
        {
            return "click";
        }

		ConcurrentQueue<vClick> _queueClick = new ConcurrentQueue<vClick>();
		public override void OnPacket(JArray json)
		{
			if (json.Count == 0)
			{
				//Middleware - click ÁßÁöµÊ
				return;
			}

			foreach (var jitem in json)
			{
				try
				{
					// click data
					float x = (float)jitem["x"];
					float y = (float)jitem["y"];
					ushort time = (ushort)jitem["t"];
					_queueClick.Enqueue(new vClick(x, y, time));
					if (_queueClick.Count > 1000)
					{
						vClick click;
						_queueClick.TryDequeue(out click);
						Log.w(TAG, "click data overflow");
					}
				}
				catch
				{
					Log.w(TAG, "broken click data detected");
				}
			}
		}

		List<vClick> _listClick = new List<vClick>();

		internal vClick DequeueClick()
		{
			vClick click;
			if (_queueClick.TryDequeue(out click))
				return click;
			return null;
		}

		public override void MakeInput(int targetDisplay)
		{
			vClick click;
			while ((click = DequeueClick()) != null)
				_listClick.Add(click);
			vCatchInput_Click._aryClicks[targetDisplay] = _listClick.ToArray();
			if (vCatchInput_Click._aryClicks[targetDisplay].Length != 0)
				_listClick = new List<vClick>();
		}

		public override void LateUpdate(int targetDisplay, vScreen vScrn, vCatchInputModule inputModule)
		{
			// canvas event process
			if (vScrn != null && inputModule != null && inputModule.isActiveAndEnabled)
			{
				vClick[] clicks = vCatchInput_Click.vClicks(targetDisplay);
				if (clicks != null)
				{
					foreach (var c in clicks)
						inputModule.ProcessTouch(targetDisplay, -1, c.x, c.y, vScrn, true, true);
				}
			}

			vClick click;
			while ((click = DequeueClick()) != null)
				_listClick.Add(click);
			vCatchInput_Click._aryClicks[targetDisplay] = null;
		}

		public override void ResetData()
        {
			vClick click;
			while (_queueClick.TryDequeue(out click)) ;

			_queueClick = new ConcurrentQueue<vClick>();
		}

		const string TAG = "vCatchProtocol_Click";
    }

	public static class vCatchInput_Click
	{
		public static vClick[][] _aryClicks = { null, null, null, null, null, null, null, null };

		public static vClick[] vClicks(int targetDisplay)
        {
			if (_aryClicks[targetDisplay] == null)
				vCatchDisplay.vCatchDisplays[targetDisplay].MakeInput(vCatchProtocol.SensorProtocolType.Click);
			if (_aryClicks[targetDisplay] == null)
				_aryClicks[targetDisplay] = new vClick[0];
			return _aryClicks[targetDisplay];
		}
	}
}
