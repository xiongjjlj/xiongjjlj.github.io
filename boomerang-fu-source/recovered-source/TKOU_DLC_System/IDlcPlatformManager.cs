using System;
using UnityEngine;

namespace TKOU_DLC_System
{
	public interface IDlcPlatformManager
	{
		event Action OnInstalledDlcsUpdated;

		void Initialize();

		bool IsInstalled(string dlcName);

		void Mount(string dlcName);

		string GetPath(string dlcName, string fileName);

		AssetBundle GetAssetBundle(string dlcName, string assetBundleName);

		AssetBundleCreateRequest GetAssetBundleRequestAsync(string dlcName, string assetBundleName);

		void Unmount(string dlcName);

		string[] GetInstalledDlcNames();

		void OpenInStore(string dlcName);
	}
}
