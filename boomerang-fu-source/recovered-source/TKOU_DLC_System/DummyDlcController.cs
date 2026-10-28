using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TKOU_DLC_System
{
	public class DummyDlcController : IDlcPlatformManager
	{
		public event Action OnInstalledDlcsUpdated = delegate
		{
		};

		public void Initialize()
		{
		}

		public string[] GetInstalledDlcNames()
		{
			return (from d in Directory.GetDirectories(Application.streamingAssetsPath + "/DummyDlcs")
				select Path.GetFileName(d)).ToArray();
		}

		public bool IsInstalled(string dlcName)
		{
			return Directory.Exists(Application.streamingAssetsPath + "/DummyDlcs/" + dlcName);
		}

		public void OpenInStore(string dlcName)
		{
			Debug.Log("It's DummyDLCManager. DLC cannot be opened in store");
		}

		public void Mount(string dlcName)
		{
		}

		public string GetPath(string dlcName, string fileName)
		{
			return Application.streamingAssetsPath + "/DummyDlcs/" + dlcName + "/" + fileName;
		}

		public AssetBundle GetAssetBundle(string dlcName, string assetBundleName)
		{
			string path = Application.streamingAssetsPath + "/DummyDlcs/" + dlcName + "/" + assetBundleName;
			if (File.Exists(path))
			{
				return AssetBundle.LoadFromFile(path);
			}
			return null;
		}

		public AssetBundleCreateRequest GetAssetBundleRequestAsync(string dlcName, string assetBundleName)
		{
			string path = Application.streamingAssetsPath + "/DummyDlcs/" + dlcName + "/" + assetBundleName;
			if (File.Exists(path))
			{
				return AssetBundle.LoadFromFileAsync(path);
			}
			return null;
		}

		public void Unmount(string dlcName)
		{
		}
	}
}
