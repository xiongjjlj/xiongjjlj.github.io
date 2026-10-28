using System;
using I2.Loc;
using UnityEngine;

public class GameObjectLocalization : MonoBehaviour
{
	[Serializable]
	public class LocalizedGameObject
	{
		public string language;

		public GameObject go;
	}

	public GameObject defaultGO;

	public LocalizedGameObject[] gameObjectOverrides;

	private void Awake()
	{
		Singleton<SettingsManager>.Instance.RegisterGameObjectLocalization(this);
	}

	public void UpdateLanguage()
	{
		if (gameObjectOverrides == null)
		{
			return;
		}
		for (int i = 0; i < gameObjectOverrides.Length; i++)
		{
			if (LocalizationManager.CurrentLanguage == gameObjectOverrides[i].language)
			{
				defaultGO.SetActive(value: false);
				gameObjectOverrides[i].go.SetActive(value: true);
			}
			else
			{
				defaultGO.SetActive(value: true);
				gameObjectOverrides[i].go.SetActive(value: false);
			}
		}
	}
}
