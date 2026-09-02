/****************************************************************************
*                                                                           *
* vCatchProtocol_Multimodal_AGB.cs                                          *
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
    public class vCatchProtocol_MM_AGB : vCatchProtocol.Protocol
    {
		public vCatchProtocol_MM_AGB(vCatchBehaviour.Logger log) : base(log)
        {
		}

		public override string name()
        {
            return "multimodal_agb";
        }

		ConcurrentDictionary<int, vMmPlayer> _dicMmPlayer = new ConcurrentDictionary<int, vMmPlayer>();
		bool _newMmPlayer = true;
		ConcurrentQueue<vMmAGB> _queueMmAGB = new ConcurrentQueue<vMmAGB>();
		public override void OnPacket(JArray json)
		{
			if (json.Count == 0)
			{
				//Middleware - AGB ÁßÁöµÊ
				return;
			}

			foreach (var obj in json)
			{
				try
				{
					// AGB data
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

						vMmAGB agb = new vMmAGB(i, ax, ay, az, gx, gy, gz, dtime, btns, time);
						_queueMmAGB.Enqueue(agb);
						if (_queueMmAGB.Count > 1000)
						{
							_queueMmAGB.TryDequeue(out agb);
							Log.w(TAG, "AGB data overflow");
						}
						//else Log.i(TAG, "AGB data cnt: " + _queueMmAGB.Count);
					}
				}
				catch
				{
					Log.w(TAG, "broken AGB data detected");
				}
			}
		}

		List<vMmAGB> _listMmAGB = new List<vMmAGB>();

		internal vMmAGB DequeueMmAGB()
		{
			vMmAGB agb;
			if (_queueMmAGB.TryDequeue(out agb))
				return agb;
			return null;
		}

		public override void MakeInput(int targetDisplay)
		{
			vMmAGB agb;
			while ((agb = DequeueMmAGB()) != null)
				_listMmAGB.Add(agb);
			vCatchInput_MmAGB._aryMmAGBs[targetDisplay] = _listMmAGB.ToArray();
			if (vCatchInput_MmAGB._aryMmAGBs[targetDisplay].Length != 0)
				_listMmAGB = new List<vMmAGB>();
		}

		public override void LateUpdate(int targetDisplay, vScreen vScrn, vCatchInputModule inputModule)
		{
			vMmAGB agb;
			while ((agb = DequeueMmAGB()) != null)
				_listMmAGB.Add(agb);
			vCatchInput_MmAGB._aryMmAGBs[targetDisplay] = null;

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
			vMmAGB agb;
			while (_queueMmAGB.TryDequeue(out agb)) ;

			_queueMmAGB = new ConcurrentQueue<vMmAGB>();
		}

		const string TAG = "vCatchProtocol_MmAGB";
    }

	public static class vCatchInput_MmAGB
	{
		public static vMmAGB[][] _aryMmAGBs = { null, null, null, null, null, null, null, null };

		public static vMmAGB[] vMmAGBs(int targetDisplay)
        {
			if (_aryMmAGBs[targetDisplay] == null)
				vCatchDisplay.vCatchDisplays[targetDisplay].MakeInput(vCatchProtocol.SensorProtocolType.Multimodal_AGB);
			if (_aryMmAGBs[targetDisplay] == null)
				_aryMmAGBs[targetDisplay] = new vMmAGB[0];
			return _aryMmAGBs[targetDisplay];
		}
	}
}
