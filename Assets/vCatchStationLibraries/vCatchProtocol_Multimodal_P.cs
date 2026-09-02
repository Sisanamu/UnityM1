/****************************************************************************
*                                                                           *
* vCatchProtocol_Multimodal_P.cs                                            *
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
    public class vCatchProtocol_MM_P : vCatchProtocol.Protocol
    {
		public vCatchProtocol_MM_P(vCatchBehaviour.Logger log) : base(log)
        {
		}

		public override string name()
        {
            return "multimodal_p";
        }

		ConcurrentDictionary<int, vMmPlayer> _dicMmPlayer = new ConcurrentDictionary<int, vMmPlayer>();
		bool _newMmPlayer = true;
		ConcurrentQueue<vMmP> _queueMmP = new ConcurrentQueue<vMmP>();
		public override void OnPacket(JArray json)
		{
			if (json.Count == 0)
			{
				//Middleware - B ÁßÁöµÊ
				return;
			}

			foreach (var obj in json)
			{
				try
				{
					// B data
					int i = (int)obj["i"];
					if (i == 0 || i == -1)
					{
						var pitem = obj["p"];
						int id = (int)pitem["i"];

						vMmPlayer player;
						if (i == -1)
						{
							_dicMmPlayer.TryRemove(id, out player);
							_newMmPlayer = true;
							continue;
						}

						string parts = (string)pitem["parts"];

						float? batlevel = null;
						try
						{
							batlevel = (float)pitem["batlevel"];
						}
						catch { }

						bool? charging = null;
						try
						{
							charging = (bool)pitem["charging"];
						}
						catch { }

						//Log.i(TAG, "mmplayer info {0}", id);
						if (!_dicMmPlayer.TryGetValue(id, out player) || player == null)
						{
							player = new vMmPlayer(id, parts, batlevel, charging);
							_dicMmPlayer.TryAdd(id, player);
							_newMmPlayer = true;
						}
						else
						{
							if (player.parts != parts ||
								player.batlevel != batlevel ||
								player.charging != charging)
							{
								vMmPlayer playerNew = new vMmPlayer(id, parts, batlevel, charging);
								_dicMmPlayer.TryUpdate(id, playerNew, player);
								_newMmPlayer = true;
							}
						}
					}
					else
					{
						int p;
						ushort time;
						try
						{
							p = (int)obj["p"];
							time = (ushort)obj["t"];
						}
						catch { return; }

						vMmP mmp = new vMmP(i, p, time);
						_queueMmP.Enqueue(mmp);
						if (_queueMmP.Count > 1000)
						{
							_queueMmP.TryDequeue(out mmp);
							Log.w(TAG, "P data overflow");
						}
						//else Log.i(TAG, "P data cnt: " + _queueMmP.Count);
					}
				}
				catch
				{
					Log.w(TAG, "broken P data detected");
				}
			}
		}

		List<vMmP> _listMmP = new List<vMmP>();

		internal vMmP DequeueMmP()
		{
			vMmP p;
			if (_queueMmP.TryDequeue(out p))
				return p;
			return null;
		}

		public override void MakeInput(int targetDisplay)
		{
			vMmP p;
			while ((p = DequeueMmP()) != null)
				_listMmP.Add(p);
			vCatchInput_MmP._aryMmPs[targetDisplay] = _listMmP.ToArray();
			if (vCatchInput_MmP._aryMmPs[targetDisplay].Length != 0)
				_listMmP = new List<vMmP>();
		}

		public override void LateUpdate(int targetDisplay, vScreen vScrn, vCatchInputModule inputModule)
		{
			vMmP p;
			while ((p = DequeueMmP()) != null)
				_listMmP.Add(p);
			vCatchInput_MmP._aryMmPs[targetDisplay] = null;

			if (_newMmPlayer)
            {
				vMmPlayer[] aryPlayers = new vMmPlayer[_dicMmPlayer.Values.Count];
				_dicMmPlayer.Values.CopyTo(aryPlayers, 0);

				_newMmPlayer = false;
				vCatchInput_MmPlayer._aryMmPlayers[targetDisplay] = aryPlayers;
				vCatchInput_MmPlayer.fireEvent();
			}
		}

		public override void ResetData()
        {
			vMmP p;
			while (_queueMmP.TryDequeue(out p)) ;

			_queueMmP = new ConcurrentQueue<vMmP>();
		}

		const string TAG = "vCatchProtocol_MmP";
    }

	public static class vCatchInput_MmP
	{
		public static vMmP[][] _aryMmPs = { null, null, null, null, null, null, null, null };

		public static vMmP[] vMmPs(int targetDisplay)
        {
			if (_aryMmPs[targetDisplay] == null)
				vCatchDisplay.vCatchDisplays[targetDisplay].MakeInput(vCatchProtocol.SensorProtocolType.Multimodal_P);
			if (_aryMmPs[targetDisplay] == null)
				_aryMmPs[targetDisplay] = new vMmP[0];
			return _aryMmPs[targetDisplay];
		}
	}
}
