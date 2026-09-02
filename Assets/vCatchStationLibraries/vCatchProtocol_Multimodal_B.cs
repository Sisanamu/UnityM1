/****************************************************************************
*                                                                           *
* vCatchProtocol_Multimodal_B.cs                                            *
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
    public class vCatchProtocol_MM_B : vCatchProtocol.Protocol
    {
		public vCatchProtocol_MM_B(vCatchBehaviour.Logger log) : base(log)
        {
		}

		public override string name()
        {
            return "multimodal_b";
        }

		ConcurrentDictionary<int, vMmPlayer> _dicMmPlayer = new ConcurrentDictionary<int, vMmPlayer>();
		bool _newMmPlayer = true;
		ConcurrentQueue<vMmB> _queueMmB = new ConcurrentQueue<vMmB>();
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

						vMmB b = new vMmB(i, btns, time);
						_queueMmB.Enqueue(b);
						if (_queueMmB.Count > 1000)
						{
							_queueMmB.TryDequeue(out b);
							Log.w(TAG, "B data overflow");
						}
						//else Log.i(TAG, "B data cnt: " + _queueMmB.Count);
					}
				}
				catch
				{
					Log.w(TAG, "broken B data detected");
				}
			}
		}

		List<vMmB> _listMmB = new List<vMmB>();

		internal vMmB DequeueMmB()
		{
			vMmB b;
			if (_queueMmB.TryDequeue(out b))
				return b;
			return null;
		}

		public override void MakeInput(int targetDisplay)
		{
			vMmB b;
			while ((b = DequeueMmB()) != null)
				_listMmB.Add(b);
			vCatchInput_MmB._aryMmBs[targetDisplay] = _listMmB.ToArray();
			if (vCatchInput_MmB._aryMmBs[targetDisplay].Length != 0)
				_listMmB = new List<vMmB>();
		}

		public override void LateUpdate(int targetDisplay, vScreen vScrn, vCatchInputModule inputModule)
		{
			vMmB b;
			while ((b = DequeueMmB()) != null)
				_listMmB.Add(b);
			vCatchInput_MmB._aryMmBs[targetDisplay] = null;

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
			vMmB b;
			while (_queueMmB.TryDequeue(out b)) ;

			_queueMmB = new ConcurrentQueue<vMmB>();
		}

		const string TAG = "vCatchProtocol_MmB";
    }

	public static class vCatchInput_MmB
	{
		public static vMmB[][] _aryMmBs = { null, null, null, null, null, null, null, null };

		public static vMmB[] vMmBs(int targetDisplay)
        {
			if (_aryMmBs[targetDisplay] == null)
				vCatchDisplay.vCatchDisplays[targetDisplay].MakeInput(vCatchProtocol.SensorProtocolType.Multimodal_B);
			if (_aryMmBs[targetDisplay] == null)
				_aryMmBs[targetDisplay] = new vMmB[0];
			return _aryMmBs[targetDisplay];
		}
	}
}
