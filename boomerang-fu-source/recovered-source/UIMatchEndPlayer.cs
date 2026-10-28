using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMatchEndPlayer : UIPlayer
{
	private UIMenuMatchEnd uiMenuMatchEnd;

	public GameObject[] awardTiles = new GameObject[3];

	private Image background;

	private CanvasGroup canvasGroup;

	private RectTransform rt;

	public List<MatchAward> awardsReceived = new List<MatchAward>();

	public GameObject rank;

	public GameObject crown;

	private TextMeshProUGUI rankText;

	private Color lightColor;

	private Color borderColor;

	public override void Reset()
	{
		base.Reset();
	}

	public override void SetupUIPlayer(int playerID)
	{
		base.SetupUIPlayer(playerID);
		uiMenuMatchEnd = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.MatchEnd) as UIMenuMatchEnd;
		background = GetComponent<Image>();
		rt = GetComponent<RectTransform>();
		if (rank != null)
		{
			rankText = rank.GetComponentInChildren<TextMeshProUGUI>();
			rank.SetActive(value: false);
		}
		if (crown != null)
		{
			crown.SetActive(value: false);
		}
		awardsReceived.Clear();
		for (int i = 0; i < awardTiles.Length; i++)
		{
			awardTiles[i].SetActive(value: false);
		}
		ScheduleLayoutUpdate();
	}

	public void SetActive(bool active)
	{
		canvasGroup = GetComponent<CanvasGroup>();
		if (active)
		{
			base.gameObject.SetActive(value: true);
			if (canvasGroup != null)
			{
				canvasGroup.DOComplete();
				canvasGroup.alpha = 0f;
			}
		}
		else
		{
			if (canvasGroup != null)
			{
				canvasGroup.DOComplete();
				canvasGroup.alpha = 0f;
			}
			base.gameObject.SetActive(value: false);
		}
	}

	public void SetupUIPlayer(int playerID, Character setCharacter, int team, PlayerType playerType, HatType equippedHat)
	{
		SetupUIPlayer(playerID);
		SetCharacterModel(setCharacter);
		SetTeam(team);
		SetIsWinner(isWinner: false);
		base.playerType = playerType;
		UpdateAppearance();
		if (character != null)
		{
			character.RemoveCurrentHat();
			character.equippedHat = equippedHat;
			character.SetHat();
			character.gameObject.SetActive(value: false);
		}
	}

	public override void SetTeam(int team)
	{
		base.SetTeam(team);
		switch (team)
		{
		case 1:
			circle.color = Singleton<ColorManager>.Instance.GetTeamBackgroundColor(team);
			break;
		case 2:
			circle.color = Singleton<ColorManager>.Instance.GetTeamBackgroundColor(team);
			break;
		}
	}

	public void SetIsWinner(bool isWinner)
	{
		if (isWinner)
		{
			rt.DOComplete();
			rt.localScale = Vector3.one;
			rt.DOScale(1.1f, 0.16f).From(isRelative: false);
			background.color = borderColor;
			background.enabled = true;
			if (character != null)
			{
				character.SetFacialExpression(FacialExpression.happy, 600f);
				character.anim.ResetTrigger("Hero");
				character.anim.SetTrigger("Hero");
			}
		}
		else
		{
			background.enabled = false;
			if (character != null)
			{
				character.SetFacialExpression(FacialExpression.sad, 600f);
			}
		}
	}

	public void AddAward(MatchAward award)
	{
		awardsReceived.Add(award);
	}

	public IEnumerator ShowAwards()
	{
		if (rank != null && crown != null)
		{
			if ((Singleton<SettingsManager>.Instance.teamMatch && Singleton<GameManager>.Instance.matchWinners.Contains(team)) || (!Singleton<SettingsManager>.Instance.teamMatch && Singleton<GameManager>.Instance.matchScores[playerID].Rank == 1))
			{
				rank.SetActive(value: false);
				crown.SetActive(value: true);
			}
			else if (!Singleton<SettingsManager>.Instance.teamMatch)
			{
				rank.SetActive(value: true);
				rankText.text = Singleton<GameManager>.Instance.matchScores[playerID].Rank.ToString();
			}
		}
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/matchend_show_player");
		rt.DOComplete();
		rt.localScale = Vector3.one;
		rt.DOScale(Vector3.one * 0.5f, 0.2f).SetEase(Ease.OutBack).From(isRelative: false);
		rt.DOPunchRotation(new Vector3(0f, 0f, Random.Range(2f, 4f)), 0.35f);
		Singleton<UIManager>.Instance.CameraShake(new Vector2(0f, 1f), 10f, 0.15f);
		canvasGroup.DOFade(1f, 0.15f);
		character.gameObject.SetActive(value: true);
		bool isWinner = (!Singleton<SettingsManager>.Instance.teamMatch && Singleton<GameManager>.Instance.matchWinners.Contains(playerID)) || (Singleton<SettingsManager>.Instance.teamMatch && Singleton<GameManager>.Instance.matchWinners.Contains(team));
		if (isWinner)
		{
			character.SetFacialExpression(FacialExpression.content, 0.4f);
		}
		yield return new WaitForSecondsRealtime(0.2f);
		if (isWinner)
		{
			yield return new WaitForSecondsRealtime(0.2f);
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/matchend_show_winner");
			SetIsWinner(isWinner: true);
		}
		else
		{
			SetIsWinner(isWinner: false);
		}
		for (int i = 0; i < awardsReceived.Count; i++)
		{
			if (i < awardsReceived.Count)
			{
				yield return new WaitForSecondsRealtime(0.2f);
				awardTiles[i].SetActive(value: true);
				awardTiles[i].transform.DOComplete();
				awardTiles[i].transform.localScale = Vector3.one;
				awardTiles[i].transform.DOScale(Vector3.one * 0.5f, 0.2f).SetEase(Ease.OutBack).From(isRelative: false);
				awardTiles[i].transform.DOPunchRotation(new Vector3(0f, 0f, Random.Range(2f, 4f)), 0.35f);
				awardTiles[i].GetComponentInChildren<Image>().color = lightColor;
				awardTiles[i].GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.GetTranslation(awardsReceived[i].Description);
				if (isWinner)
				{
					Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/matchend_show_award_winner");
				}
				else
				{
					Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/matchend_show_award_loser");
				}
				if (playerType != PlayerType.Bot)
				{
					Singleton<AchievementManager>.Instance.UnlockAchievementFromMatchAward(awardsReceived[i]);
				}
			}
		}
		yield return new WaitForSecondsRealtime(0.2f);
	}

	private void UpdateAppearance()
	{
		Color color = character.Color;
		if (team == 1)
		{
			color = character.colorTeamOne;
		}
		else if (team == 2)
		{
			color = character.colorTeamTwo;
		}
		HSBColor hsbColor = HSBColor.FromColor(color);
		if (hsbColor.b > 0.85f)
		{
			hsbColor.s -= 0.1f;
		}
		if (hsbColor.b < 0.75f)
		{
			hsbColor.b = Mathf.Max(hsbColor.b, 0.95f);
			hsbColor.s = Mathf.Min(hsbColor.s, 0.8f);
		}
		lightColor = HSBColor.ToColor(hsbColor);
		hsbColor = HSBColor.FromColor(color);
		hsbColor.b -= 0.28f;
		hsbColor.s -= 0.1f;
		borderColor = HSBColor.ToColor(hsbColor);
		circle.color = ColorExt.SetAlpha(lightColor, 0.5f);
		rankText.transform.parent.GetComponent<Image>().color = lightColor;
	}

	protected override void ScheduleLayoutUpdateForAllPlayers()
	{
		if (!(uiMenuMatchEnd == null) && uiMenuMatchEnd.uiPlayers != null)
		{
			for (int i = 0; i < uiMenuMatchEnd.uiPlayers.Length; i++)
			{
				uiMenuMatchEnd.uiPlayers[i].ScheduleLayoutUpdate();
			}
		}
	}

	protected override void UpdateLayout()
	{
		if (!(rank == null) && !(circle == null) && !(crown == null))
		{
			RectTransform component = circle.GetComponent<RectTransform>();
			rank.GetComponent<RectTransform>().anchoredPosition = VectorExt.GetPointOnCircle(45f, component.rect.size.x / 2f);
			crown.GetComponent<RectTransform>().anchoredPosition = VectorExt.GetPointOnCircle(45f, component.rect.size.x / 2f);
			base.UpdateLayout();
		}
	}
}
