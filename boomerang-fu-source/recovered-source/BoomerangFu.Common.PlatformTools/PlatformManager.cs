using UnityEngine;

namespace BoomerangFu.Common.PlatformTools
{
	public class PlatformManager : Singleton<PlatformManager>
	{
		public IPlatformController platformController;

		public IPlatformStorageManager platformStorageManager;

		private void Awake()
		{
			Object.DontDestroyOnLoad(base.gameObject);
		}

		private void Start()
		{
			LoadPlatfromController();
			LoadSaveDataManager();
		}

		private void LoadPlatfromController()
		{
		}

		private void LoadSaveDataManager()
		{
		}

		public void ChangeMainUser()
		{
			if (platformController != null)
			{
				platformController.ChangeMainUser();
			}
		}
	}
}
