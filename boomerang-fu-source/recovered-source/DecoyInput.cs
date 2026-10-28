using System.Collections;
using UnityEngine;

public class DecoyInput : MonoBehaviour
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

	public Decoy decoy;

	public Vector2 inputMove = new Vector2(0f, 0f);

	public bool inputDash;

	public bool inputWave;

	public bool inputIsEnabled = true;

	private CircularBuffer<FailedInput> inputBuffer = new CircularBuffer<FailedInput>(8);

	private static float inputBufferDashDelay = 0.25f;

	private static float inputBufferAttackDelay = 0.35f;

	private static float nearbyEnemyThreshold = 7f;

	public int roundsSinceLastInputReceived;

	public float timeSinceLastInputReceived;

	private void Start()
	{
		decoy = GetComponent<Decoy>();
		roundsSinceLastInputReceived = 0;
	}

	public void Init()
	{
		StopAllCoroutines();
		inputIsEnabled = false;
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
			inputWave = false;
		}
		if (decoy.actorState == Actor.ActorState.Alive && inputIsEnabled)
		{
			if (inputDash || inputMove.magnitude > 0.1f)
			{
				roundsSinceLastInputReceived = 0;
				timeSinceLastInputReceived = 0f;
			}
		}
		else if (!inputIsEnabled || decoy.actorState == Actor.ActorState.Dead)
		{
			inputMove = Vector2.zero;
		}
		if (decoy.actorState != Actor.ActorState.Dead && inputIsEnabled && inputDash)
		{
			if (decoy.Dash())
			{
				inputBuffer.Clear();
			}
			else
			{
				inputBuffer.Add(new FailedInput(InputType.Dash, Singleton<TimeKeeper>.Instance.CurrentTime));
			}
		}
		if (inputDash || Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing || decoy.actorState != Actor.ActorState.Alive || inputBuffer.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < inputBuffer.Count; i++)
		{
			if (inputBuffer[i].inputType == InputType.Dash && !(Singleton<TimeKeeper>.Instance.TimeSince(inputBuffer[i].timeStamp) > inputBufferDashDelay) && decoy.Dash())
			{
				inputBuffer.Clear();
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
}
