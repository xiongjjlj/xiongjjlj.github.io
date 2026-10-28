using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIScoreRow : MonoBehaviour
{
	public CanvasGroup canvasGroup;

	public UIPlayerIcon playerIcon;

	public Image outline;

	public Image characterIconBackground;

	public Image characterIconSprite;

	public RectTransform characterIconCrown;

	public ParticleSystem addPointPFX;

	private int _rowID;

	[SerializeField]
	public MatchScore rowScore;

	[SerializeField]
	private List<UIScorePoint> uiScorePoints;

	[SerializeField]
	private Color borderColor;

	[SerializeField]
	private Color darkColor;

	[SerializeField]
	private Color lightColor;

	private float incrementWaitTime = 0.4f;

	public int RowID => _rowID;

	public bool HasCrown => characterIconCrown.gameObject.activeSelf;

	public void SetupCharacter(int playerID, PlayerType playerType, Character character)
	{
		SetColors(character.Color);
		outline.color = borderColor;
		characterIconBackground.color = lightColor;
		characterIconSprite.enabled = true;
		characterIconSprite.sprite = character.scoresSpriteIdle;
		characterIconBackground.sprite = null;
		playerIcon.SetActive(setActive: true);
		playerIcon.SetPlayer(playerID, playerType);
		playerIcon.SetColor(borderColor);
		_rowID = playerID;
	}

	public void SetupTeam(int team, Sprite teamFlag)
	{
		SetColors(Singleton<ColorManager>.Instance.teamColors[team]);
		outline.color = borderColor;
		characterIconBackground.color = Color.white;
		characterIconSprite.enabled = false;
		characterIconBackground.sprite = teamFlag;
		characterIconCrown.gameObject.SetActive(value: false);
		playerIcon.SetActive(setActive: false);
		_rowID = team;
	}

	private void SetColors(Color baseColor)
	{
		HSBColor hsbColor = HSBColor.FromColor(baseColor);
		hsbColor.b -= 0.28f;
		hsbColor.s -= 0.1f;
		borderColor = HSBColor.ToColor(hsbColor);
		hsbColor.b -= 0.16f;
		hsbColor.s += 0.1f;
		darkColor = HSBColor.ToColor(hsbColor);
		hsbColor = HSBColor.FromColor(baseColor);
		if (hsbColor.b > 0.85f)
		{
			hsbColor.s -= 0.2f;
		}
		hsbColor.b = Mathf.Max(hsbColor.b, 0.95f);
		hsbColor.s = Mathf.Min(hsbColor.s, 0.6f);
		lightColor = HSBColor.ToColor(hsbColor);
	}

	public void Cleanup()
	{
		rowScore = new MatchScore();
		if (uiScorePoints != null && uiScorePoints.Count > 0)
		{
			for (int i = 0; i < uiScorePoints.Count; i++)
			{
				Object.Destroy(uiScorePoints[i].gameObject);
			}
		}
		uiScorePoints.Clear();
		HideCrown(animate: false);
	}

	public void CreateScorePoints(int numPoints, UIScorePoint scorePointPrefab)
	{
		for (int i = 0; i < numPoints; i++)
		{
			UIScorePoint uIScorePoint = Object.Instantiate(scorePointPrefab, outline.transform);
			uiScorePoints.Add(uIScorePoint);
			uIScorePoint.activeColor = lightColor;
			uIScorePoint.inactiveColor = darkColor;
			uIScorePoint.SetActive(active: false, animate: false);
		}
	}

	public void Show()
	{
		canvasGroup.alpha = 1f;
		canvasGroup.transform.DOComplete();
		canvasGroup.transform.localScale = Vector3.one;
		for (int i = 0; i < uiScorePoints.Count; i++)
		{
			uiScorePoints[i].ResetScaleAndIcon();
		}
	}

	public void Hide()
	{
		canvasGroup.alpha = 0f;
	}

	public void Fade()
	{
		canvasGroup.alpha = 0.5f;
		canvasGroup.transform.DOComplete();
		canvasGroup.transform.localScale = Vector3.one;
		canvasGroup.transform.DOScale(0.9f, 0.2f);
		HideCrown();
	}

	public IEnumerator TallyRow()
	{
		ParticleSystem.MainModule main = addPointPFX.main;
		main.startColor = lightColor;
		while (rowScore.Points < Singleton<GameManager>.Instance.matchScores[_rowID].Points)
		{
			yield return StartCoroutine(AddPoint());
		}
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.DeathMatch && Singleton<SettingsManager>.Instance.GetVictoryCondition == VictoryCondition.Kills)
		{
			if (rowScore.Suicides < Singleton<GameManager>.Instance.matchScores[_rowID].Suicides || rowScore.TeamKills < Singleton<GameManager>.Instance.matchScores[_rowID].TeamKills)
			{
				yield return new WaitForSecondsRealtime(incrementWaitTime);
			}
			while (rowScore.Suicides < Singleton<GameManager>.Instance.matchScores[_rowID].Suicides)
			{
				yield return StartCoroutine(RemoveSuicide());
			}
			while (rowScore.TeamKills < Singleton<GameManager>.Instance.matchScores[_rowID].TeamKills)
			{
				yield return StartCoroutine(RemoveTeamKill());
			}
		}
	}

	private IEnumerator AddPoint()
	{
		yield return new WaitForSecondsRealtime(incrementWaitTime);
		rowScore.Points++;
		rowScore.Total++;
		if (Singleton<SettingsManager>.Instance.GameOptions.screenShakeEnabled)
		{
			base.transform.DOComplete();
			base.transform.DOPunchRotation(new Vector3(0f, 0f, Random.Range(0.5f, 1.5f)), 0.35f);
		}
		if (rowScore.Total > uiScorePoints.Count && uiScorePoints.Count > 0)
		{
			UIScorePoint uIScorePoint = Object.Instantiate(uiScorePoints[0], outline.transform);
			uiScorePoints.Add(uIScorePoint);
			uIScorePoint.activeColor = lightColor;
			uIScorePoint.inactiveColor = darkColor;
			uIScorePoint.SetActive(active: false, animate: false);
			yield return new WaitForEndOfFrame();
		}
		if (rowScore.Total > uiScorePoints.Count)
		{
			yield break;
		}
		uiScorePoints[rowScore.Total - 1].SetActive(active: true);
		Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/ui/add_point", "playerID", _rowID);
		if (addPointPFX != null && canvasGroup.alpha > 0f)
		{
			addPointPFX.transform.position = uiScorePoints[rowScore.Total - 1].transform.position;
			addPointPFX.Play();
		}
		if ((Singleton<GameManager>.Instance.matchWinners.Count == 1 || (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek && Singleton<GameManager>.Instance.matchWinners.Count >= 1)) && Singleton<GameManager>.Instance.matchWinners.Contains(_rowID) && rowScore.Total >= Singleton<SettingsManager>.Instance.matchScoreGoal)
		{
			UIMenuRoundEnd uIMenuRoundEnd = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.RoundEnd) as UIMenuRoundEnd;
			if (uIMenuRoundEnd != null)
			{
				uIMenuRoundEnd.FadeAllNonWinningRows();
			}
			ShowCrown();
			Singleton<AudioManager>.Instance.WinMatch();
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/winning_point");
		}
	}

	private IEnumerator RemoveSuicide()
	{
		rowScore.Suicides++;
		if (rowScore.Total > 0 && rowScore.Total <= uiScorePoints.Count)
		{
			yield return new WaitForSecondsRealtime(incrementWaitTime);
			rowScore.Total--;
			uiScorePoints[rowScore.Total].SetActive(active: false);
			uiScorePoints[rowScore.Total].image.sprite = uiScorePoints[rowScore.Total].skullSprite;
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/remove_point");
		}
	}

	public void ShowCrown()
	{
		characterIconCrown.gameObject.SetActive(value: true);
		characterIconCrown.localScale = Vector3.one;
		characterIconCrown.DOScale(Vector3.one * 0.3f, 0.2f).SetEase(Ease.OutSine).From(isRelative: false);
	}

	public void HideCrown(bool animate = true)
	{
		characterIconCrown.localScale = Vector3.one;
		if (animate)
		{
			characterIconCrown.DOScale(Vector3.one * 0.3f, 0.2f).SetEase(Ease.InBack).OnComplete(delegate
			{
				characterIconCrown.gameObject.SetActive(value: false);
			});
		}
		else
		{
			characterIconCrown.gameObject.SetActive(value: false);
		}
	}

	private IEnumerator RemoveTeamKill()
	{
		rowScore.TeamKills++;
		if (rowScore.Total > 0 && rowScore.Total <= uiScorePoints.Count)
		{
			yield return new WaitForSecondsRealtime(incrementWaitTime);
			rowScore.Total--;
			uiScorePoints[rowScore.Total].GetComponent<Animator>().SetBool("IsFull", value: false);
			uiScorePoints[rowScore.Total].transform.Find("Fill").GetComponent<Image>().color = new Color(0.92f, 0.92f, 0.92f, 0.5f);
		}
	}
}
