using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuUnlocked : UIMenuBase
{
	public RectTransform headerRT;

	public TextMeshProUGUI unlockedTitle;

	public TextMeshProUGUI characterTitle;

	public LocalizationParamsManager unlockedTitleLocalizationParams;

	public UIButton continueButton;

	public UIButton equipButton;

	[SerializeField]
	private CharacterType characterType;

	[SerializeField]
	private HatType hatType;

	private static float shortWaitTime = 0.2f;

	private static float longWaitTime = 0.6f;

	public UIUnlockedPlayer uiPlayer;

	public Image background;

	public RectTransform[] backgroundCircles;

	public ParticleSystem appearPFX;

	public override void Init()
	{
		Singleton<TimeKeeper>.Instance.ResetTimeScale();
		Singleton<UIManager>.Instance.DisableGameplayCanvas();
		Singleton<UIManager>.Instance.SetSelectedGameObject(null);
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = false;
		continueButton.gameObject.SetActive(value: false);
		equipButton.gameObject.SetActive(value: false);
		unlockedTitle.color = Color.white;
		characterTitle.text = LocalizationManager.GetTranslation(Singleton<GameManager>.Instance.characterNames[characterType]);
		HSBColor hsbColor = HSBColor.FromColor(Singleton<GameManager>.Instance.GetCharacterPrefab(characterType).Color);
		if (hsbColor.b > 0.85f)
		{
			hsbColor.s -= 0.1f;
		}
		else
		{
			hsbColor.b = Mathf.Max(hsbColor.b, 0.925f);
			hsbColor.s = Mathf.Min(hsbColor.s, 0.9f);
		}
		characterTitle.color = HSBColor.ToColor(hsbColor);
		unlockedTitle.gameObject.SetActive(value: false);
		characterTitle.gameObject.SetActive(value: false);
		uiPlayer.SetupUIPlayer(Singleton<GameManager>.Instance.GetCharacterPrefab(characterType), 0, hatType);
		for (int i = 0; i < backgroundCircles.Length; i++)
		{
			backgroundCircles[i].DOComplete();
			backgroundCircles[i].localScale = Vector3.zero;
		}
		StartCoroutine(IntroAnimation());
	}

	public void UnlockHat(KeyValuePair<CharacterType, HatType> characterHatType)
	{
		Debug.Log("Unlock hat " + characterHatType.Value.ToString() + " for " + characterHatType.Key);
		characterType = characterHatType.Key;
		hatType = characterHatType.Value;
		unlockedTitleLocalizationParams.SetParameterValue("prize", LocalizationManager.GetTranslation("ui_hat"));
		Singleton<SettingsManager>.Instance.SaveUnlockedItems(null);
	}

	public void UnlockCharacter(CharacterType characterType)
	{
		Debug.Log("Unlock character " + characterType);
		this.characterType = characterType;
		hatType = HatType.None;
		unlockedTitleLocalizationParams.SetParameterValue("prize", LocalizationManager.GetTranslation("ui_character"));
		Singleton<SettingsManager>.Instance.SaveUnlockedItems(null);
	}

	private IEnumerator IntroAnimation()
	{
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/unlock_item");
		appearPFX.Play();
		for (int i = 0; i < backgroundCircles.Length; i++)
		{
			backgroundCircles[i].localScale = Vector3.one;
			backgroundCircles[i].DOScale(0.001f, ((float)i + 1f) * 1.5f).From(isRelative: false).SetEase(Ease.OutExpo);
		}
		yield return new WaitForSecondsRealtime(shortWaitTime);
		unlockedTitle.gameObject.SetActive(value: true);
		characterTitle.gameObject.SetActive(value: true);
		headerRT.DOComplete();
		headerRT.localScale = Vector3.one;
		headerRT.DOScale(Vector3.one * 0.25f, 0.4f).SetEase(Ease.OutBack).From(isRelative: false);
		uiPlayer.Show();
		yield return new WaitForSecondsRealtime(1.5f);
		if (hatType != HatType.None)
		{
			for (int j = 0; j < Singleton<GameManager>.Instance.players.Count; j++)
			{
				if (Singleton<GameManager>.Instance.players[j].character.characterType == characterType)
				{
					equipButton.gameObject.SetActive(value: true);
					break;
				}
			}
		}
		continueButton.gameObject.SetActive(value: true);
		continueButton.interactable = true;
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
		Singleton<UIManager>.Instance.SetSelectedGameObject(continueButton.gameObject);
	}

	protected override void ZoomInWrapper()
	{
		if (wrapper != null)
		{
			wrapper.localScale = Vector3.one;
			wrapper.DOScale(0.9f, 0.2f).From(isRelative: false);
		}
	}

	public void UIResponse_EquipNow()
	{
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			if (Singleton<GameManager>.Instance.players[i].character.characterType == characterType)
			{
				Singleton<GameManager>.Instance.players[i].character.equippedHat = hatType;
				break;
			}
		}
		UIMenuMatchEnd uIMenuMatchEnd = Singleton<UIManager>.Instance.GetMenu(MenuType.MatchEnd) as UIMenuMatchEnd;
		if (uIMenuMatchEnd != null)
		{
			for (int j = 0; j < uIMenuMatchEnd.uiPlayers.Length; j++)
			{
				if (uIMenuMatchEnd.uiPlayers[j].gameObject.activeSelf && uIMenuMatchEnd.uiPlayers[j].character.characterType == characterType)
				{
					uIMenuMatchEnd.uiPlayers[j].character.RemoveCurrentHat();
					uIMenuMatchEnd.uiPlayers[j].character.equippedHat = hatType;
					uIMenuMatchEnd.uiPlayers[j].character.SetHat();
					Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/equip_hat");
					break;
				}
			}
		}
		UIResponse_Close();
	}
}
