using System.Collections;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
	private enum InputType
	{
		None,
		Dash,
		Attack,
		Throw
	}

	private class FailedInput
	{
		public InputType inputType;

		public float timeStamp;

		public FailedInput(InputType inputType, float timeStamp)
		{
			this.inputType = inputType;
			this.timeStamp = timeStamp;
		}
	}

	public Player player;

	public RewiredController rewiredController;

	private HintBar hintBar;

	public Vector2 inputMove = new Vector2(0f, 0f);

	public bool inputDash;

	public bool inputAttack;

	public bool inputThrow;

	public bool inputThrowHeld;

	public bool inputWave;

	public bool inputIsEnabled = true;

	public bool inputThrowIsBlocked;

	private CircularBuffer<FailedInput> inputBuffer = new CircularBuffer<FailedInput>(8);

	private static float inputBufferDashDelay = 0.25f;

	private static float inputBufferAttackDelay = 0.35f;

	private static float nearbyEnemyThreshold = 7f;

	public int roundsSinceLastInputReceived;

	public float timeSinceLastInputReceived;

	private void Start()
	{
		player = GetComponent<Player>();
		roundsSinceLastInputReceived = 0;
	}

	public void Init(HintBar hintBar = null)
	{
		StopAllCoroutines();
		inputIsEnabled = false;
		rewiredController = GetComponent<RewiredController>();
		this.hintBar = hintBar;
		if (hintBar.currentHint != HintBar.HintType.None)
		{
			hintBar.HideHint();
		}
		timeSinceLastInputReceived = 0f;
	}

	private void Update()
	{
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
		{
			if (!inputIsEnabled)
			{
				StartCoroutine(EnableInputAfterDelay());
			}
		}
		else
		{
			if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.TransitionIn)
			{
				inputIsEnabled = false;
				inputMove = Vector2.zero;
			}
			inputDash = false;
			inputAttack = false;
			inputThrow = false;
			inputThrowHeld = false;
			inputWave = false;
			if (hintBar != null && hintBar.currentHint != HintBar.HintType.None)
			{
				hintBar.HideHint();
			}
		}
		if (player.actorState == Actor.ActorState.Alive && inputIsEnabled)
		{
			if (inputAttack)
			{
				if (player.Attack())
				{
					inputBuffer.Clear();
				}
				else
				{
					inputBuffer.Add(new FailedInput(InputType.Attack, Singleton<TimeKeeper>.Instance.CurrentTime));
				}
				if (hintBar != null && hintBar.currentHint == HintBar.HintType.Attack)
				{
					hintBar.HideHint();
				}
			}
			if (inputThrow && !inputThrowIsBlocked)
			{
				if (player.ThrowDisc())
				{
					inputBuffer.Clear();
				}
				else if (player.thrownDiscs.Count == 0)
				{
					inputBuffer.Add(new FailedInput(InputType.Throw, Singleton<TimeKeeper>.Instance.CurrentTime));
				}
				if (hintBar != null && hintBar.currentHint == HintBar.HintType.Throw)
				{
					hintBar.HideHint();
				}
			}
			else if (inputThrow)
			{
				inputThrowIsBlocked = false;
			}
			if (inputThrowHeld && !inputThrowIsBlocked)
			{
				player.Aim();
			}
			if (inputWave)
			{
				player.Wave();
			}
			if (inputDash || inputAttack || inputThrow || inputThrowHeld || inputMove.magnitude > 0.1f)
			{
				roundsSinceLastInputReceived = 0;
				timeSinceLastInputReceived = 0f;
				player.dropInAIOnTimeOut = false;
			}
		}
		else if (!inputIsEnabled || player.actorState == Actor.ActorState.Dead)
		{
			inputMove = Vector2.zero;
		}
		if (player.actorState != Actor.ActorState.Dead && inputIsEnabled)
		{
			if (inputDash)
			{
				if (player.Dash())
				{
					inputBuffer.Clear();
				}
				else
				{
					inputBuffer.Add(new FailedInput(InputType.Dash, Singleton<TimeKeeper>.Instance.CurrentTime));
				}
				if (hintBar != null && hintBar.currentHint == HintBar.HintType.Dash)
				{
					hintBar.HideHint();
				}
				else if (hintBar != null && (hintBar.currentHint == HintBar.HintType.Teleport || hintBar.currentHint == HintBar.HintType.Decoy))
				{
					hintBar.HideHint();
				}
				if (Singleton<LevelManager>.Instance.level.isTutorialLevel && !player.hasCompletedTutorial && player.metadata.Throws == 0 && player.metadata.Attacks == 0)
				{
					StartCoroutine(ShowThrowHint());
				}
			}
		}
		else if (player.actorState == Actor.ActorState.Dead && inputIsEnabled && Singleton<SettingsManager>.Instance.teamMatch && Singleton<SettingsManager>.Instance.MatchSettings.teamRevives && (inputDash || inputAttack || inputThrow))
		{
			player.EmitSkullParticle();
		}
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Paused && !inputThrowHeld)
		{
			player.StopAiming();
		}
		if (inputThrow)
		{
			player.StopAiming();
		}
		inputThrow = false;
		bool flag = Singleton<GameManager>.Instance.IsInHidingPhase && player.IsSeeker;
		if (Time.timeScale == 0f && !flag)
		{
			StopVibration();
		}
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing && player.actorState == Actor.ActorState.Alive)
		{
			if (player.HasDiscs > 0 && hintBar != null && (hintBar.currentHint == HintBar.HintType.Telekinesis || hintBar.currentHint == HintBar.HintType.ControlledDisc))
			{
				hintBar.HideHint();
			}
		}
		else if (hintBar != null && hintBar.currentHint != HintBar.HintType.None)
		{
			hintBar.HideHint();
		}
		if (hintBar != null && hintBar.currentHint != HintBar.HintType.None && player.IsDisguised)
		{
			hintBar.HideHint();
		}
		if (inputThrow || inputDash || inputAttack || Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing || player.actorState != Actor.ActorState.Alive || inputBuffer.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < inputBuffer.Count; i++)
		{
			switch (inputBuffer[i].inputType)
			{
			case InputType.Dash:
				if (!(Singleton<TimeKeeper>.Instance.TimeSince(inputBuffer[i].timeStamp) > inputBufferDashDelay) && player.Dash())
				{
					inputBuffer.Clear();
					return;
				}
				break;
			case InputType.Attack:
				if (!(Singleton<TimeKeeper>.Instance.TimeSince(inputBuffer[i].timeStamp) > inputBufferAttackDelay) && player.Attack())
				{
					inputBuffer.Clear();
					return;
				}
				break;
			case InputType.Throw:
				if (!(Singleton<TimeKeeper>.Instance.TimeSince(inputBuffer[i].timeStamp) > inputBufferAttackDelay) && !inputThrowIsBlocked && player.thrownDiscs.Count == 0 && player.ThrowDisc())
				{
					inputBuffer.Clear();
					return;
				}
				break;
			}
		}
	}

	public IEnumerator EnableInputAfterDelay()
	{
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		inputIsEnabled = true;
	}

	public void StartVibration(float intensity, float duration)
	{
		if (rewiredController != null)
		{
			rewiredController.StartVibration(intensity, duration);
		}
	}

	public void SetVibration(float intensity)
	{
		if (rewiredController != null)
		{
			rewiredController.SetVibration(intensity);
		}
	}

	public void StopVibration()
	{
		if (rewiredController != null)
		{
			rewiredController.StopVibration();
		}
	}

	public IEnumerator ShowDashHint(float delay, bool onlyShowHintIfNeverDashed = false)
	{
		yield return new WaitForSeconds(delay);
		if (ShouldShowHint() && (!onlyShowHintIfNeverDashed || player.metadata.Dashes == 0))
		{
			hintBar.ShowHint(HintBar.HintType.Dash, 10f);
		}
	}

	private IEnumerator ShowThrowHint()
	{
		yield return new WaitForSeconds(3f);
		if (ShouldShowHint() && !player.hasCompletedTutorial && player.metadata.Throws < 4)
		{
			hintBar.ShowHint(HintBar.HintType.Throw, 3f);
		}
		else if (ShouldShowHint() && !player.hasCompletedTutorial && player.metadata.Attacks < 4)
		{
			hintBar.ShowHint(HintBar.HintType.Attack, 3f);
		}
	}

	public void ConsiderShowingTeleportHint()
	{
		if (ShouldShowHint() && player.activePowerup.HasPowerup(PowerupType.TeleportDisc) && player.IsItSafeToTeleport() && player.canDash && player.metadata.Teleports < 8)
		{
			StartCoroutine(ShowTeleportHint());
		}
	}

	public void ConsiderHidingTeleportHint()
	{
		if (hintBar.currentHint == HintBar.HintType.Teleport && !player.IsItSafeToTeleport())
		{
			hintBar.HideHint();
		}
	}

	private IEnumerator ShowTeleportHint()
	{
		yield return new WaitForSeconds(1f);
		if (ShouldShowHint() && player.activePowerup.HasPowerup(PowerupType.TeleportDisc) && player.IsItSafeToTeleport() && player.canDash && player.IsDisarmed)
		{
			hintBar.ShowHint(HintBar.HintType.Teleport, 3f);
		}
	}

	public void ConsiderShowingDecoyHint()
	{
		if (ShouldShowHint() && player.IsDashSafe(player.transform.up) && player.canDash && player.metadata.DecoysSpawned < 2 && !player.hasCreatedDecoy)
		{
			StartCoroutine(ShowDecoyHint());
		}
	}

	private IEnumerator ShowDecoyHint()
	{
		yield return new WaitForSeconds(3f);
		if (ShouldShowHint() && player.activePowerup.HasPowerup(PowerupType.Decoy) && player.IsDashSafe(player.transform.up) && player.canDash && player.metadata.DecoysSpawned < 2 && !player.hasCreatedDecoy)
		{
			hintBar.ShowHint(HintBar.HintType.Decoy, 3f);
		}
	}

	public void ConsiderShowingTelekinesisHint(Disc disc)
	{
		if (!player.IsStunned && !player.isShowingOff && disc != null && (disc.IsDroppedInHazard() || disc.IsWayOutOfBounds() || Singleton<LevelManager>.Instance.level.isTutorialLevel || ((Singleton<SettingsManager>.Instance.GameOptions.conventionMode || Singleton<SettingsManager>.Instance.demoMode) && Singleton<LevelManager>.Instance.matchIndex < 5)) && Singleton<SettingsManager>.Instance.MatchSettings.canRetrieveDiscs && player.metadata.TimeRetrieving < 10f)
		{
			StartCoroutine(ShowTelekinesisHint(disc));
		}
	}

	private IEnumerator ShowTelekinesisHint(Disc disc)
	{
		yield return new WaitForSeconds(1f);
		if (disc != null && disc.gameObject.activeSelf && !disc.IsGoldenDisc && ShouldShowHint() && player.HasDiscs == 0)
		{
			hintBar.ShowHint(HintBar.HintType.Telekinesis, 6f);
		}
	}

	public void ConsiderShowingControlledDiscHint(Disc disc)
	{
		if (!player.IsStunned && !player.isShowingOff && player.metadata.TimeRetrieving < 10f)
		{
			StartCoroutine(ShowControlledDiscHint(disc));
		}
	}

	private IEnumerator ShowControlledDiscHint(Disc disc)
	{
		yield return new WaitForSeconds(0.3f);
		if (disc != null && disc.canBeControlled && !disc.IsGoldenDisc && ShouldShowHint())
		{
			hintBar.ShowHint(HintBar.HintType.ControlledDisc, 4f);
		}
	}

	public void ConsiderHidingControlledDiscHint()
	{
		if (hintBar.currentHint == HintBar.HintType.ControlledDisc)
		{
			hintBar.HideHint();
		}
	}

	public void ConsiderShowingChipOutHint()
	{
		if (ShouldShowHint())
		{
			StartCoroutine(ShowChipOutHint());
		}
	}

	private IEnumerator ShowChipOutHint()
	{
		yield return new WaitForSeconds(1.5f);
		if (ShouldShowHint() && player.IsFrozen)
		{
			hintBar.ShowHint(HintBar.HintType.ChipOut, 5.5f);
		}
	}

	public void ConsiderHidingChipOutHint()
	{
		if (hintBar.currentHint == HintBar.HintType.ChipOut)
		{
			hintBar.HideHint();
		}
	}

	private bool ShouldShowHint()
	{
		if (!Singleton<SettingsManager>.Instance.GameOptions.showButtonHints)
		{
			return false;
		}
		if (player.IsSeeker)
		{
			return false;
		}
		if (hintBar == null)
		{
			return false;
		}
		if (hintBar.currentHint != HintBar.HintType.None)
		{
			return false;
		}
		if ((bool)GetComponent<PlayerAI>())
		{
			return false;
		}
		if (player.IsStunned || player.IsHidden || player.IsDisguised || player.isShowingOff)
		{
			return false;
		}
		if (player.actorState != Actor.ActorState.Alive)
		{
			return false;
		}
		if (!Singleton<LevelManager>.Instance.level.isTutorialLevel && EnemiesAreNearby())
		{
			return false;
		}
		if (Singleton<GameManager>.Instance.OpponentsLeftStandingNow() < 2)
		{
			return false;
		}
		return true;
	}

	public void HideCurrentHint()
	{
		if (hintBar != null && hintBar.currentHint != HintBar.HintType.None)
		{
			hintBar.HideHint();
		}
	}

	private bool EnemiesAreNearby()
	{
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			if (!(Singleton<GameManager>.Instance.players[i] == player) && (Singleton<GameManager>.Instance.players[i].transform.position - base.transform.position).magnitude < nearbyEnemyThreshold)
			{
				return true;
			}
		}
		return false;
	}
}
