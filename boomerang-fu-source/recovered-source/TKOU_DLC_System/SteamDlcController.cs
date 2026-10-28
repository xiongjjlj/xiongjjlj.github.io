using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using UnityEngine;

namespace TKOU_DLC_System
{
	public class SteamDlcController : IDlcPlatformManager
	{
		private SteamDlcConfig _config;

		private List<string> _intalledDlcLabels = new List<string>();

		private Dictionary<string, string> _namePackageDict = new Dictionary<string, string>();

		public event Action OnInstalledDlcsUpdated = delegate
		{
		};

		public SteamDlcController(SteamDlcConfig config)
		{
			_config = config;
		}

		private void PrepareInstalledDlcList()
		{
			_namePackageDict.Clear();
			for (int i = 0; i < _config.dlcs.Count; i++)
			{
				string appID = _config.dlcs[i].appID;
				try
				{
					uint.Parse(appID);
					if (SteamApps.BIsDlcInstalled(new AppId_t(uint.Parse(_config.dlcs[i].appID))))
					{
						_intalledDlcLabels.Add(_config.dlcs[i].Name);
					}
					else
					{
						Debug.Log("DLC not registered with Steam: " + _config.dlcs[i].Name);
					}
				}
				catch (FormatException)
				{
					Console.WriteLine("Unable to parse '" + appID + "'");
				}
			}
		}

		public void Initialize()
		{
			PrepareInstalledDlcList();
		}

		public void Mount(string dlcName)
		{
		}

		public string GetPath(string dlcName, string fileName)
		{
			if (_namePackageDict.ContainsKey(dlcName))
			{
				return _namePackageDict[dlcName] + "/" + fileName;
			}
			Debug.LogError(dlcName + " " + fileName);
			Debug.LogError("Optional package " + dlcName + " is not in dictionary");
			return null;
		}

		public AssetBundle GetAssetBundle(string dlcName, string assetBundleName)
		{
			string path = GetPath(dlcName, assetBundleName);
			AssetBundle result = null;
			if (!string.IsNullOrEmpty(path))
			{
				result = AssetBundle.LoadFromFile(path);
			}
			return result;
		}

		public AssetBundleCreateRequest GetAssetBundleRequestAsync(string dlcName, string assetBundleName)
		{
			string path = GetPath(dlcName, assetBundleName);
			AssetBundleCreateRequest result = null;
			if (!string.IsNullOrEmpty(path))
			{
				result = AssetBundle.LoadFromFileAsync(path);
			}
			return result;
		}

		public void Unmount(string dlcName)
		{
		}

		public string[] GetInstalledDlcNames()
		{
			return _intalledDlcLabels.ToArray();
		}

		public bool IsInstalled(string dlcName)
		{
			return _intalledDlcLabels.Contains(dlcName);
		}

		private string GetEntitlementLabel(string dlcName)
		{
			SteamDlcConfig.DlcInfo dlcInfo = _config.dlcs.FirstOrDefault((SteamDlcConfig.DlcInfo dlc) => dlc.Name == dlcName);
			if (dlcInfo != null)
			{
				return dlcInfo.appID;
			}
			Debug.LogError("Coudln't find label for dlcName: " + dlcName);
			return "";
		}

		public string GetDlcWebPage(string id)
		{
			return "https://store.steampowered.com/app/" + id;
		}

		public void OpenInStore(string dlcName)
		{
			string entitlementLabel = GetEntitlementLabel(dlcName);
			Application.OpenURL(GetDlcWebPage(entitlementLabel));
		}
	}
}
