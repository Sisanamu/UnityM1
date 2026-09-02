/****************************************************************************
*                                                                           *
* vCatchProtocol_Multimodal_JB.cs                                           *
*                                                                           *
* made by Willy.Lee                                                         *
*                                                                           *
*    Kee-Wan Lee, 2022-          e-mail : wiljwilj@hotmail.com              *
*                                                                           *
****************************************************************************/
// 2024-03-11 - Removed interpolation data

using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace vCatchStation
{
    public class vCatchProtocol_MM_JB : vCatchProtocol.Protocol
    {
		public vCatchProtocol_MM_JB(vCatchBehaviour.Logger log) : base(log)
        {
		}

		public override string name()
        {
            return "multimodal_jb";
        }

		ConcurrentDictionary<int, vMmPlayer> _dicMmPlayer = new ConcurrentDictionary<int, vMmPlayer>();
		bool _newMmPlayer = true;
		ConcurrentQueue<vMmJB> _queueMmJB = new ConcurrentQueue<vMmJB>();
		public override void OnPacket(JArray json)
		{
			if (json.Count == 0)
			{
				//Middleware - JB ÁßÁöµÊ
				return;
			}

			foreach (var obj in json)
			{
				try
				{
					// JB data
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

						//Log.i(TAG, "mmplayer info {0} {1} {2} {3}", id, parts, batlevel, charging);
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
						int btns;
						ushort time;
						try
						{
							btns = (int)obj["b"];
							time = (ushort)obj["t"];
						}
						catch { return; }

						JArray aryi;

						float jx, jy;
						try
						{
							aryi = (JArray)obj["j"];
							jx = (float)aryi[0];
							jy = (float)aryi[1];
						}
						catch { return; }

						vMmJB jb = new vMmJB(i, jx, jy, btns, time);
						_queueMmJB.Enqueue(jb);
						if (_queueMmJB.Count > 1000)
						{
							_queueMmJB.TryDequeue(out jb);
							Log.w(TAG, "JB data overflow");
						}
						//else Log.i(TAG, "JB data cnt: " + _queueMmJB.Count);
					}
				}
				catch
				{
					Log.w(TAG, "broken JB data detected");
				}
			}
		}

		List<vMmJB> _listMmJB = new List<vMmJB>();

		internal vMmJB DequeueMmJB()
		{
			vMmJB jb;
			if (_queueMmJB.TryDequeue(out jb))
				return jb;
			return null;
		}

		public override void MakeInput(int targetDisplay)
		{
			vMmJB jb;
			while ((jb = DequeueMmJB()) != null)
				_listMmJB.Add(jb);
			vCatchInput_MmJB._aryMmJBs[targetDisplay] = _listMmJB.ToArray();
			if (vCatchInput_MmJB._aryMmJBs[targetDisplay].Length != 0)
				_listMmJB = new List<vMmJB>();
		}

		public override void LateUpdate(int targetDisplay, vScreen vScrn, vCatchInputModule inputModule)
		{
			vMmJB jb;
			while ((jb = DequeueMmJB()) != null)
				_listMmJB.Add(jb);
			vCatchInput_MmJB._aryMmJBs[targetDisplay] = null;

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
			vMmJB jb;
			while (_queueMmJB.TryDequeue(out jb)) ;

			_queueMmJB = new ConcurrentQueue<vMmJB>();
		}

		const string TAG = "vCatchProtocol_MmJB";
    }

	public static class vCatchInput_MmJB
	{
		public static vMmJB[][] _aryMmJBs = { null, null, null, null, null, null, null, null };

		public static vMmJB[] vMmJBs(int targetDisplay)
        {
			if (_aryMmJBs[targetDisplay] == null)
				vCatchDisplay.vCatchDisplays[targetDisplay].MakeInput(vCatchProtocol.SensorProtocolType.Multimodal_JB);
			if (_aryMmJBs[targetDisplay] == null)
				_aryMmJBs[targetDisplay] = new vMmJB[0];
			return _aryMmJBs[targetDisplay];
		}
	}
}
