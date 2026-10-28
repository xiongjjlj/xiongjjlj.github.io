using System.Collections.Generic;
using Rewired;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIEventSystemPlayerSwapper : MonoBehaviour
{
	private EventSystem eventSystem;

	[SerializeField]
	private float timeOfLastSystemPlayerInput;

	private static float minTimeBetweenControllerSwaps = 0.2f;

	[SerializeField]
	private List<Rewired.Player> potentialMasterPlayers = new List<Rewired.Player>(6);

	[SerializeField]
	private int masterPlayerID;

	private void Start()
	{
		eventSystem = EventSystem.current;
		AssignAllPlayersToEventSystem();
	}

	private void Update()
	{
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Paused)
		{
			UIMenuBase currentMenu = Singleton<UIManager>.Instance.GetCurrentMenu();
			if (currentMenu == null || currentMenu.MenuID == UIMenuBase.MenuType.Pause)
			{
				if (potentialMasterPlayers.Count == 0 && eventSystem.sendNavigationEvents)
				{
					SwapFirstPlayerToEventSystem();
				}
				return;
			}
		}
		if (!(eventSystem != null) || !eventSystem.enabled)
		{
			return;
		}
		if (potentialMasterPlayers.Count == 0 && eventSystem.sendNavigationEvents)
		{
			SwapFirstPlayerToEventSystem();
		}
		for (int i = 0; i < potentialMasterPlayers.Count; i++)
		{
			if (potentialMasterPlayers[i] == null)
			{
				continue;
			}
			if (potentialMasterPlayers[i].isPlaying)
			{
				if (GetAnyUIButtonOrAxisInput(potentialMasterPlayers[i]))
				{
					timeOfLastSystemPlayerInput = Singleton<TimeKeeper>.Instance.CurrentTimeUnscaled;
					break;
				}
			}
			else if (Singleton<TimeKeeper>.Instance.TimeSinceUnscaled(timeOfLastSystemPlayerInput) > minTimeBetweenControllerSwaps && GetAnyUIButtonOrAxisInput(potentialMasterPlayers[i]))
			{
				SwapEventSystemMasterPlayer(potentialMasterPlayers[i]);
				timeOfLastSystemPlayerInput = Singleton<TimeKeeper>.Instance.CurrentTimeUnscaled;
				break;
			}
		}
	}

	public Rewired.Player GetCurrentMasterPlayer()
	{
		for (int i = 0; i < potentialMasterPlayers.Count; i++)
		{
			if (potentialMasterPlayers[i].isPlaying)
			{
				masterPlayerID = potentialMasterPlayers[i].id;
				return potentialMasterPlayers[i];
			}
		}
		if (ReInput.players.playerCount > 0)
		{
			masterPlayerID = 0;
			return ReInput.players.Players[0];
		}
		masterPlayerID = -1;
		return null;
	}

	private Rewired.Player GetFirstPlayerPressingAnyButton()
	{
		if (potentialMasterPlayers.Count == 0)
		{
			return null;
		}
		for (int i = 0; i < potentialMasterPlayers.Count; i++)
		{
			if (potentialMasterPlayers[i] == null)
			{
				continue;
			}
			foreach (Controller controller in potentialMasterPlayers[i].controllers.Controllers)
			{
				if (controller.GetAnyButtonDown())
				{
					return potentialMasterPlayers[i];
				}
			}
		}
		return null;
	}

	private void SwapEventSystemMasterPlayer(Rewired.Player masterPlayer)
	{
		Rewired.Player currentMasterPlayer = GetCurrentMasterPlayer();
		foreach (Rewired.Player player in ReInput.players.Players)
		{
			if (masterPlayer != null && player == masterPlayer)
			{
				player.isPlaying = true;
			}
			else
			{
				player.isPlaying = false;
			}
		}
		if (currentMasterPlayer == null || masterPlayer == null || currentMasterPlayer != masterPlayer)
		{
			UIMenuBase currentMenu = Singleton<UIManager>.Instance.GetCurrentMenu();
			if (currentMenu != null)
			{
				currentMenu.UpdateMasterButtonPrompts();
			}
		}
	}

	public bool GetAnyUIButtonOrAxisInput(Rewired.Player rwPlayer)
	{
		if (!rwPlayer.GetButtonDown(18) && !rwPlayer.GetButtonDown(19) && !(Mathf.Abs(rwPlayer.GetAxis(16)) > 0f))
		{
			return Mathf.Abs(rwPlayer.GetAxis(17)) > 0f;
		}
		return true;
	}

	private void SwapFirstPlayerToEventSystem()
	{
		if (ReInput.players.Players.Count > 0)
		{
			SwapEventSystemMasterPlayer(ReInput.players.Players[0]);
		}
	}

	public void ClearAllPlayersFromEventSystem()
	{
		SwapEventSystemMasterPlayer(null);
		potentialMasterPlayers.Clear();
	}

	public void SwapFirstPlayerPressingAnyButtonToEventSystem()
	{
		Rewired.Player firstPlayerPressingAnyButton = GetFirstPlayerPressingAnyButton();
		if (firstPlayerPressingAnyButton != null)
		{
			SwapEventSystemMasterPlayer(firstPlayerPressingAnyButton);
		}
	}

	public void AssignAllPlayersToEventSystem()
	{
		potentialMasterPlayers.Clear();
		foreach (Rewired.Player player in ReInput.players.Players)
		{
			potentialMasterPlayers.Add(player);
		}
		SwapFirstPlayerToEventSystem();
	}

	public void ForceSpecificPlayerToMasterPlayer(Rewired.Player rwPlayer)
	{
		AssignSpecificPlayerToEventSystem(rwPlayer);
		SwapEventSystemMasterPlayer(rwPlayer);
	}

	public void AssignSpecificPlayerToEventSystem(Rewired.Player rwPlayer)
	{
		if (!potentialMasterPlayers.Contains(rwPlayer))
		{
			potentialMasterPlayers.Add(rwPlayer);
		}
	}

	public void RemoveSpecificPlayerFromEventSystem(Rewired.Player rwPlayer)
	{
		if (potentialMasterPlayers.Contains(rwPlayer))
		{
			potentialMasterPlayers.Remove(rwPlayer);
		}
	}
}
