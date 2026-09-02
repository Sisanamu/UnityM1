/****************************************************************************
*                                                                           *
* vCatchUnityUtil.cs                                                        *
*                                                                           *
* made by Willy.Lee                                                         *
*                                                                           *
*    Kee-Wan Lee, 2022-          e-mail : wiljwilj@hotmail.com              *
*                                                                           *
****************************************************************************/
// 2022-08-16 - Add MessageAndQuit
// 2022-09-06 - Remove Click, Drag utilities

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace vCatchStation
{
	static class vCatchUnityUtil
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int x;
			public int y;
		}

		[DllImport("user32.dll")]
		static extern bool GetCursorPos(out POINT lpPoint);
		const int SM_CYSCREEN = 1;
		[DllImport("user32.dll")]
		static extern int GetSystemMetrics(int systemMetric);

		public static Vector3 RelativeMouseAt()
		{
#if UNITY_EDITOR
			var mouseOverWindow = EditorWindow.mouseOverWindow;
			Assembly assembly = typeof(EditorWindow).Assembly;
			//Type type = assembly.GetType("UnityEditor.GameView");
			Type type = assembly.GetType("UnityEditor.PlayModeView");

			int displayID = 0;
			if (type.IsInstanceOfType(mouseOverWindow))
			{
				var displayField = type.GetField("m_TargetDisplay", BindingFlags.NonPublic | BindingFlags.Instance);
				displayID = (int)displayField.GetValue(mouseOverWindow);
			}
			var pos = Input.mousePosition;
			pos[2] = displayID;
			return pos;
#else
			POINT pt;
			GetCursorPos(out pt);
			Vector3 vec = new Vector3();
			vec.x = (float)pt.x;
			vec.y = GetSystemMetrics(SM_CYSCREEN) - 1 - (float)pt.y;
			return Display.RelativeMouseAt(vec);
#endif
		}


		// drag data to simplifier filter
		public static vDrag[] vDragsSimplificationFilter(vDrag[] drags)
        {
			List<vDrag> listDrag = new List<vDrag>();
			Dictionary<int, bool> dicId = new Dictionary<int, bool>();
			foreach (vDrag drag in drags)
            {
				if (!drag.Move)
                {
					listDrag.Add(drag);
					dicId[drag.id] = false;
					continue;
				}

				if (dicId.TryGetValue(drag.id, out bool move) && move)
                {
					for (int idx = listDrag.Count - 1; idx >= 0; idx--)
                    {
						if (listDrag[idx].id == drag.id)
						{
							listDrag[idx] = drag;
							break;
						}
                    }
				}
                else
                {
					listDrag.Add(drag);
					dicId[drag.id] = true;
				}
			}
			return listDrag.ToArray();
		}


		[DllImport("advapi32.dll")]
		static extern int RegDeleteValue(IntPtr hKey, string lpValueName);
		[DllImport("kernel32.dll")]
		static extern bool IsWow64Process(IntPtr processHandle, out bool wow64Process);
		[DllImport("advapi32.dll", CharSet = CharSet.Auto)]
		static extern int RegOpenKeyEx(IntPtr hKey, string subKey,
			int ulOptions, int samDesired, out IntPtr hkResult);
		[DllImport("advapi32.dll")]
		static extern int RegCloseKey(IntPtr hKey);
		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern int RegQueryInfoKey(IntPtr hKey, uint lpClass, uint lpcchClass,
			IntPtr lpReserved, uint lpcSubkey, uint lpcchMaxSubkeyLen,
			uint lpcchMaxClassLen, out uint lpcValues, out uint lpcchMaxValueNameLen,
			out uint lpcbMaxValueLen, IntPtr lpSecurityDescriptor, IntPtr lpftLastWriteTime);
		[DllImport("advapi32.dll", SetLastError = true)]
		static extern uint RegEnumValue(IntPtr hKey, uint dwIndex,
			StringBuilder lpValueName, ref uint lpcValueName,
			IntPtr lpReserved, IntPtr lpType, IntPtr lpData, IntPtr lpcbData);

		const uint ERROR_SUCCESS = 0;

		static IntPtr HKEY_CURRENT_USER = new IntPtr(0x80000001u);

		enum RegWow64Options
		{
			None = 0,
			KEY_WOW64_64KEY = 0x0100,
			KEY_WOW64_32KEY = 0x0200
		}
		enum RegistryRights
		{
			ReadKey = 131097,
			WriteKey = 131078
		}

		static bool RemoveRegistryProfile(IntPtr hAppKey, string entry)
		{
			// necessary to cast away const below
			return RegDeleteValue(hAppKey, entry) == ERROR_SUCCESS;
		}
		public static void RemoveScreenRegistry()
        {
			int rs = (int)RegistryRights.ReadKey | (int)RegistryRights.WriteKey;
			bool isWow64;
			if (IsWow64Process(IntPtr.Zero, out isWow64) && isWow64)
				rs |= (int)RegWow64Options.KEY_WOW64_64KEY;

			string keyApp = "SOFTWARE\\" + Application.companyName + "\\" + Application.productName;
			IntPtr hAppKey = IntPtr.Zero;
			if (RegOpenKeyEx(HKEY_CURRENT_USER, keyApp, 0, rs, out hAppKey) != ERROR_SUCCESS)
				return;

			uint cValues = 0;
			uint cchMaxValue = 0;
			uint cbMaxValueData = 0;
			if (RegQueryInfoKey(hAppKey, 0, 0, IntPtr.Zero, 0, 0, 0,
					out cValues, out cchMaxValue, out cbMaxValueData, IntPtr.Zero, IntPtr.Zero) != ERROR_SUCCESS || cValues <= 0)
            {
				RegCloseKey(hAppKey);
				return;
			}

			for (uint ni = 0; ni < cValues; ni++)
			{
				uint cchValue = 1024;
				StringBuilder valueName = new StringBuilder(1024);
				if (RegEnumValue(hAppKey, ni, valueName, ref cchValue,
					IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) == ERROR_SUCCESS)
                {
					string achValue = valueName.ToString();
					if (achValue.IndexOf("Screenmanager Fullscreen mode Default_") == 0 ||
						achValue.IndexOf("Screenmanager Fullscreen mode_") == 0 ||
						achValue.IndexOf("Screenmanager Resolution Height Default_h") == 0 ||
						achValue.IndexOf("Screenmanager Resolution Height_h") == 0 ||
						achValue.IndexOf("Screenmanager Resolution Use Native Default_h") == 0 ||
						achValue.IndexOf("Screenmanager Resolution Use Native_h") == 0 ||
						achValue.IndexOf("Screenmanager Resolution Width Default_h") == 0 ||
						achValue.IndexOf("Screenmanager Resolution Width_h") == 0 ||
						achValue.IndexOf("Screenmanager Stereo 3D_h") == 0 ||
						achValue.IndexOf("unity.player_session_count_h") == 0 ||
						achValue.IndexOf("unity.player_sessionid_h") == 0 ||
						achValue.IndexOf("UnitySelectMonitor_h") == 0)
					{
						RemoveRegistryProfile(hAppKey, achValue);
					}
				}
			}

			RegCloseKey(hAppKey);
		}


		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern int MessageBox(IntPtr hwnd, String text, String caption, uint type);

		static bool s_bQuit = true;

		public static void MessageAndQuit(string msg, string title = null)
		{
			if (!s_bQuit || msg == null)
				return;
			s_bQuit = false;
#if UNITY_EDITOR
			UnityEngine.Debug.LogError("[" + (title == null ? "vCatchStation" : title) + "] " + msg);
#else
			MessageBox(IntPtr.Zero, msg, title == null ?"vCatchStation" : title, 0);
			Application.Quit();
#endif
		}
	}
}
