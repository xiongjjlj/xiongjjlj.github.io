using System.Collections;
using DG.Tweening;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuMatchEnd : UIMenuBase
{
	public RawImage replayScreen;

	public TextMeshProUGUI matchEndTitle;

	public UIButton rematchButton;

	public GameObject rematchSpinner;

	public Selectable wishlistButton;

	public GameObject quitButton;

	private static float shortWaitTime = 0.2f;

	private static float longWaitTime = 0.6f;

	public MatchEndPlayer[] matchEndPlayers;

	public UIMatchEndPlayer[] uiPlayers;

	public UINavigationRefresher navigationRefresher;

	private float prevProgress;

	public UIProgressBar progressBar;

	private bool areAnyItemsAvailableToUnlock = true;

	public void SetBackgroundTexture(Texture tex = null)
	{
		if (tex != null)
		{
			replayScreen.DOComplete();
			replayScreen.color = new Color(1f, 1f, 1f, UIMenuRoundEnd.replayTargetAlpha);
			replayScreen.DOFade(UIMenuRoundEnd.replayTargetAlpha * 0.5f, 0.2f);
			replayScreen.texture = tex;
		}
		else
		{
			replayScreen.color = new Color(1f, 1f, 1f, 0f);
			replayScreen.texture = null;
		}
	}

	public override void Init()
	{
		Singleton<TimeKeeper>.Instance.ResetTimeScale();
		Singleton<UIManager>.Instance.DisableGameplayCanvas();
		Singleton<UIManager>.Instance.SetSelectedGameObject(null);
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = false;
		rematchButton.gameObject.SetActive(value: false);
		rematchSpinner.SetActive(value: false);
		wishlistButton.gameObject.SetActive(value: false);
		quitButton.SetActive(value: false);
		progressBar.gameObject.SetActive(value: false);
		if (Singleton<SettingsManager>.Instance.teamMatch && Singleton<GameManager>.Instance.matchWinners.Count == 1)
		{
			if (Singleton<GameManager>.Instance.matchWinners.Contains(1))
			{
				matchEndTitle.text = LocalizationManager.GetTranslation("ui_teamred") + " " + LocalizationManager.GetTranslation("ui_suffix_wins");
			}
			else if (Singleton<GameManager>.Instance.matchWinners.Contains(2))
			{
				matchEndTitle.text = LocalizationManager.GetTranslation("ui_teamblue") + " " + LocalizationManager.GetTranslation("ui_suffix_wins");
			}
			matchEndTitle.color = Singleton<ColorManager>.Instance.teamColors[Singleton<GameManager>.Instance.matchWinners[0]];
		}
		else if (Singleton<GameManager>.Instance.matchWinners.Count == 1)
		{
			for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
			{
				if (Singleton<GameManager>.Instance.matchWinners.Contains(Singleton<GameManager>.Instance.players[i].playerID))
				{
					matchEndTitle.text = LocalizationManager.GetTranslation(Singleton<GameManager>.Instance.characterNames[Singleton<GameManager>.Instance.players[i].character.characterType]) + " " + LocalizationManager.GetTranslation("ui_suffix_wins");
					HSBColor hsbColor = HSBColor.FromColor(Singleton<GameManager>.Instance.players[i].character.Color);
					if (hsbColor.b > 0.85f)
					{
						hsbColor.s -= 0.1f;
					}
					else
					{
						hsbColor.b = Mathf.Max(hsbColor.b, 0.925f);
						hsbColor.s = Mathf.Min(hsbColor.s, 0.9f);
					}
					matchEndTitle.color = HSBColor.ToColor(hsbColor);
					matchEndTitle.transform.parent.GetComponent<Image>().color = matchEndTitle.color;
					break;
				}
			}
		}
		else
		{
			matchEndTitle.text = LocalizationManager.GetTranslation("ui_title_draw");
			matchEndTitle.color = Color.white;
		}
		matchEndTitle.gameObject.SetActive(value: false);
		prevProgress = (float)Singleton<SettingsManager>.Instance.UnlockedItems.totalProgress / 1000f;
		switch (Singleton<SettingsManager>.Instance.MatchSettings.matchLength)
		{
		case MatchLength.Short:
			Singleton<SettingsManager>.Instance.UnlockedItems.totalProgress += 800;
			break;
		case MatchLength.Medium:
			Singleton<SettingsManager>.Instance.UnlockedItems.totalProgress += 1000;
			break;
		case MatchLength.Long:
			Singleton<SettingsManager>.Instance.UnlockedItems.totalProgress += 1200;
			break;
		}
		Singleton<GameManager>.Instance.ClearNextItemsToUnlock();
		int num = Mathf.FloorToInt(prevProgress % 1f + (float)Singleton<SettingsManager>.Instance.UnlockedItems.totalProgress / 1000f - prevProgress);
		Debug.Log("Number of items to unlock this match: " + num);
		areAnyItemsAvailableToUnlock = true;
		for (int j = 0; j < num; j++)
		{
			if (!Singleton<GameManager>.Instance.UnlockCharacter(elementOfChance: false) && !Singleton<GameManager>.Instance.UnlockHat())
			{
				if (!Singleton<GameManager>.Instance.UnlockCharacter(elementOfChance: false))
				{
					Debug.Log("All items are unlocked");
					areAnyItemsAvailableToUnlock = false;
					break;
				}
				Debug.Log("Force character unlocked because no hats left");
			}
		}
		for (int k = 0; k < uiPlayers.Length; k++)
		{
			uiPlayers[k].SetActive(active: false);
		}
		Singleton<GameManager>.Instance.GiveAwardsToPlayers();
		Singleton<SettingsManager>.Instance.UnlockedItems.totalMatchesPlayed++;
		Singleton<SettingsManager>.Instance.UnlockedItems.matchesPlayedSinceLastCharacterUnlock++;
		Singleton<SettingsManager>.Instance.SaveUnlockedItems(OnSaveUnlockedItems);
	}

	private void OnSaveUnlockedItems(bool unlockedItemsSaved)
	{
		StartCoroutine(ShowMatchAwards());
	}

	private IEnumerator ShowMatchAwards()
	{
		yield return new WaitForSecondsRealtime(0.1f);
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/matchend_victory");
		matchEndTitle.gameObject.SetActive(value: true);
		RectTransform matchEndTitleRT = matchEndTitle.rectTransform;
		matchEndTitleRT.anchoredPosition = new Vector2(0f, -440f);
		matchEndTitleRT.DOComplete();
		matchEndTitleRT.localScale = Vector3.one;
		matchEndTitleRT.DOScale(Vector3.one * 0.25f, 0.33f).SetEase(Ease.OutBack).From(isRelative: false);
		matchEndTitleRT.DOPunchRotation(new Vector3(0f, 0f, Random.Range(1f, 3f)), 0.33f);
		yield return new WaitForSecondsRealtime(0.3f);
		Singleton<UIManager>.Instance.CameraShake(new Vector2(0f, 3f), 10f, 0.25f);
		yield return new WaitForSecondsRealtime(0.7f);
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/matchend_victory_whoosh");
		yield return matchEndTitleRT.DOAnchorPosY(0f, 0.5f).SetEase(Ease.InOutExpo);
		yield return new WaitForSecondsRealtime(0.35f);
		Singleton<AudioManager>.Instance.PlayMenuMusic(victory: true);
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			uiPlayers[i].SetupUIPlayer(Singleton<GameManager>.Instance.players[i].playerID, Singleton<GameManager>.Instance.GetCharacterPrefab(Singleton<GameManager>.Instance.players[i].character.characterType), Singleton<GameManager>.Instance.players[i].team, (!(Singleton<GameManager>.Instance.players[i].GetComponent<PlayerAI>() == null)) ? PlayerType.Bot : PlayerType.Human, Singleton<GameManager>.Instance.players[i].character.equippedHat);
			uiPlayers[i].SetActive(active: true);
			int num = Mathf.Min(3, Singleton<GameManager>.Instance.players[i].awards.Count);
			for (int j = 0; j < num; j++)
			{
				uiPlayers[i].AddAward(Singleton<GameManager>.Instance.players[i].awards[j]);
			}
		}
		Singleton<GameManager>.Instance.ExitMatch();
		for (int k = 0; k < Singleton<GameManager>.Instance.players.Count; k++)
		{
			yield return StartCoroutine(uiPlayers[k].ShowAwards());
		}
		Singleton<AudioManager>.Instance.ProgressMenuMusic();
		yield return new WaitForSecondsRealtime(shortWaitTime);
		if (areAnyItemsAvailableToUnlock || Singleton<GameManager>.Instance.nextCharactersToUnlock.Count + Singleton<GameManager>.Instance.nextHatsToUnlock.Count > 0)
		{
			progressBar.gameObject.SetActive(value: true);
			progressBar.Init(prevProgress);
			yield return StartCoroutine(progressBar.Animate(prevProgress, (float)Singleton<SettingsManager>.Instance.UnlockedItems.totalProgress / 1000f));
			while (Singleton<UIManager>.Instance.GetCurrentMenu().MenuID == MenuType.Unlocked)
			{
				yield return null;
			}
			yield return new WaitForSecondsRealtime(longWaitTime);
			quitButton.SetActive(value: true);
			yield return new WaitForSecondsRealtime(shortWaitTime);
			rematchButton.gameObject.SetActive(value: true);
			rematchButton.interactable = true;
		}
		else
		{
			yield return new WaitForSecondsRealtime(longWaitTime);
			quitButton.SetActive(value: true);
			yield return new WaitForSecondsRealtime(shortWaitTime);
			if (Singleton<SettingsManager>.Instance.demoMode)
			{
				wishlistButton.gameObject.SetActive(value: true);
				yield return new WaitForSecondsRealtime(shortWaitTime);
			}
			rematchButton.gameObject.SetActive(value: true);
			rematchButton.interactable = true;
		}
		if (navigationRefresher != null)
		{
			navigationRefresher.RefreshNavigation();
		}
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
		Singleton<UIManager>.Instance.SetSelectedGameObject(null);
		Singleton<UIManager>.Instance.SetSelectedGameObject(rematchButton.gameObject);
	}

	private IEnumerator StartRematch()
	{
		rematchSpinner.SetActive(value: true);
		yield return StartCoroutine(Singleton<UIManager>.Instance.WaitForMatchStartThenClose(rematchButton, rematch: true));
		rematchSpinner.SetActive(value: false);
	}

	public void UIResponse_Rematch()
	{
		rematchButton.interactable = false;
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = false;
		StartCoroutine(StartRematch());
	}

	public void UIResponse_Wishlist()
	{
		Singleton<UIManager>.Instance.ShowWishlistPrompt();
	}

	public void UIResponse_Quit()
	{
		Singleton<UIManager>.Instance.QuitMatch();
	}
}
