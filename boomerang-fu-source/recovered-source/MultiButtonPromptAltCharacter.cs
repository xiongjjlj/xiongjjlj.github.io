using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MultiButtonPromptAltCharacter : MultiButtonPrompt
{
	public Image highlight;

	private List<UILobbyPlayer> eligibleLobbyPlayers = new List<UILobbyPlayer>();

	public override void SetButtonPrompts(bool animate = false)
	{
		UIMenuLobby uIMenuLobby = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Lobby) as UIMenuLobby;
		if (!(uIMenuLobby != null))
		{
			return;
		}
		HashSet<Guid> hashSet = new HashSet<Guid>();
		eligibleLobbyPlayers.Clear();
		for (int i = 0; i < uIMenuLobby.uiPlayers.Length && eligibleLobbyPlayers.Count < buttonPrompts.Length; i++)
		{
			if (uIMenuLobby.uiPlayers[i].readyState != UILobbyPlayer.ReadyState.Selecting || uIMenuLobby.uiPlayers[i].rewiredPlayer == null || uIMenuLobby.uiPlayers[i].currentGridCell == null || !(uIMenuLobby.uiPlayers[i].currentGridCell.altCharacter != null) || !Singleton<GameManager>.Instance.IsCharacterUnlocked(uIMenuLobby.uiPlayers[i].currentGridCell.altCharacter.characterType))
			{
				continue;
			}
			if (Singleton<GameManager>.Instance.DoesPlayerHaveValidJoystick(uIMenuLobby.uiPlayers[i].rewiredPlayer))
			{
				if (hashSet.Contains(uIMenuLobby.uiPlayers[i].rewiredPlayer.controllers.Joysticks[0].hardwareTypeGuid))
				{
					continue;
				}
				hashSet.Add(uIMenuLobby.uiPlayers[i].rewiredPlayer.controllers.Joysticks[0].hardwareTypeGuid);
			}
			eligibleLobbyPlayers.Add(uIMenuLobby.uiPlayers[i]);
		}
		if (eligibleLobbyPlayers.Count == 0)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		if (!base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(value: true);
			if (animate)
			{
				base.transform.DOComplete();
				base.transform.localScale = Vector3.one * 0.85f;
				base.transform.DOScale(1f, 0.15f).SetEase(Ease.OutBack);
			}
		}
		UICharacterGridCell uICharacterGridCell = null;
		for (int j = 0; j < buttonPrompts.Length; j++)
		{
			if (j < eligibleLobbyPlayers.Count)
			{
				buttonPrompts[j].gameObject.SetActive(value: true);
				buttonPrompts[j].Init(eligibleLobbyPlayers[j].rewiredPlayer, "UIRestore");
				if (Singleton<SettingsManager>.Instance.UnlockedItems.newCharactersToHighlight.Contains(eligibleLobbyPlayers[j].currentGridCell.altCharacter.characterType))
				{
					uICharacterGridCell = eligibleLobbyPlayers[j].currentGridCell;
				}
				else
				{
					highlight.gameObject.SetActive(value: false);
				}
			}
			else
			{
				buttonPrompts[j].gameObject.SetActive(value: false);
			}
		}
		if (uICharacterGridCell == null)
		{
			highlight.gameObject.SetActive(value: false);
		}
		else
		{
			highlight.gameObject.SetActive(value: true);
		}
	}
}
