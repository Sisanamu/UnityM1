/****************************************************************************
*                                                                           *
* vCatchProtocol_Multimodal_AGJB.cs                                         *
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
    public class vCatchProtocol_MM_AGJB : vCatchProtocol.Protocol
    {
		public vCatchProtocol_MM_AGJB(vCatchBehaviour.Logger log) : base(log)
        {
		}

		public override string name()
        {
            return "multimodal_agjb";
        }

		ConcurrentDictionary<int, vMmPlayer> _dicMmPlayer = new ConcurrentDictionary<int, vMmPlayer>();
		bool _newMmPlayer = true;
		ConcurrentQueue<vMmAGJB> _queueMmAGJB = new ConcurrentQueue<vMmAGJB>();
		public override void OnPacket(JArray json)
		{
			if (json.Count == 0)
			{
				//Middleware - AGJB ÁßÁöµÊ
				return;
			}

			foreach (var obj in json)
			{
				try
				{
					// AGJB data
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

						float ax, ay, az;
						try
						{
							aryi = (JArray)obj["a"];
							ax = (float)aryi[0];
							ay = (float)aryi[1];
							az = (float)aryi[2];
						}
						catch { return; }

						float gx, gy, gz;
						try
						{
							aryi = (JArray)obj["g"];
							gx = (float)aryi[0];
							gy = (float)aryi[1];
							gz = (float)aryi[2];
						}
						catch { return; }

						ushort dtime = 0;
						try
						{
							dtime = (ushort)obj["dt"];
						}
						catch { }

						float jx, jy;
						try
						{
							aryi = (JArray)obj["j"];
							jx = (float)aryi[0];
							jy = (float)aryi[1];
						}
						catch { return; }

						vMmAGJB agjb = new vMmAGJB(i, ax, ay, az, gx, gy, gz, dtime, jx, jy, btns, time);
						_queueMmAGJB.Enqueue(agjb);
						if (_queueMmAGJB.Count > 1000)
						{
							_queueMmAGJB.TryDequeue(out agjb);
							Log.w(TAG, "AGJB data overflow");
						}
						//else Log.i(TAG, "AGJB data cnt: " + _queueMmAGJB.Count);
					}
				}
				catch
				{
					Log.w(TAG, "broken AGJB data detected");
				}
			}
		}

		List<vMmAGJB> _listMmAGJB = new List<vMmAGJB>();

		internal vMmAGJB DequeueMmAGJB()
		{
			vMmAGJB agjb;
			if (_queueMmAGJB.TryDequeue(out agjb))
				return agjb;
			return null;
		}

		public override void MakeInput(int targetDisplay)
		{
			vMmAGJB agjb;
			while ((agjb = DequeueMmAGJB()) != null)
				_listMmAGJB.Add(agjb);
			vCatchInput_MmAGJB._aryMmAGJBs[targetDisplay] = _listMmAGJB.ToArray();
			if (vCatchInput_MmAGJB._aryMmAGJBs[targetDisplay].Length != 0)
				_listMmAGJB = new List<vMmAGJB>();
		}

		public override void LateUpdate(int targetDisplay, vScreen vScrn, vCatchInputModule inputModule)
		{
			vMmAGJB agjb;
			while ((agjb = DequeueMmAGJB()) != null)
				_listMmAGJB.Add(agjb);
			vCatchInput_MmAGJB._aryMmAGJBs[targetDisplay] = null;

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
			vMmAGJB agjb;
			while (_queueMmAGJB.TryDequeue(out agjb)) ;

			_queueMmAGJB = new ConcurrentQueue<vMmAGJB>();
		}

		const string TAG = "vCatchProtocol_MmAGJB";
    }

	public static class vCatchInput_MmAGJB
	{
		public static vMmAGJB[][] _aryMmAGJBs = { null, null, null, null, null, null, null, null };

		public static vMmAGJB[] vMmAGJBs(int targetDisplay)
        {
			if (_aryMmAGJBs[targetDisplay] == null)
				vCatchDisplay.vCatchDisplays[targetDisplay].MakeInput(vCatchProtocol.SensorProtocolType.Multimodal_AGJB);
			if (_aryMmAGJBs[targetDisplay] == null)
				_aryMmAGJBs[targetDisplay] = new vMmAGJB[0];
			return _aryMmAGJBs[targetDisplay];
		}
	}
}
