using System;
using System.Collections.Generic;
using UnityEngine;

namespace TKOU_DLC_System
{
	[CreateAssetMenu(fileName = "SteamDlcConfig", menuName = "ScriptableObjects/SteamDlcConfig")]
	public class SteamDlcConfig : ScriptableObject
	{
		[Serializable]
		public class DlcInfo
		{
			public string Name;

			public string appID;
		}

		public List<DlcInfo> dlcs;
	}
}
