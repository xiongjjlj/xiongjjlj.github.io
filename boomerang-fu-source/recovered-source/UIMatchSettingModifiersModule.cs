using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class UIMatchSettingModifiersModule : UIMatchSettingModule
{
	public UISliderButton matchLength;

	public UISliderButton victoryCondition;

	public UISliderButton warmupRound;

	public UISliderButton autoBalance;

	public UISliderButton hideAndSeekAttempts;

	public UISliderButton hideAndSeekTimeLimit;

	public UISliderButton hideAndSeekHideTime;

	public UISliderButton friendlyFire;

	public UISliderButton teamRevives;

	public UISliderButton availablePowerups;

	public UISliderButton edgeProtection;

	public UISliderButton pullBack;

	public UISliderButton homing;

	public UISliderButton suddenDeath;

	public UIVariableRow[] variableRows;

	public GameObject powerupToggleRow;

	public UIPowerupToggle[] powerupToggles;

	public TextMeshProUGUI powerupLabel;

	public UISliderButton powerupSpawnFreq;

	public UIFillButton restoreDefaultPrompt;

	[SerializeField]
	private float restoreDefaultCharge;

	private static float restoreDefaultDuration = 1f;

	[SerializeField]
	private bool restoreDefaultInputBlocked;

	public GameObject selectPrompt;

	public override void Enable()
	{
		for (int i = 0; i < variableRows.Length; i++)
		{
			variableRows[i].TurnOnOff();
		}
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

	private void OnEnable()
	{
		matchLength.OnValueChanged.AddListener(OnChangeMatchLength);
		victoryCondition.OnValueChanged.AddListener(OnChangeVictoryCondition);
		warmupRound.OnValueChanged.AddListener(OnChangeWarmupRound);
		autoBalance.OnValueChanged.AddListener(OnChangeAutoBalanace);
		friendlyFire.OnValueChanged.AddListener(OnChangeFriendlyFire);
		teamRevives.OnValueChanged.AddListener(OnChangeTeamRevives);
		availablePowerups.OnValueChanged.AddListener(OnChangeAvailablePowerups);
		powerupSpawnFreq.OnValueChanged.AddListener(OnChangePowerupSpawnFreq);
		hideAndSeekAttempts.OnValueChanged.AddListener(OnChangeHideAndSeekAttempts);
		hideAndSeekTimeLimit.OnValueChanged.AddListener(OnChangeHideAndSeekTimeLimit);
		hideAndSeekHideTime.OnValueChanged.AddListener(OnChangeHideAndSeekHideTime);
		edgeProtection.OnValueChanged.AddListener(OnChangeEdgeProtection);
		pullBack.OnValueChanged.AddListener(OnChangePullBack);
		homing.OnValueChanged.AddListener(OnChangeHoming);
		suddenDeath.OnValueChanged.AddListener(OnChangeSuddenDeath);
		for (int i = 0; i < powerupToggles.Length; i++)
		{
			powerupToggles[i].onValueChanged.AddListener(OnTogglePowerup);
		}
	}

	private void OnDisable()
	{
		matchLength.OnValueChanged.RemoveListener(OnChangeMatchLength);
		victoryCondition.OnValueChanged.RemoveListener(OnChangeVictoryCondition);
		warmupRound.OnValueChanged.RemoveListener(OnChangeWarmupRound);
		autoBalance.OnValueChanged.RemoveListener(OnChangeAutoBalanace);
		friendlyFire.OnValueChanged.RemoveListener(OnChangeFriendlyFire);
		teamRevives.OnValueChanged.RemoveListener(OnChangeTeamRevives);
		availablePowerups.OnValueChanged.RemoveListener(OnChangeAvailablePowerups);
		powerupSpawnFreq.OnValueChanged.RemoveListener(OnChangePowerupSpawnFreq);
		hideAndSeekAttempts.OnValueChanged.RemoveListener(OnChangeHideAndSeekAttempts);
		hideAndSeekTimeLimit.OnValueChanged.RemoveListener(OnChangeHideAndSeekTimeLimit);
		hideAndSeekHideTime.OnValueChanged.RemoveListener(OnChangeHideAndSeekHideTime);
		edgeProtection.OnValueChanged.RemoveListener(OnChangeEdgeProtection);
		pullBack.OnValueChanged.RemoveListener(OnChangePullBack);
		homing.OnValueChanged.RemoveListener(OnChangeHoming);
		suddenDeath.OnValueChanged.RemoveListener(OnChangeSuddenDeath);
		for (int i = 0; i < powerupToggles.Length; i++)
		{
			powerupToggles[i].onValueChanged.RemoveListener(OnTogglePowerup);
		}
	}

	public void LoadValuesFromDisk()
	{
		matchLength.SetValue((int)Singleton<SettingsManager>.Instance.MatchSettings.matchLength);
		victoryCondition.SetValue((int)Singleton<SettingsManager>.Instance.MatchSettings.victoryConditionDeathMatch);
		warmupRound.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.MatchSettings.includeTutorial));
		autoBalance.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.MatchSettings.autoBalance));
		friendlyFire.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.MatchSettings.friendlyFire));
		teamRevives.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.MatchSettings.teamRevives));
		if (Singleton<SettingsManager>.Instance.MatchSettings.availablePowerups == PowerupType.None)
		{
			availablePowerups.SetValue(0);
			OnChangeAvailablePowerups(0);
		}
		else if (Singleton<SettingsManager>.Instance.MatchSettings.availablePowerups == Powerup.AllPowerups())
		{
			availablePowerups.SetValue(1);
			OnChangeAvailablePowerups(1);
		}
		else
		{
			WritePowerupToggleValues();
			availablePowerups.SetValue(2);
			OnChangeAvailablePowerups(2);
		}
		powerupSpawnFreq.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.MatchSettings.powerupSpawnFrequency));
		hideAndSeekAttempts.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.MatchSettings.hideAndSeekAttempts));
		hideAndSeekTimeLimit.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.MatchSettings.hideAndSeekTimeLimit));
		hideAndSeekHideTime.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.MatchSettings.hideAndSeekHideTime));
		edgeProtection.SetValue(Singleton<SettingsManager>.Instance.MatchSettings.edgeProtection);
		pullBack.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.MatchSettings.canRetrieveDiscs));
		homing.SetValue(Convert.ToInt32(Singleton<SettingsManager>.Instance.MatchSettings.homingEnabled));
		if (Singleton<SettingsManager>.Instance.MatchSettings.suddenDeathTime == 0f)
		{
			suddenDeath.SetValue(0);
		}
		else if (Singleton<SettingsManager>.Instance.MatchSettings.suddenDeathTime == 30f)
		{
			suddenDeath.SetValue(1);
		}
		else
		{
			suddenDeath.SetValue(2);
		}
	}

	public void UIResponse_RestoreDefault()
	{
		Singleton<SettingsManager>.Instance.RestoreDefaultMatchSettings(OnMatchSettingsRestarted);
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/restore_defaults");
	}

	private void OnMatchSettingsRestarted(bool areSettingsSaved)
	{
		if (!areSettingsSaved)
		{
			Debug.LogError("OnMatchSettingsRestarted. Restarting modifiers failed.");
		}
		LoadValuesFromDisk();
	}

	private void OnChangeMatchLength(int value)
	{
		Singleton<SettingsManager>.Instance.MatchSettings.matchLength = (MatchLength)value;
	}

	private void OnChangeVictoryCondition(int value)
	{
		Singleton<SettingsManager>.Instance.MatchSettings.victoryConditionDeathMatch = (VictoryCondition)value;
	}

	private void OnChangeWarmupRound(int value)
	{
		Singleton<SettingsManager>.Instance.MatchSettings.includeTutorial = Convert.ToBoolean(value);
	}

	private void OnChangeAutoBalanace(int value)
	{
		Singleton<SettingsManager>.Instance.MatchSettings.autoBalance = Convert.ToBoolean(value);
	}

	private void OnChangeEdgeProtection(int value)
	{
		Singleton<SettingsManager>.Instance.MatchSettings.edgeProtection = value;
	}

	private void OnChangeFriendlyFire(int value)
	{
		Singleton<SettingsManager>.Instance.MatchSettings.friendlyFire = Convert.ToBoolean(value);
	}

	private void OnChangeTeamRevives(int value)
	{
		Singleton<SettingsManager>.Instance.MatchSettings.teamRevives = Convert.ToBoolean(value);
	}

	private void OnTogglePowerup(bool value)
	{
		ReadPowerupToggleValues();
	}

	private void ReadPowerupToggleValues()
	{
		for (int i = 0; i < powerupToggles.Length; i++)
		{
			if (powerupToggles[i].isOn)
			{
				Singleton<SettingsManager>.Instance.MatchSettings.availablePowerups |= powerupToggles[i].powerupType;
			}
			else
			{
				Singleton<SettingsManager>.Instance.MatchSettings.availablePowerups &= ~powerupToggles[i].powerupType;
			}
		}
	}

	private void WritePowerupToggleValues()
	{
		for (int i = 0; i < powerupToggles.Length; i++)
		{
			powerupToggles[i].SetIsOnWithoutNotify(Singleton<SettingsManager>.Instance.MatchSettings.availablePowerups.HasPowerup(powerupToggles[i].powerupType));
		}
	}

	private void OnChangeAvailablePowerups(int value)
	{
		switch (value)
		{
		case 0:
			HidePowerupToggleRow();
			Singleton<SettingsManager>.Instance.MatchSettings.availablePowerups = PowerupType.None;
			break;
		case 1:
			HidePowerupToggleRow();
			Singleton<SettingsManager>.Instance.MatchSettings.availablePowerups = Powerup.AllPowerups();
			break;
		case 2:
			if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
			{
				HidePowerupToggleRow();
				break;
			}
			WritePowerupToggleValues();
			ShowPowerupToggleRow();
			break;
		}
	}

	private void ShowPowerupToggleRow()
	{
		if (powerupToggleRow != null)
		{
			powerupToggleRow.gameObject.SetActive(value: true);
		}
		for (int i = 0; i < powerupToggles.Length; i++)
		{
			powerupToggles[i].interactable = true;
		}
		powerupLabel.gameObject.SetActive(value: false);
	}

	private void HidePowerupToggleRow()
	{
		if (powerupToggleRow != null)
		{
			powerupToggleRow.gameObject.SetActive(value: false);
		}
		for (int i = 0; i < powerupToggles.Length; i++)
		{
			powerupToggles[i].interactable = false;
		}
	}

	private bool IsPowerupLegalInThisMatchType(PowerupType powerup)
	{
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.GoldenDisc && (powerup == PowerupType.TeleportDisc || powerup == PowerupType.ControlledDisc || powerup == PowerupType.ShrinkingBounds))
		{
			return false;
		}
		return true;
	}

	private void OnChangePowerupSpawnFreq(int value)
	{
		Singleton<SettingsManager>.Instance.MatchSettings.powerupSpawnFrequency = (PowerupSpawnFrequency)value;
	}

	private void OnChangeHideAndSeekAttempts(int value)
	{
		Singleton<SettingsManager>.Instance.MatchSettings.hideAndSeekAttempts = (HideAndSeekAttempts)value;
	}

	private void OnChangeHideAndSeekTimeLimit(int value)
	{
		Singleton<SettingsManager>.Instance.MatchSettings.hideAndSeekTimeLimit = (HideAndSeekTimeLimit)value;
	}

	private void OnChangeHideAndSeekHideTime(int value)
	{
		Singleton<SettingsManager>.Instance.MatchSettings.hideAndSeekHideTime = (HideAndSeekHideTime)value;
	}

	private void OnChangePullBack(int value)
	{
		Singleton<SettingsManager>.Instance.MatchSettings.canRetrieveDiscs = Convert.ToBoolean(value);
	}

	private void OnChangeHoming(int value)
	{
		Singleton<SettingsManager>.Instance.MatchSettings.homingEnabled = Convert.ToBoolean(value);
	}

	private void OnChangeSuddenDeath(int value)
	{
		switch (value)
		{
		case 0:
			Singleton<SettingsManager>.Instance.MatchSettings.suddenDeathTime = 0f;
			break;
		case 1:
			Singleton<SettingsManager>.Instance.MatchSettings.suddenDeathTime = 30f;
			break;
		case 2:
			Singleton<SettingsManager>.Instance.MatchSettings.suddenDeathTime = 120f;
			break;
		}
	}
}
