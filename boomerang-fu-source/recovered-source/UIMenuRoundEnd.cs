using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Moments;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuRoundEnd : UIMenuBase
{
	private CanvasGroup canvasGroup;

	public Canvas content;

	public UIScorePoint uiScorePointPrefab;

	public UIScoreRow[] uiScoreRows;

	public Sprite teamOneFlag;

	public Sprite teamTwoFlag;

	private static float shortWaitTime = 0.2f;

	private static float longWaitTime = 0.6f;

	public RectTransform warmUpMessage;

	public UIButton continueButton;

	public GameObject continueSpinner;

	public GameObject quitButton;

	[Space(10f)]
	[Header("Replay")]
	public RawImage replayScreen;

	public UIButtonReplay replayButton;

	private RawImage replayTarget;

	private Recorder replayRecorder;

	private List<RenderTexture> replayFrames = new List<RenderTexture>();

	private bool replayIsPlaying;

	private float replayTime;

	private float replayTimePerFrame;

	private int replayIndex;

	private bool skipIdleFrame;

	private float canvasTargetAlpha = 1f;

	public static float replayTargetAlpha = 0f;

	public override void Setup()
	{
		base.Setup();
		canvasGroup = base.transform.Find("Wrapper").GetComponent<CanvasGroup>();
		replayRecorder = Singleton<Camera3D>.Instance.GetComponent<Recorder>();
		replayTimePerFrame = 1f / (float)replayRecorder.m_FramePerSecond;
	}

	public override void Init()
	{
		canvasGroup.alpha = 0f;
		canvasGroup.DOFade(1f, 0.4f);
		canvasTargetAlpha = 1f;
		replayTargetAlpha = 0.5f;
		replayScreen.color = new Color(1f, 1f, 1f, 0f);
		base.Init();
		continueButton.gameObject.SetActive(value: false);
		continueSpinner.gameObject.SetActive(value: false);
		quitButton.SetActive(value: false);
		replayButton.gameObject.SetActive(value: false);
		content.enabled = !Singleton<SettingsManager>.Instance.tieBreaker;
		warmUpMessage.gameObject.SetActive(value: false);
		int num = (Singleton<SettingsManager>.Instance.teamMatch ? 2 : Singleton<GameManager>.Instance.players.Count);
		for (int i = 0; i < num && i < uiScoreRows.Length; i++)
		{
			if (Singleton<LevelManager>.Instance.level.isTutorialLevel)
			{
				uiScoreRows[i].gameObject.SetActive(value: false);
				continue;
			}
			uiScoreRows[i].gameObject.SetActive(value: true);
			uiScoreRows[i].Hide();
		}
		Singleton<UIManager>.Instance.SetSelectedGameObject(null);
		StartCoroutine(ShowScores());
	}

	private void Update()
	{
		if (!quitButton.activeSelf)
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				quitButton.SetActive(value: true);
			}
			for (int i = 0; i < ReInput.players.playerCount; i++)
			{
				if (ReInput.players.GetPlayer(i).GetButtonShortPressDown("Start"))
				{
					quitButton.SetActive(value: true);
					break;
				}
			}
		}
		if (skipIdleFrame)
		{
			skipIdleFrame = false;
		}
		else
		{
			if (replayIsPlaying)
			{
				if (canvasGroup.alpha != canvasTargetAlpha)
				{
					canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, canvasTargetAlpha, Time.unscaledDeltaTime * 10f);
				}
				if (replayScreen.color.a != replayTargetAlpha)
				{
					replayScreen.color = ColorExt.SetAlpha(replayScreen.color, Mathf.Lerp(replayScreen.color.a, replayTargetAlpha, Time.unscaledDeltaTime * 10f));
				}
				for (int j = 0; j < ReInput.players.playerCount; j++)
				{
					if (ReInput.players.GetPlayer(j).GetButton(4))
					{
						canvasTargetAlpha = 0f;
						replayTargetAlpha = 0.8f;
						break;
					}
					canvasTargetAlpha = 1f;
					replayTargetAlpha = 0.5f;
				}
			}
			if (replayIsPlaying && Singleton<SettingsManager>.Instance.GameOptions.recordReplays && Singleton<GameManager>.Instance.gameState != GameManager.GameState.Paused)
			{
				replayTime += Time.unscaledDeltaTime;
				if (replayTime > replayTimePerFrame)
				{
					if (replayIndex >= replayFrames.Count)
					{
						replayIndex = 0;
					}
					if (replayButton.thumbnailReplay.enabled)
					{
						replayButton.thumbnailReplay.texture = replayFrames[replayIndex];
					}
					replayScreen.texture = replayFrames[replayIndex];
					replayIndex++;
					replayTime -= replayTimePerFrame;
				}
			}
		}
		if (Singleton<GameManager>.Instance.gifRecorder.m_IsSaving && replayButton.replayStartedSaving)
		{
			if (Singleton<GameManager>.Instance.gifRecorder.isCreatingPalette)
			{
				replayButton.SetProgressBar(0.05f);
			}
			else
			{
				replayButton.SetProgressBar(Singleton<GameManager>.Instance.gifRecorder.m_Progress);
			}
		}
		else if (!Singleton<GameManager>.Instance.gifRecorder.m_IsSaving && replayButton.replayStartedSaving)
		{
			replayButton.OnReplaySaved();
		}
	}

	public void OnReplayProcessingDone()
	{
		replayButton.OnReplayProcessingDone();
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
		continueButton.gameObject.SetActive(value: true);
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/show");
		Singleton<UIManager>.Instance.SetSelectedGameObject(continueButton.gameObject);
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		if (hasFocus)
		{
			skipIdleFrame = true;
		}
	}

	private void OnApplicationPause(bool pauseStatus)
	{
		if (!pauseStatus)
		{
			skipIdleFrame = true;
		}
	}

	public void ResetScores()
	{
		for (int i = 0; i < uiScoreRows.Length; i++)
		{
			if (Singleton<SettingsManager>.Instance.teamMatch)
			{
				if (i < 2)
				{
					uiScoreRows[i].Cleanup();
					switch (i)
					{
					case 0:
						uiScoreRows[i].SetupTeam(1, teamOneFlag);
						break;
					case 1:
						uiScoreRows[i].SetupTeam(2, teamTwoFlag);
						break;
					}
					uiScoreRows[i].CreateScorePoints(Singleton<SettingsManager>.Instance.matchScoreGoal, uiScorePointPrefab);
				}
				else
				{
					uiScoreRows[i].gameObject.SetActive(value: false);
				}
			}
			else if (i < Singleton<GameManager>.Instance.players.Count)
			{
				uiScoreRows[i].gameObject.SetActive(value: true);
				uiScoreRows[i].Cleanup();
				uiScoreRows[i].SetupCharacter(Singleton<GameManager>.Instance.players[i].playerID, Singleton<GameManager>.Instance.players[i].playerType, Singleton<GameManager>.Instance.players[i].character);
				uiScoreRows[i].CreateScorePoints(Singleton<SettingsManager>.Instance.matchScoreGoal, uiScorePointPrefab);
			}
			else
			{
				uiScoreRows[i].gameObject.SetActive(value: false);
			}
		}
	}

	private IEnumerator ShowScores()
	{
		int rowCount = (Singleton<SettingsManager>.Instance.teamMatch ? 2 : Singleton<GameManager>.Instance.players.Count);
		if (Singleton<LevelManager>.Instance.level.isTutorialLevel)
		{
			rowCount = 0;
			yield return new WaitForSecondsRealtime(shortWaitTime);
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/roundtitle_other");
			warmUpMessage.gameObject.SetActive(value: true);
			warmUpMessage.transform.localScale = Vector3.one;
			warmUpMessage.transform.DOScale(Vector3.one * 0.5f, 0.2f).SetEase(Ease.OutBack).From(isRelative: false);
		}
		else if (!Singleton<SettingsManager>.Instance.tieBreaker)
		{
			for (int i = 0; i < rowCount && i < uiScoreRows.Length; i++)
			{
				uiScoreRows[i].Show();
			}
			yield return new WaitForSecondsRealtime(shortWaitTime);
		}
		if (!Singleton<SettingsManager>.Instance.tieBreaker)
		{
			for (int j = 0; j < rowCount && j < uiScoreRows.Length; j++)
			{
				yield return StartCoroutine(uiScoreRows[j].TallyRow());
			}
		}
		int num = 0;
		List<UIScoreRow> crownRows = new List<UIScoreRow>(6);
		for (int k = 0; k < rowCount && k < uiScoreRows.Length; k++)
		{
			if (uiScoreRows[k].rowScore.Total > num)
			{
				crownRows.Clear();
				num = uiScoreRows[k].rowScore.Total;
			}
			if (uiScoreRows[k].rowScore.Total >= num)
			{
				crownRows.Add(uiScoreRows[k]);
			}
		}
		yield return new WaitForSecondsRealtime(shortWaitTime);
		yield return new WaitForSecondsRealtime(shortWaitTime);
		PlayReplay();
		yield return new WaitForSecondsRealtime(shortWaitTime);
		if (crownRows.Count > 0)
		{
			bool flag = false;
			bool flag2 = (float)crownRows.Count > (float)rowCount / 2f;
			for (int l = 0; l < rowCount && l < uiScoreRows.Length; l++)
			{
				if (uiScoreRows[l].HasCrown && (!crownRows.Contains(uiScoreRows[l]) | flag2))
				{
					uiScoreRows[l].HideCrown();
					flag = true;
				}
			}
			if (!flag2)
			{
				for (int m = 0; m < rowCount && m < uiScoreRows.Length; m++)
				{
					if (!uiScoreRows[m].HasCrown && crownRows.Contains(uiScoreRows[m]))
					{
						uiScoreRows[m].ShowCrown();
						flag = true;
					}
				}
			}
			if (flag)
			{
				Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/crown_move");
				yield return new WaitForSecondsRealtime(longWaitTime);
			}
		}
		continueButton.gameObject.SetActive(value: true);
		continueButton.interactable = true;
		Singleton<UIManager>.Instance.SetSelectedGameObject(continueButton.gameObject);
		if (Singleton<SettingsManager>.Instance.GameOptions.recordReplays && Singleton<GameManager>.Instance.canPlatformSaveGifs)
		{
			yield return new WaitForSecondsRealtime(shortWaitTime);
			yield return new WaitForSecondsRealtime(shortWaitTime);
			replayButton.gameObject.SetActive(value: true);
			replayButton.PrepareThumbnailReplay();
		}
		else
		{
			replayButton.gameObject.SetActive(value: false);
		}
	}

	public void FadeAllNonWinningRows()
	{
		List<int> list = new List<int>(uiScoreRows.Length);
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			int rowID = Singleton<GameManager>.Instance.GetRowID(Singleton<GameManager>.Instance.players[i]);
			if (!Singleton<GameManager>.Instance.matchWinners.Contains(rowID))
			{
				list.Add(rowID);
			}
		}
		for (int j = 0; j < uiScoreRows.Length; j++)
		{
			if (list.Contains(uiScoreRows[j].RowID))
			{
				uiScoreRows[j].Fade();
			}
		}
	}

	private void PlayReplay()
	{
		if (!Singleton<SettingsManager>.Instance.GameOptions.recordReplays)
		{
			replayScreen.enabled = true;
			StartCoroutine(FadeInReplay());
			return;
		}
		replayFrames.Clear();
		foreach (RenderTexture frame in replayRecorder.m_Frames)
		{
			replayFrames.Add(frame);
		}
		if (replayFrames.Count > 0)
		{
			replayScreen.enabled = true;
			replayIndex = 0;
			replayIsPlaying = true;
			StartCoroutine(FadeInReplay());
		}
		else
		{
			Debug.Log("replayRecorder.m_Frames was empty!");
		}
	}

	public void StopReplay()
	{
		replayFrames.Clear();
		replayIsPlaying = false;
		replayScreen.enabled = false;
	}

	private IEnumerator FadeInReplay()
	{
		float currentAlpha = 0f;
		float targetAlpha = 0.5f;
		while (currentAlpha < targetAlpha)
		{
			yield return null;
			currentAlpha += Time.unscaledDeltaTime * targetAlpha;
			replayScreen.color = new Color(1f, 1f, 1f, currentAlpha);
		}
	}

	public IEnumerator ContinueToNextRound()
	{
		if (Singleton<GameManager>.Instance.matchWinners.Count > 1)
		{
			Singleton<SettingsManager>.Instance.tieBreaker = true;
		}
		continueSpinner.SetActive(value: true);
		if (!Singleton<GameManager>.Instance.abortLoading)
		{
			yield return StartCoroutine(Singleton<GameManager>.Instance.LoadNextRound());
			if (Singleton<GameManager>.Instance.abortLoading)
			{
				Singleton<GameManager>.Instance.gameState = GameManager.GameState.Menu;
				yield break;
			}
			StopReplay();
			continueSpinner.SetActive(value: false);
			Singleton<GameManager>.Instance.StartRound();
			Singleton<UIManager>.Instance.PopMenu();
			Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
		}
	}

	public void UIResponse_Continue()
	{
		continueButton.interactable = false;
		if (Singleton<GameManager>.Instance.matchWinners.Count == 1 || Singleton<SettingsManager>.Instance.tieBreaker || ((Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek || Singleton<SettingsManager>.Instance.showcaseMode) && Singleton<GameManager>.Instance.matchWinners.Count > 0))
		{
			Singleton<UIManager>.Instance.PopMenu();
			Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
			UIMenuMatchEnd uIMenuMatchEnd = Singleton<UIManager>.Instance.GetMenu(MenuType.MatchEnd) as UIMenuMatchEnd;
			if (uIMenuMatchEnd != null)
			{
				Singleton<UIManager>.Instance.PushMenu(uIMenuMatchEnd);
				uIMenuMatchEnd.SetBackgroundTexture(replayScreen.texture);
				StopReplay();
			}
		}
		else
		{
			Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = false;
			StartCoroutine(ContinueToNextRound());
		}
	}

	public void UIResponse_SaveReplay()
	{
		if (!replayButton.replaySaved && !replayButton.replayStartedSaving)
		{
			Singleton<GameManager>.Instance.gifRecorder.SaveRecordingToDisk();
			replayButton.SaveReplay();
			Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = false;
			continueButton.gameObject.SetActive(value: false);
		}
		else
		{
			_ = replayButton.replaySaved;
		}
	}

	public void UIResponse_Quit()
	{
		StopReplay();
		Singleton<UIManager>.Instance.QuitMatch();
	}
}
