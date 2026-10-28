using System;
using TMPro;
using UnityEngine;

namespace TKOU_DLC_System
{
	public class DlcPlatformManager : MonoBehaviour
	{
		public IDlcPlatformManager DLCManager;

		private bool _onInstalledDlcsUpdatedPending;

		[SerializeField]
		private UWPDlcConfig _uwpDlcConfig;

		[SerializeField]
		private PS4DlcConfig _ps4DlcConfig;

		[SerializeField]
		private SteamDlcConfig _steamDlcConfig;

		[SerializeField]
		private SwitchDlcConfig _switchDlcConfig;

		[SerializeField]
		private bool showDLCInstalledOverlay = true;

		[SerializeField]
		private GameObject dlcInstalledOverlay;

		[SerializeField]
		private TextMeshProUGUI infoLabel;

		public static DlcPlatformManager Instance { get; private set; }

		public event Action OnInstalledDlcsUpdated;

		private void Awake()
		{
			Debug.Log("DLC Manager Awake()");
			if (Instance == null)
			{
				Instance = this;
				UnityEngine.Object.DontDestroyOnLoad(this);
			}
			else
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
			Debug.Log("Platform: Stand Alone OSX");
			DLCManager = new SteamDlcController(_steamDlcConfig);
			DLCManager.Initialize();
			DLCManager.OnInstalledDlcsUpdated += DLCManager_OnInstalledDlcsUpdated;
		}

		private void Start()
		{
			dlcInstalledOverlay.SetActive(showDLCInstalledOverlay);
		}

		private void OnDestroy()
		{
			DLCManager.OnInstalledDlcsUpdated -= DLCManager_OnInstalledDlcsUpdated;
		}

		private void Update()
		{
			if (_onInstalledDlcsUpdatedPending)
			{
				_onInstalledDlcsUpdatedPending = false;
				OnInstalledDlcsUpdated?.Invoke();
			}
		}

		public string[] GetInstalledDlcs()
		{
			return DLCManager.GetInstalledDlcNames();
		}

		public bool IsInstalled(string dlcName, bool debugLog)
		{
			if (DLCManager.IsInstalled(dlcName))
			{
				infoLabel.text = "DLC " + dlcName + " installed";
			}
			else if (debugLog)
			{
				Debug.Log("DLC not installed");
				string[] installedDlcs = GetInstalledDlcs();
				if (installedDlcs.Length != 0)
				{
					Debug.Log("List of installed DLCs: ");
				}
				string[] array = installedDlcs;
				foreach (string text in array)
				{
					Debug.Log("Installed DLC: " + text + " ");
				}
			}
			return DLCManager.IsInstalled(dlcName);
		}

		public void OpenInStore(string dlcName)
		{
			DLCManager.OpenInStore(dlcName);
		}

		public void Mount(string dlcName)
		{
			DLCManager.Mount(dlcName);
		}

		public string GetPath(string dlcName, string assetBundleName)
		{
			return DLCManager.GetPath(dlcName, assetBundleName);
		}

		public AssetBundle GetAssetBundle(string dlcName, string assetBundleName)
		{
			return DLCManager.GetAssetBundle(dlcName, assetBundleName);
		}

		public AssetBundleCreateRequest GetAssetBundleRequestAsync(string dlcName, string assetBundleName)
		{
			return DLCManager.GetAssetBundleRequestAsync(dlcName, assetBundleName);
		}

		public void Unmount(string dlcName)
		{
			DLCManager.Unmount(dlcName);
		}

		private void DLCManager_OnInstalledDlcsUpdated()
		{
			_onInstalledDlcsUpdatedPending = true;
		}
	}
}
