using UnityEngine;

public class TimeKeeper : Singleton<TimeKeeper>
{
	private float lastTimeScale = 1f;

	[SerializeField]
	private float freezeTimer;

	public bool freezeFrame;

	public float targetScale = 1f;

	[SerializeField]
	private float slowMoTimer;

	[SerializeField]
	private float slowScale = 0.2f;

	[SerializeField]
	private float slowMoInDur;

	[SerializeField]
	private float slowMoHoldDur;

	[SerializeField]
	private float slowMoOutDur;

	[SerializeField]
	public bool slowMo;

	private bool slowMoIsLockedIn;

	public bool dramaEnabled;

	private float dramaCooldown;

	private float dramaCooldownDur = 2f;

	private bool timeFrozen;

	private float timeCounter;

	private float unscaledTimeCounter;

	public string slowMoState;

	public float CurrentTime => timeCounter;

	public float CurrentTimeUnscaled => unscaledTimeCounter;

	private void Update()
	{
		timeCounter += Time.deltaTime;
		unscaledTimeCounter += Time.unscaledDeltaTime;
		switch (Singleton<GameManager>.Instance.gameState)
		{
		case GameManager.GameState.Menu:
		case GameManager.GameState.Paused:
		case GameManager.GameState.Loading:
			Singleton<AudioManager>.Instance.SetTimeScale(1f);
			break;
		case GameManager.GameState.Playing:
		case GameManager.GameState.TransitionOut:
		case GameManager.GameState.TransitionIn:
			UpdateTime();
			break;
		}
	}

	private void UpdateTime()
	{
		Time.timeScale = targetScale;
		if (slowMoTimer < slowMoInDur + slowMoHoldDur + slowMoOutDur)
		{
			slowMo = true;
			slowMoTimer += Time.unscaledDeltaTime;
			float num = 0f;
			if (slowMoTimer < slowMoInDur)
			{
				num = Mathf.Clamp01(slowMoTimer / slowMoInDur);
				num = Easing.EaseOut(num);
				slowMoState = "Lerp In";
			}
			else if (slowMoTimer < slowMoInDur + slowMoHoldDur || slowMoIsLockedIn)
			{
				num = 1f;
				slowMoState = "Hold";
			}
			else if (!slowMoIsLockedIn)
			{
				num = 1f - Mathf.Clamp01((slowMoTimer - slowMoHoldDur - slowMoInDur) / slowMoOutDur);
				num = Easing.EaseIn(num);
				slowMoState = "Lerp Out";
			}
			Time.timeScale = Mathf.Lerp(targetScale, slowScale, num);
			Singleton<CameraFX>.Instance.UpdateVignette(Mathf.Clamp01(num));
		}
		else if (slowMo && !slowMoIsLockedIn)
		{
			StopSlowMo();
		}
		Singleton<AudioManager>.Instance.SetTimeScale(Time.timeScale);
		if (freezeTimer > 0f)
		{
			_ = freezeFrame;
			freezeFrame = true;
			freezeTimer -= Time.unscaledDeltaTime;
			Time.timeScale = 0f;
		}
		else if (freezeFrame)
		{
			freezeFrame = false;
			Time.timeScale = lastTimeScale;
		}
	}

	private void StopSlowMo()
	{
		slowMoIsLockedIn = false;
		slowMo = false;
		slowMoTimer = float.MaxValue;
		slowMoInDur = 0f;
		slowMoHoldDur = 0f;
		slowMoOutDur = 0f;
		Time.timeScale = targetScale;
	}

	public void SlowMoEffect(float holdDuration, float inDuration = 0.1f, float slowScale = 0.2f, bool forceSlowMo = false)
	{
		if ((!forceSlowMo && !Singleton<SettingsManager>.Instance.GameOptions.enableSlowMo) || slowMoIsLockedIn)
		{
			return;
		}
		if (slowScale < Time.timeScale || Time.timeScale == 0f)
		{
			this.slowScale = slowScale;
		}
		if (!slowMo)
		{
			slowMoInDur = inDuration;
			slowMoTimer = 0f;
		}
		slowMoOutDur = holdDuration;
		if (slowMo)
		{
			if (slowMoTimer > slowMoInDur + slowMoHoldDur - slowMoHoldDur * 0.15f)
			{
				slowMoTimer = slowMoInDur;
				slowMoHoldDur = holdDuration * 0.4f;
			}
			slowMoHoldDur += holdDuration * 0.2f;
		}
		else
		{
			slowMoHoldDur += holdDuration;
		}
	}

	public void SlowMoTimeWarp(float holdDuration, float inDuration = 0.1f, float outDuration = 1f, float slowScale = 0.2f)
	{
		if (!slowMoIsLockedIn)
		{
			this.slowScale = slowScale;
			slowMoInDur = inDuration;
			slowMoOutDur = outDuration;
			holdDuration += slowMoInDur + slowMoOutDur;
			slowMoTimer = holdDuration;
			slowMoHoldDur = holdDuration;
		}
	}

	public void EndGoldenDiscRound()
	{
		SlowMoEffect(10f, 0.5f, 0.2f, forceSlowMo: true);
		slowMoIsLockedIn = true;
	}

	public void EndMatch()
	{
		SlowMoEffect(10f, 0.5f, 0.015f, forceSlowMo: true);
		slowMoIsLockedIn = true;
	}

	public void PauseTime()
	{
		if (Time.timeScale != 0f)
		{
			lastTimeScale = Time.timeScale;
		}
		Time.timeScale = 0f;
	}

	public void UnPauseTime()
	{
		Time.timeScale = lastTimeScale;
	}

	public void FreezeFrame(float duration)
	{
		if (Singleton<SettingsManager>.Instance.GameOptions.freezeFramesEnabled)
		{
			lastTimeScale = Time.timeScale;
			freezeTimer = duration;
		}
	}

	public void SetTimeScale(float scale)
	{
		StopSlowMo();
		Time.timeScale = scale;
		Singleton<CameraFX>.Instance.UpdateVignette(scale);
	}

	public void ResetTimeScale()
	{
		StopSlowMo();
		targetScale = 1f;
		slowScale = 0.2f;
		Time.timeScale = 1f;
		Singleton<CameraFX>.Instance.UpdateVignette(0f);
	}

	public float TimeSince(float time)
	{
		return timeCounter - time;
	}

	public float TimeSinceUnscaled(float time)
	{
		return unscaledTimeCounter - time;
	}
}
