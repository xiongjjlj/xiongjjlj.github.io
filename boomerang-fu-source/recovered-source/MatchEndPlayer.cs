using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchEndPlayer : MonoBehaviour
{
	public Character character;

	public int playerID;

	public int team;

	public bool isWinner;

	public List<MatchAward> awardsReceived = new List<MatchAward>();

	public GameObject rank;

	private TextMeshProUGUI rankText;

	private Color lightColor;

	private Animator candidateAnimator;

	private GameObject avatar;

	public GameObject[] awardTiles = new GameObject[3];

	public void Start()
	{
		candidateAnimator = GetComponent<Animator>();
		avatar = base.transform.Find("Avatar").gameObject;
		rankText = rank.GetComponentInChildren<TextMeshProUGUI>();
		rank.SetActive(value: false);
	}

	public void AssignCandidate(Character setCharacter, int setPlayerID, int setTeam)
	{
		awardsReceived.Clear();
		for (int i = 0; i < awardTiles.Length; i++)
		{
			awardTiles[i].SetActive(value: false);
		}
		rank.SetActive(value: false);
		character = setCharacter;
		playerID = setPlayerID;
		team = setTeam;
		candidateAnimator.SetBool("IsWinner", value: false);
		UpdateAppearance();
	}

	public void AddAward(MatchAward award)
	{
		awardsReceived.Add(award);
	}

	public IEnumerator ShowAwards()
	{
		for (int i = 0; i < awardsReceived.Count; i++)
		{
			if (i < awardsReceived.Count)
			{
				yield return new WaitForSecondsRealtime(0.2f);
				if (i == 0)
				{
					if ((!Singleton<SettingsManager>.Instance.teamMatch && Singleton<GameManager>.Instance.matchWinners.Contains(playerID)) || (Singleton<SettingsManager>.Instance.teamMatch && Singleton<GameManager>.Instance.matchWinners.Contains(team)))
					{
						candidateAnimator.SetBool("IsWinner", value: true);
						avatar.GetComponent<Image>().sprite = character.lobbySpriteHappy;
					}
					else
					{
						candidateAnimator.SetBool("IsWinner", value: false);
					}
				}
				awardTiles[i].SetActive(value: true);
				awardTiles[i].GetComponentInChildren<Image>().color = lightColor;
				awardTiles[i].GetComponentInChildren<TextMeshProUGUI>().text = awardsReceived[i].Description;
			}
			if (!Singleton<SettingsManager>.Instance.teamMatch)
			{
				yield return new WaitForSecondsRealtime(0.2f);
				rank.SetActive(value: true);
				rankText.text = Singleton<GameManager>.Instance.matchScores[playerID].Rank.ToString();
			}
		}
	}

	private void UpdateAppearance()
	{
		HSBColor hsbColor = HSBColor.FromColor(character.Color);
		if (hsbColor.b > 0.85f)
		{
			hsbColor.s -= 0.1f;
		}
		if (hsbColor.b < 0.75f)
		{
			hsbColor.b = Mathf.Max(hsbColor.b, 0.925f);
			hsbColor.s = Mathf.Min(hsbColor.s, 0.9f);
		}
		lightColor = HSBColor.ToColor(hsbColor);
		avatar.GetComponent<Image>().sprite = character.lobbySpriteIdle;
		base.transform.Find("Glow").GetComponent<Image>().color = lightColor - new Color(0f, 0f, 0f, 0.5f);
		rankText.transform.parent.GetComponent<Image>().color = lightColor;
	}
}
