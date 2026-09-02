/****************************************************************************
*                                                                           *
* vCatchProtocol_Multimodal_Player.cs                                       *
*                                                                           *
* made by Willy.Lee                                                         *
*                                                                           *
*    Kee-Wan Lee, 2022-          e-mail : wiljwilj@hotmail.com              *
*                                                                           *
****************************************************************************/

using System.Collections.Generic;

namespace vCatchStation
{
	public interface vCatchInput_iMmPlayer
    {
		void vCatchInput_MmPlayer_OnMmPlayer();
	}

	public static class vCatchInput_MmPlayer
	{
		public static vMmPlayer[][] _aryMmPlayers = { null, null, null, null, null, null, null, null };

		public static vMmPlayer[] vMmPlayers(int targetDisplay)
		{
			if (_aryMmPlayers[targetDisplay] == null)
				_aryMmPlayers[targetDisplay] = new vMmPlayer[0];
			return _aryMmPlayers[targetDisplay];
		}

		static List<vCatchInput_iMmPlayer> _listListenr = new List<vCatchInput_iMmPlayer>();

		public static void AddEventListener(vCatchInput_iMmPlayer listener)
        {
			_listListenr.Add(listener);
		}

		public static void fireEvent()
        {
			_listListenr.ForEach(delegate (vCatchInput_iMmPlayer iMmPlayer)
			{
				iMmPlayer.vCatchInput_MmPlayer_OnMmPlayer();
			});
		}
	}
}
