using System;
using System.Collections.Generic;
using DG.Tweening;
using Rewired;
using UnityEngine;

public class MultiButtonPrompt : MonoBehaviour
{
	public ButtonPrompt[] buttonPrompts;

	protected List<Rewired.Player> elligiblePlayers = new List<Rewired.Player>(2);

	public virtual void SetButtonPrompts(bool animate)
	{
		UIMenuLobby uIMenuLobby = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Lobby) as UIMenuLobby;
		if (!(uIMenuLobby != null))
		{
			return;
		}
		HashSet<Guid> hashSet = new HashSet<Guid>();
		elligiblePlayers.Clear();
		for (int i = 0; i < uIMenuLobby.uiPlayers.Length && elligiblePlayers.Count < buttonPrompts.Length; i++)
		{
			if (uIMenuLobby.uiPlayers[i].readyState != UILobbyPlayer.ReadyState.Selecting || uIMenuLobby.uiPlayers[i].rewiredPlayer == null || uIMenuLobby.uiPlayers[i].character == null || Singleton<GameManager>.Instance.NumHatsUnlockedForCharacter(uIMenuLobby.uiPlayers[i].character.characterType) <= 1)
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
			elligiblePlayers.Add(uIMenuLobby.uiPlayers[i].rewiredPlayer);
		}
		if (elligiblePlayers.Count == 0)
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
		if (elligiblePlayers.Count == 1 && Singleton<GameManager>.Instance.DoesPlayerHaveValidJoystick(elligiblePlayers[0]))
		{
			buttonPrompts[0].gameObject.SetActive(value: true);
			buttonPrompts[0].Init(elligiblePlayers[0], "UITabLeft");
			buttonPrompts[1].gameObject.SetActive(value: true);
			buttonPrompts[1].Init(elligiblePlayers[0], "UITabRight");
			return;
		}
		for (int j = 0; j < buttonPrompts.Length; j++)
		{
			if (j < elligiblePlayers.Count)
			{
				buttonPrompts[j].gameObject.SetActive(value: true);
				buttonPrompts[j].Init(elligiblePlayers[j], "UITabRight");
			}
			else
			{
				buttonPrompts[j].gameObject.SetActive(value: false);
			}
		}
	}
}
