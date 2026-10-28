using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UIMatchSettingMapsModule : UIMatchSettingModule
{
	public UISliderButton selectedMaps;

	public UIMapToggle mapTogglePrefab;

	public List<UIMapToggle> mapToggles;

	public Transform originalMapsRoot;

	public Transform dlcTwoMapsRoot;

	public UIAutoScroller autoScroller;

	public UIFillButton restoreDefaultPrompt;

	[SerializeField]
	private float restoreDefaultCharge;

	private static float restoreDefaultDuration = 1f;

	[SerializeField]
	private bool restoreDefaultInputBlocked;

	public GameObject selectPrompt;

	public CanvasGroup errorMessage;

	private int numMapsForCurrentMatchType;

	private IEnumerator errorMessageRoutine;

	public override void Enable()
	{
		CreateMapToggles();
		LoadValuesFromDisk();
		base.Enable();
		Singleton<UIManager>.Instance.SetSelectedGameObject(initialSelection);
		RectTransform component = GetComponent<RectTransform>();
		if (component != null)
		{
			component.localScale = Vector3.one;
			component.DOScale(0.9f, 0.1f).From(isRelative: false);
		}
		UIMenuLobby uIMenuLobby = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Lobby) as UIMenuLobby;
		if (uIMenuLobby != null && uIMenuLobby.uiPlayersCanvas != null)
		{
			uIMenuLobby.uiPlayersCanvas.gameObject.SetActive(value: false);
		}
		if (restoreDefaultPrompt != null)
		{
			restoreDefaultPrompt.gameObject.SetActive(value: true);
			restoreDefaultPrompt.InitButtonPrompt(Singleton<UIManager>.Instance.GetCurrentMasterPlayer());
		}
		if (selectPrompt != null)
		{
			selectPrompt.SetActive(value: false);
		}
	}

	public override void Disable()
	{
		base.Disable();
		UIMenuLobby uIMenuLobby = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Lobby) as UIMenuLobby;
		if (uIMenuLobby != null && uIMenuLobby.uiPlayersCanvas != null)
		{
			uIMenuLobby.uiPlayersCanvas.gameObject.SetActive(value: true);
		}
		if (restoreDefaultPrompt != null)
		{
			restoreDefaultPrompt.gameObject.SetActive(value: false);
		}
		if (selectPrompt != null)
		{
			selectPrompt.SetActive(value: true);
		}
	}

	private void OnEnable()
	{
		selectedMaps.OnValueChanged.AddListener(OnChangeSelectedMaps);
		for (int i = 0; i < mapToggles.Count; i++)
		{
			mapToggles[i].onValueChanged.AddListener(OnToggleMap);
		}
		errorMessage.DOComplete();
		errorMessage.gameObject.SetActive(value: false);
		errorMessage.alpha = 0f;
	}

	private void OnDisable()
	{
		selectedMaps.OnValueChanged.RemoveListener(OnChangeSelectedMaps);
		for (int i = 0; i < mapToggles.Count; i++)
		{
			mapToggles[i].onValueChanged.RemoveListener(OnToggleMap);
		}
	}

	private void Update()
	{
		if (restoreDefaultInputBlocked && Singleton<UIManager>.Instance.GetCurrentMasterPlayer().GetButtonUp("UIRestore"))
		{
			restoreDefaultInputBlocked = false;
		}
		if (!restoreDefaultInputBlocked && Singleton<UIManager>.Instance.GetCurrentMasterPlayer().GetButton("UIRestore"))
		{
			if (restoreDefaultPrompt != null)
			{
				restoreDefaultCharge += Time.unscaledDeltaTime;
				if (restoreDefaultCharge >= restoreDefaultDuration)
				{
					restoreDefaultCharge = 0f;
					restoreDefaultInputBlocked = true;
					restoreDefaultPrompt.Flash();
					UIResponse_RestoreDefault();
					Singleton<UIManager>.Instance.VibrateMasterPlayer(1, 1f, 0.5f);
				}
			}
		}
		else
		{
			restoreDefaultCharge = 0f;
		}
		restoreDefaultPrompt.SetFill(restoreDefaultCharge / restoreDefaultDuration);
	}

	private void CreateMapToggles()
	{
		for (int num = mapToggles.Count - 1; num >= 0; num--)
		{
			Object.Destroy(mapToggles[num].gameObject);
		}
		mapToggles.Clear();
		numMapsForCurrentMatchType = 0;
		foreach (LevelAsset item in Singleton<LevelManager>.Instance.GetLevelAssetsByDLCID(DownloadableContentID.None))
		{
			if (item.supportedMatchTypes.Contains(Singleton<SettingsManager>.Instance.matchType) && item.complexity != LevelAsset.Complexity.Tutorial)
			{
				UIMapToggle uIMapToggle = Object.Instantiate(mapTogglePrefab, Vector3.zero, Quaternion.identity, originalMapsRoot);
				uIMapToggle.SetLevelAsset(item);
				mapToggles.Add(uIMapToggle);
				numMapsForCurrentMatchType++;
			}
		}
		foreach (LevelAsset item2 in Singleton<LevelManager>.Instance.GetLevelAssetsByDLCID(DownloadableContentID.JustDessertsPack))
		{
			if (item2.supportedMatchTypes.Contains(Singleton<SettingsManager>.Instance.matchType))
			{
				UIMapToggle uIMapToggle2 = Object.Instantiate(mapTogglePrefab, Vector3.zero, Quaternion.identity, dlcTwoMapsRoot);
				uIMapToggle2.SetLevelAsset(item2);
				mapToggles.Add(uIMapToggle2);
				numMapsForCurrentMatchType++;
			}
		}
		autoScroller.selectables.AddRange(mapToggles);
	}

	private void OnToggleMap(bool value)
	{
		if (!value)
		{
			Singleton<SettingsManager>.Instance.UseCustomMaps = true;
		}
		WriteMapToggleValuesToDisk();
		Singleton<SettingsManager>.Instance.UseCustomMaps = Singleton<SettingsManager>.Instance.NumDisabledMapsForCurrentMatchType > 0;
		selectedMaps.SetValue(Singleton<SettingsManager>.Instance.UseCustomMaps ? 1 : 0, sendCallback: false);
	}

	private void WriteMapToggleValuesToDisk()
	{
		if (!AreMapsValid())
		{
			return;
		}
		for (int i = 0; i < mapToggles.Count; i++)
		{
			int item = Singleton<LevelManager>.Instance.levelAssets.IndexOf(mapToggles[i].levelAsset);
			List<int> list = Singleton<SettingsManager>.Instance.MatchSettings.disabledDeathmatchLevels;
			if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc)
			{
				list = Singleton<SettingsManager>.Instance.MatchSettings.disabledGoldenDiscLevels;
			}
			if (mapToggles[i].isOn)
			{
				if (list.Contains(item))
				{
					list.Remove(item);
				}
			}
			else if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
	}

	private void ReadMapToggleValues()
	{
		List<int> list = Singleton<SettingsManager>.Instance.MatchSettings.disabledDeathmatchLevels;
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc)
		{
			list = Singleton<SettingsManager>.Instance.MatchSettings.disabledGoldenDiscLevels;
		}
		for (int i = 0; i < mapToggles.Count; i++)
		{
			if (!mapToggles[i].IsUnlocked)
			{
				mapToggles[i].SetIsOnWithoutNotify(value: false);
			}
			else if (Singleton<SettingsManager>.Instance.UseCustomMaps)
			{
				int item = Singleton<LevelManager>.Instance.levelAssets.IndexOf(mapToggles[i].levelAsset);
				mapToggles[i].SetIsOnWithoutNotify(!list.Contains(item));
			}
			else
			{
				mapToggles[i].SetIsOnWithoutNotify(value: true);
			}
		}
	}

	private void OnChangeSelectedMaps(int value)
	{
		switch (value)
		{
		case 0:
			Singleton<SettingsManager>.Instance.UseCustomMaps = false;
			break;
		case 1:
			Singleton<SettingsManager>.Instance.UseCustomMaps = true;
			break;
		}
		ReadMapToggleValues();
	}

	public void LoadValuesFromDisk()
	{
		ReadMapToggleValues();
		selectedMaps.SetValue(Singleton<SettingsManager>.Instance.UseCustomMaps ? 1 : 0, sendCallback: false);
	}

	public bool AreMapsValid()
	{
		if (!Singleton<SettingsManager>.Instance.UseCustomMaps)
		{
			return true;
		}
		int num = 0;
		for (int i = 0; i < mapToggles.Count; i++)
		{
			if (!mapToggles[i].isOn)
			{
				num++;
			}
		}
		if (num < numMapsForCurrentMatchType)
		{
			return true;
		}
		return false;
	}

	public void ShowErrorMessage()
	{
		if (!errorMessage.gameObject.activeSelf)
		{
			if (errorMessageRoutine != null)
			{
				StopCoroutine(errorMessageRoutine);
			}
			errorMessageRoutine = ErrorMessageCoroutine();
			StartCoroutine(errorMessageRoutine);
		}
	}

	private IEnumerator ErrorMessageCoroutine()
	{
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/show_alert");
		errorMessage.gameObject.SetActive(value: true);
		errorMessage.DOComplete();
		errorMessage.alpha = 0f;
		errorMessage.DOFade(1f, 0.15f);
		errorMessage.transform.DOComplete();
		errorMessage.transform.localScale = Vector3.one * 1.15f;
		errorMessage.transform.DOScale(1f, 0.15f);
		yield return null;
		float t = 0f;
		while (t < 2f)
		{
			t += Time.unscaledDeltaTime;
			if (Singleton<UIManager>.Instance.GetAnyInputFromEventSystem())
			{
				t = 2f;
			}
			yield return null;
		}
		errorMessage.DOFade(0f, 0.15f).OnComplete(delegate
		{
			errorMessage.gameObject.SetActive(value: false);
		});
	}

	public void UIResponse_RestoreDefault()
	{
		Singleton<SettingsManager>.Instance.RestoreDefaultMaps(OnMapsRestarted);
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/restore_defaults");
	}

	private void OnMapsRestarted(bool areSettingsSaved)
	{
		if (!areSettingsSaved)
		{
			Debug.LogError("OnMapsRestarted. Restarting maps failed.");
		}
		LoadValuesFromDisk();
	}
}
