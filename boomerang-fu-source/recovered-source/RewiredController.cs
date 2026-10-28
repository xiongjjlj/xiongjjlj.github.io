using System.Collections;
using Rewired;
using UnityEngine;

public class RewiredController : MonoBehaviour
{
	private int _rewiredID;

	public Rewired.Player rewiredPlayer;

	private PlayerInput playerInput;

	private IEnumerator vibrationRoutine;

	[SerializeField]
	public int rewiredID
	{
		get
		{
			return _rewiredID;
		}
		set
		{
			_rewiredID = value;
			rewiredPlayer = ReInput.players.GetPlayer(_rewiredID);
		}
	}

	private void Awake()
	{
		playerInput = GetComponent<PlayerInput>();
	}

	private void Update()
	{
		if (Singleton<SettingsManager>.Instance.localizationMode && rewiredPlayer.GetButtonDown(5) && rewiredPlayer.GetButton(3))
		{
			Singleton<SettingsManager>.Instance.matchScoreGoal = 1;
		}
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing || Singleton<GameManager>.Instance.gameState == GameManager.GameState.TransitionIn)
		{
			playerInput.inputMove.x = rewiredPlayer.GetAxis(0);
			playerInput.inputMove.y = rewiredPlayer.GetAxis(1);
			playerInput.inputDash = rewiredPlayer.GetButtonDown(2);
			playerInput.inputAttack = rewiredPlayer.GetButtonDown(3);
			playerInput.inputThrow = rewiredPlayer.GetButtonUp(4);
			playerInput.inputThrowHeld = rewiredPlayer.GetButton(4);
			playerInput.inputWave = rewiredPlayer.GetButton(5);
		}
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing || Singleton<GameManager>.Instance.gameState == GameManager.GameState.TransitionOut || Singleton<GameManager>.Instance.gameState == GameManager.GameState.TransitionIn)
		{
			if (rewiredPlayer.GetButtonShortPressDown(6))
			{
				Singleton<UIManager>.Instance.Pause(rewiredPlayer);
			}
			else if (rewiredPlayer.GetButtonShortPressDown(22))
			{
				UIMenuInstructions uIMenuInstructions = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Instructions) as UIMenuInstructions;
				if (uIMenuInstructions != null)
				{
					Singleton<UIManager>.Instance.PushMenu(uIMenuInstructions);
					uIMenuInstructions.ShowInstructionPanels();
				}
			}
		}
		playerInput.inputMove = Vector2.ClampMagnitude(playerInput.inputMove, 1f);
	}

	private void OnDestroy()
	{
		StopAllCoroutines();
		StopVibration();
	}

	public void StartVibration(float intensity, float duration)
	{
		if (Singleton<SettingsManager>.Instance.GameOptions.vibrationEnabled)
		{
			vibrationRoutine = Vibrate(intensity, duration);
			StartCoroutine(vibrationRoutine);
		}
	}

	public void StopVibration()
	{
		if (vibrationRoutine != null)
		{
			StopCoroutine(vibrationRoutine);
		}
		foreach (Joystick joystick in rewiredPlayer.controllers.Joysticks)
		{
			if (joystick.supportsVibration)
			{
				joystick.SetVibration(0f, 0f);
			}
		}
	}

	public void SetVibration(float intensity)
	{
		if (Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.Switch)
		{
			intensity *= 0.5f;
		}
		foreach (Joystick joystick in rewiredPlayer.controllers.Joysticks)
		{
			if (joystick.supportsVibration)
			{
				joystick.SetVibration(intensity, intensity);
			}
		}
	}

	private IEnumerator Vibrate(float intensity, float duration)
	{
		if (Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.Switch)
		{
			intensity *= 0.5f;
			duration *= 0.25f;
		}
		foreach (Joystick j in rewiredPlayer.controllers.Joysticks)
		{
			if (j.supportsVibration)
			{
				j.SetVibration(intensity, intensity);
				if (duration > 0f)
				{
					yield return new WaitForSecondsRealtime(duration);
					j.SetVibration(0f, 0f);
				}
				else
				{
					yield return new WaitForEndOfFrame();
				}
			}
		}
	}
}
