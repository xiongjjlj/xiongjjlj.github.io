using Steamworks;
using UnityEngine;

public class SteamUpdater : Singleton<SteamUpdater>
{
	private CGameID gameID;

	private bool hasRequestedStats;

	public bool areLocalStatsValid;

	private bool storeStatsNextFrame;

	protected Callback<GameOverlayActivated_t> gameOverlayActivatedCallback;

	protected Callback<UserStatsReceived_t> userStatsReceivedCallback;

	protected Callback<UserStatsStored_t> userStatsStoredCallback;

	protected Callback<UserAchievementStored_t> userAchievementStoredCallback;

	private void OnEnable()
	{
		if (SteamManager.Initialized)
		{
			gameID = new CGameID(SteamUtils.GetAppID());
			userStatsReceivedCallback = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
			userStatsStoredCallback = Callback<UserStatsStored_t>.Create(OnUserStatsStored);
			userAchievementStoredCallback = Callback<UserAchievementStored_t>.Create(OnAchievementStored);
			gameOverlayActivatedCallback = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);
			hasRequestedStats = false;
			areLocalStatsValid = false;
		}
	}

	private void Update()
	{
		if (!SteamManager.Initialized)
		{
			return;
		}
		if (!hasRequestedStats)
		{
			if (!SteamManager.Initialized)
			{
				hasRequestedStats = true;
				return;
			}
			bool flag = SteamUserStats.RequestCurrentStats();
			hasRequestedStats = flag;
		}
		if (areLocalStatsValid && storeStatsNextFrame)
		{
			bool flag2 = SteamUserStats.StoreStats();
			storeStatsNextFrame = !flag2;
		}
	}

	public void StoreStatsNextFrame()
	{
		storeStatsNextFrame = true;
	}

	private void OnGameOverlayActivated(GameOverlayActivated_t pCallback)
	{
		Debug.Log("Game overlay triggered");
		if (pCallback.m_bActive != 0)
		{
			Singleton<GameManager>.Instance.OnGameOverlayActivated();
			Debug.Log("Steam Overlay has been activated");
		}
		else
		{
			Singleton<GameManager>.Instance.OnGameOverlayClosed();
			Debug.Log("Steam Overlay has been closed");
		}
	}

	private void OnUserStatsReceived(UserStatsReceived_t pCallback)
	{
		if (!SteamManager.Initialized || (ulong)gameID != pCallback.m_nGameID)
		{
			return;
		}
		if (EResult.k_EResultOK == pCallback.m_eResult)
		{
			Debug.Log("Received stats and achievements from Steam\n");
			areLocalStatsValid = true;
			if (Singleton<AchievementManager>.Instance != null && Singleton<AchievementManager>.Instance is SteamAchievementManager)
			{
				(Singleton<AchievementManager>.Instance as SteamAchievementManager).LoadAchievements();
			}
			else
			{
				Debug.Log("SteamAchievementManager is missing from the scene!");
			}
		}
		else
		{
			Debug.Log("RequestStats - failed, " + pCallback.m_eResult);
		}
	}

	private void OnUserStatsStored(UserStatsStored_t pCallback)
	{
		if ((ulong)gameID == pCallback.m_nGameID)
		{
			if (EResult.k_EResultOK == pCallback.m_eResult)
			{
				Debug.Log("StoreStats - success");
			}
			else if (EResult.k_EResultInvalidParam == pCallback.m_eResult)
			{
				Debug.Log("StoreStats - some failed to validate");
				OnUserStatsReceived(new UserStatsReceived_t
				{
					m_eResult = EResult.k_EResultOK,
					m_nGameID = (ulong)gameID
				});
			}
			else
			{
				Debug.Log("StoreStats - failed, " + pCallback.m_eResult);
			}
		}
	}

	private void OnAchievementStored(UserAchievementStored_t pCallback)
	{
		if ((ulong)gameID == pCallback.m_nGameID)
		{
			if (pCallback.m_nMaxProgress == 0)
			{
				Debug.Log("Achievement '" + pCallback.m_rgchAchievementName + "' unlocked!");
				return;
			}
			Debug.Log("Achievement '" + pCallback.m_rgchAchievementName + "' progress callback, (" + pCallback.m_nCurProgress + "," + pCallback.m_nMaxProgress + ")");
		}
	}

	public void UIResponse_OpenFriends()
	{
		Debug.Log("Open Steam friends list");
		SteamFriends.ActivateGameOverlay("Friends");
	}
}
