using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using I2.Loc;
using TMPro;
using UnityEngine;

public class UIMatchSettingTeamsModule : UIMatchSettingModule
{
	private UIMenuLobby uiMenuLobby;

	public GameObject continueButton;

	private IEnumerator showContinueButtonRoutine;

	public GameObject arrowPrompts;

	private UIArrow leftArrow;

	private UIArrow rightArrow;

	public UICharacterGridCell[] noTeamCharacterGridCells;

	public UICharacterGridCell[] teamOneCharacterGridCells;

	public UICharacterGridCell[] teamTwoCharacterGridCells;

	[SerializeField]
	private List<UILobbyPlayer> noTeamMembers = new List<UILobbyPlayer>(6);

	[SerializeField]
	private List<UILobbyPlayer> teamOneMembers = new List<UILobbyPlayer>(6);

	[SerializeField]
	private List<UILobbyPlayer> teamTwoMembers = new List<UILobbyPlayer>(6);

	public GameObject botsJoinTeamPrompt;

	public TextMeshProUGUI botsJoinTeamPromptLabel;

	public bool botsJoinBothTeams = true;

	private IEnumerator assignBotsToTeamsRoutine;

	public override void Enable()
	{
		base.Enable();
		uiMenuLobby = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Lobby) as UIMenuLobby;
		if (arrowPrompts != null)
		{
			leftArrow = arrowPrompts.transform.Find("ARW_Left").GetComponent<UIArrow>();
			rightArrow = arrowPrompts.transform.Find("ARW_Right").GetComponent<UIArrow>();
		}
		if (uiMenuLobby != null)
		{
			botsJoinTeamPrompt.SetActive(uiMenuLobby.GetNumBotUIPlayers() > 0);
			if (botsJoinBothTeams)
			{
				botsJoinTeamPromptLabel.text = LocalizationManager.GetTranslation("ui_bots_join_blue_team");
			}
			else
			{
				botsJoinTeamPromptLabel.text = LocalizationManager.GetTranslation("ui_bots_join_both_teams");
			}
		}
		CheckWhoIsOnWhichTeam();
		CheckIfAllPlayersAreOnATeam();
		UpdateBotsJoinTeamPrompt();
	}

	public override void Disable()
	{
		base.Disable();
	}

	private void CheckWhoIsOnWhichTeam()
	{
		if (uiMenuLobby == null)
		{
			return;
		}
		noTeamMembers.Clear();
		teamOneMembers.Clear();
		teamTwoMembers.Clear();
		for (int i = 0; i < uiMenuLobby.uiPlayers.Length; i++)
		{
			if (uiMenuLobby.uiPlayers[i].readyState == UILobbyPlayer.ReadyState.Ready)
			{
				AssignPlayerToTeam(uiMenuLobby.uiPlayers[i], updateBotTeams: false);
			}
		}
	}

	private void CheckIfAllPlayersAreOnATeam()
	{
		if (!(uiMenuLobby != null))
		{
			return;
		}
		bool flag = uiMenuLobby.AreAllPlayersOnATeam();
		if (flag && teamOneMembers.Count > 0 && teamTwoMembers.Count > 0)
		{
			if (showContinueButtonRoutine != null)
			{
				StopCoroutine(showContinueButtonRoutine);
			}
			showContinueButtonRoutine = ShowContinueButton();
			StartCoroutine(showContinueButtonRoutine);
		}
		else
		{
			continueButton.gameObject.SetActive(value: false);
		}
		leftArrow.SetVisible(!flag);
		rightArrow.SetVisible(!flag);
	}

	private IEnumerator ShowContinueButton()
	{
		yield return new WaitForSecondsRealtime(0.3f);
		if (uiMenuLobby.AreAllPlayersOnATeam() && teamOneMembers.Count > 0 && teamTwoMembers.Count > 0)
		{
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/show");
			continueButton.SetActive(value: true);
			continueButton.transform.DOKill();
			continueButton.transform.localScale = Vector3.one;
			continueButton.transform.DOScale(0.6f, 0.1f).SetEase(Ease.OutBack).From(isRelative: false);
		}
	}

	private void UpdateBotsJoinTeamPrompt()
	{
		if (uiMenuLobby != null)
		{
			botsJoinTeamPrompt.SetActive(uiMenuLobby.GetNumBotUIPlayers() > 0);
			if (botsJoinBothTeams)
			{
				botsJoinTeamPromptLabel.text = LocalizationManager.GetTranslation("ui_bots_join_both_teams");
			}
			else
			{
				botsJoinTeamPromptLabel.text = LocalizationManager.GetTranslation("ui_bots_join_blue_team");
			}
		}
	}

	public void AssignPlayerToTeam(UILobbyPlayer lobbyPlayer, bool updateBotTeams = true)
	{
		if (lobbyPlayer == null)
		{
			return;
		}
		for (int i = 0; i < 3; i++)
		{
			if (i != lobbyPlayer.team)
			{
				RemovePlayerFromTeam(lobbyPlayer, i);
			}
		}
		List<UILobbyPlayer> teamMembers = GetTeamMembers(lobbyPlayer.team);
		UICharacterGridCell[] teamGridCells = GetTeamGridCells(lobbyPlayer.team);
		if (teamMembers == null || teamGridCells == null)
		{
			return;
		}
		if (teamMembers.Contains(lobbyPlayer))
		{
			Debug.Log("team already contains player");
			return;
		}
		teamMembers.Add(lobbyPlayer);
		if (lobbyPlayer.team == 1)
		{
			leftArrow.NudgeLeft();
			Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/ui/join_team_one", "playerID", lobbyPlayer.playerID);
		}
		else if (lobbyPlayer.team == 2)
		{
			rightArrow.NudgeRight();
			Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/ui/join_team_two", "playerID", lobbyPlayer.playerID);
		}
		UICharacterGridCell animatedCell = null;
		if (lobbyPlayer.team != 0)
		{
			for (int j = 0; j < teamGridCells.Length; j++)
			{
				if (teamGridCells[j].currentCharacter == null)
				{
					animatedCell = teamGridCells[j];
					break;
				}
			}
		}
		UpdateTeamGridCells(lobbyPlayer.team, animatedCell);
		if (lobbyPlayer.playerType != PlayerType.Human || lobbyPlayer.team == 0)
		{
			return;
		}
		if ((uiMenuLobby != null && uiMenuLobby.GetNumBotUIPlayers() > 0) & updateBotTeams)
		{
			continueButton.SetActive(value: false);
			if (assignBotsToTeamsRoutine != null)
			{
				StopCoroutine(assignBotsToTeamsRoutine);
			}
			assignBotsToTeamsRoutine = AssignBotsToTeams();
			StartCoroutine(assignBotsToTeamsRoutine);
		}
		else
		{
			CheckIfAllPlayersAreOnATeam();
		}
	}

	public void RemovePlayerFromTeam(UILobbyPlayer lobbyPlayer, int team)
	{
		if (lobbyPlayer == null)
		{
			return;
		}
		List<UILobbyPlayer> teamMembers = GetTeamMembers(team);
		UICharacterGridCell[] teamGridCells = GetTeamGridCells(team);
		if (teamMembers != null && teamGridCells != null)
		{
			UICharacterGridCell animatedCell = null;
			if (teamMembers.Contains(lobbyPlayer))
			{
				teamMembers.Remove(lobbyPlayer);
			}
			UpdateTeamGridCells(team, animatedCell);
		}
	}

	private void UpdateTeamGridCells(int team, UICharacterGridCell animatedCell)
	{
		List<UILobbyPlayer> teamMembers = GetTeamMembers(team);
		UICharacterGridCell[] teamGridCells = GetTeamGridCells(team);
		if (teamGridCells == null)
		{
			return;
		}
		for (int i = 0; i < teamGridCells.Length; i++)
		{
			bool tween = false;
			if (animatedCell != null && teamGridCells[i] == animatedCell)
			{
				tween = true;
			}
			if (i >= teamMembers.Count)
			{
				teamGridCells[i].Empty(tween);
				if (team == 0)
				{
					teamGridCells[i].gameObject.SetActive(value: false);
				}
			}
			else if (teamMembers[i] != null)
			{
				if (team == 0)
				{
					teamGridCells[i].gameObject.SetActive(value: true);
				}
				teamGridCells[i].Fill(teamMembers[i], tween);
			}
		}
	}

	private List<UILobbyPlayer> GetTeamMembers(int team)
	{
		return team switch
		{
			0 => noTeamMembers, 
			1 => teamOneMembers, 
			2 => teamTwoMembers, 
			_ => null, 
		};
	}

	private UICharacterGridCell[] GetTeamGridCells(int team)
	{
		return team switch
		{
			0 => noTeamCharacterGridCells, 
			1 => teamOneCharacterGridCells, 
			2 => teamTwoCharacterGridCells, 
			_ => null, 
		};
	}

	private bool CanPlayerJoinTeam(UILobbyPlayer lobbyPlayer, int team)
	{
		return true;
	}

	public void ToggleBotTeams()
	{
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/off");
		botsJoinBothTeams = !botsJoinBothTeams;
		UpdateBotsJoinTeamPrompt();
		continueButton.SetActive(value: false);
		if (assignBotsToTeamsRoutine != null)
		{
			StopCoroutine(assignBotsToTeamsRoutine);
		}
		assignBotsToTeamsRoutine = AssignBotsToTeams();
		StartCoroutine(assignBotsToTeamsRoutine);
	}

	private IEnumerator AssignBotsToTeams()
	{
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = false;
		for (int i = 0; i < uiMenuLobby.uiPlayers.Length; i++)
		{
			if (uiMenuLobby.uiPlayers[i].readyState != UILobbyPlayer.ReadyState.Ready || uiMenuLobby.uiPlayers[i].playerType != PlayerType.Bot)
			{
				continue;
			}
			if (botsJoinBothTeams)
			{
				if (teamOneMembers.Count > teamTwoMembers.Count)
				{
					if (uiMenuLobby.uiPlayers[i].team == 0)
					{
						yield return new WaitForSecondsRealtime(0.25f);
					}
					uiMenuLobby.uiPlayers[i].SetTeam(2);
				}
				else if (teamOneMembers.Count < teamTwoMembers.Count)
				{
					if (uiMenuLobby.uiPlayers[i].team == 0)
					{
						yield return new WaitForSecondsRealtime(0.25f);
					}
					uiMenuLobby.uiPlayers[i].SetTeam(1);
				}
				else
				{
					if (uiMenuLobby.uiPlayers[i].team != 0)
					{
						continue;
					}
					int numHumansOnTeamOne = 0;
					int numHumansOnTeamTwo = 0;
					for (int j = 0; j < teamOneMembers.Count; j++)
					{
						if (teamOneMembers[j].playerType == PlayerType.Human)
						{
							numHumansOnTeamOne++;
						}
					}
					for (int k = 0; k < teamTwoMembers.Count; k++)
					{
						if (teamTwoMembers[k].playerType == PlayerType.Human)
						{
							numHumansOnTeamTwo++;
						}
					}
					yield return new WaitForSecondsRealtime(0.2f);
					if (numHumansOnTeamOne > numHumansOnTeamTwo)
					{
						uiMenuLobby.uiPlayers[i].SetTeam(2);
					}
					else if (numHumansOnTeamOne < numHumansOnTeamTwo)
					{
						uiMenuLobby.uiPlayers[i].SetTeam(1);
					}
					else
					{
						uiMenuLobby.uiPlayers[i].SetTeam(2);
					}
				}
			}
			else
			{
				if (uiMenuLobby.uiPlayers[i].team != 2)
				{
					yield return new WaitForSecondsRealtime(0.2f);
				}
				uiMenuLobby.uiPlayers[i].SetTeam(2);
			}
		}
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
		CheckIfAllPlayersAreOnATeam();
	}
}
