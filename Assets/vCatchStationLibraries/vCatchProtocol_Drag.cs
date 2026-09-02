/****************************************************************************
*                                                                           *
* vCatchProtocol_Drag.cs                                                    *
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
    public class vCatchProtocol_Drag : vCatchProtocol.Protocol
    {
		public vCatchProtocol_Drag(vCatchBehaviour.Logger log) : base(log)
		{
		}

		public override string name()
        {
            return "drag";
        }

		ConcurrentQueue<vDrag> _queueDrag = new ConcurrentQueue<vDrag>();
		public override void OnPacket(JArray json)
		{
            if (json.Count == 0)
            {
                //Middleware - drag ÁßÁöµÊ
                return;
            }

            foreach (var jitem in json)
            {
                try
                {
                    // drag data
                    int i = (int)jitem["i"];
                    int s = (int)jitem["s"];
                    float x = (float)jitem["x"];
                    float y = (float)jitem["y"];
                    float r = (float)jitem["r"];
                    ushort time = (ushort)jitem["t"];
                    _queueDrag.Enqueue(new vDrag(i, s, x, y, r, time));
                    if (_queueDrag.Count > 1000)
                    {
                        vDrag drag;
                        _queueDrag.TryDequeue(out drag);
                        Log.w(TAG, "drag data overflow");
                    }
                }
                catch
                {
                    Log.w(TAG, "broken drag data detected");
                }
            }
		}

        List<vDrag> _listDrag = new List<vDrag>();

		internal vDrag DequeueDrag()
		{
            vDrag drag;
            if (_queueDrag.TryDequeue(out drag))
                return drag;
            return null;
        }

        public override void MakeInput(int targetDisplay)
        {
            vDrag drag;
            while ((drag = DequeueDrag()) != null)
                _listDrag.Add(drag);
            vCatchInput_Drag._aryDrags[targetDisplay] = _listDrag.ToArray();
            if (vCatchInput_Drag._aryDrags[targetDisplay].Length != 0)
                _listDrag = new List<vDrag>();
        }

        public override void LateUpdate(int targetDisplay, vScreen vScrn, vCatchInputModule inputModule)
		{
            // canvas event process
            if (vScrn != null && inputModule != null && inputModule.isActiveAndEnabled)
            {
                vDrag[] drags = vCatchInput_Drag.vDrags(targetDisplay);
                if (drags != null)
                {
                    foreach (var d in drags)
                        inputModule.ProcessTouch(targetDisplay, d.id, d.x, d.y, vScrn, d.Down, d.Up);
                }
            }

            vDrag drag;
			while ((drag = DequeueDrag()) != null)
                _listDrag.Add(drag);
			vCatchInput_Drag._aryDrags[targetDisplay] = null;
        }

        public override void ResetData()
        {
            vDrag drag;
			while (_queueDrag.TryDequeue(out drag)) ;

            _queueDrag = new ConcurrentQueue<vDrag>();
		}

        const string TAG = "vCatchProtocol_Drag";
    }

	public static class vCatchInput_Drag
	{
		public static vDrag[][] _aryDrags = { null, null, null, null, null, null, null, null };

		public static vDrag[] vDrags(int targetDisplay)
        {
			if (_aryDrags[targetDisplay] == null)
				vCatchDisplay.vCatchDisplays[targetDisplay].MakeInput(vCatchProtocol.SensorProtocolType.Drag);
			if (_aryDrags[targetDisplay] == null)
                _aryDrags[targetDisplay] = new vDrag[0];
			return _aryDrags[targetDisplay];
		}
	}
}
