using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
	public enum GameplayMusicState
	{
		Match,
		RoundEnd
	}

	private class SoundCounter
	{
		public int count;

		public float timeLastPlayed;

		public float cooldown;

		public SoundCounter(float cooldown)
		{
			timeLastPlayed = Singleton<TimeKeeper>.Instance.CurrentTime;
			this.cooldown = cooldown;
		}
	}

	public Transform listener;

	private EventInstance invalidInstance;

	private EventInstance sfxBusVolumeSnapshot;

	private EventInstance musicBusVolumeSnapshot;

	private EventInstance musicFilterSnapshot;

	private EventInstance timeScaleSnapshot;

	public EventInstance menuMusic;

	public EventInstance gameplayMusic;

	private static float stingRequestDuration = 0.2f;

	[SerializeField]
	private List<float> stingRequestsTimestamps = new List<float>(1);

	private List<string> gameplayMusicPlaylist = new List<string> { "event:/music/gameplay_skulls", "event:/music/gameplay_trap", "event:/music/gameplay_stepup", "event:/music/gameplay_flutey", "event:/music/gameplay_shuffle", "event:/music/gameplay_jungle", "event:/music/gameplay_surf" };

	private bool firstMatchOfSession = true;

	private bool someoneHasWonThisMatch;

	private int gameplayMusicIndex;

	private Dictionary<string, SoundCounter> soundEventCounters = new Dictionary<string, SoundCounter>();

	private void Start()
	{
		sfxBusVolumeSnapshot = RuntimeManager.CreateInstance("snapshot:/SFXVolume");
		sfxBusVolumeSnapshot.start();
		SetSFXBusVolume(Singleton<SettingsManager>.Instance.GameOptions.sfxVolume);
		musicBusVolumeSnapshot = RuntimeManager.CreateInstance("snapshot:/MusicVolume");
		musicBusVolumeSnapshot.start();
		SetMusicBusVolume(Singleton<SettingsManager>.Instance.GameOptions.musicVolume);
		musicFilterSnapshot = RuntimeManager.CreateInstance("snapshot:/MusicFilter");
		musicFilterSnapshot.start();
		SetMusicFilter(0f);
		timeScaleSnapshot = RuntimeManager.CreateInstance("snapshot:/TimeScale");
		timeScaleSnapshot.start();
		SetTimeScale(1f);
	}

	private void Update()
	{
		if (stingRequestsTimestamps.Count > 0)
		{
			if (Singleton<TimeKeeper>.Instance.TimeSinceUnscaled(stingRequestsTimestamps[0]) <= stingRequestDuration)
			{
				RuntimeManager.StudioSystem.setParameterByName("sting", 1f);
				return;
			}
			stingRequestsTimestamps.Clear();
			RuntimeManager.StudioSystem.setParameterByName("sting", 0f);
		}
	}

	public void SetMusicBusVolume(float value)
	{
		if (musicBusVolumeSnapshot.isValid())
		{
			musicBusVolumeSnapshot.setParameterByName("musicBusVolume", value);
		}
	}

	public void SetSFXBusVolume(float value)
	{
		if (sfxBusVolumeSnapshot.isValid())
		{
			sfxBusVolumeSnapshot.setParameterByName("sfxBusVolume", value);
		}
	}

	public void SetMusicFilter(float value)
	{
		if (musicFilterSnapshot.isValid())
		{
			musicFilterSnapshot.setParameterByName("musicFilter", value);
		}
	}

	public void PlayOneShotAttached(string eventName, GameObject go)
	{
		RuntimeManager.PlayOneShotAttached(eventName, go);
	}

	public void PlayOneShotAt(string eventName, Vector3 pos)
	{
		RuntimeManager.PlayOneShot(eventName, pos);
	}

	public EventInstance PlayOneShot(string eventName)
	{
		EventInstance result = RuntimeManager.CreateInstance(eventName);
		if (result.isValid())
		{
			result.start();
			result.release();
		}
		return result;
	}

	public EventInstance PlayLooping(string eventName, Transform tf = null)
	{
		EventInstance result = CreateLoop(eventName, tf);
		result.start();
		return result;
	}

	public EventInstance CreateLoop(string eventName, Transform tf = null)
	{
		if (string.IsNullOrEmpty(eventName))
		{
			Debug.LogWarning(eventName + " is null or empty!");
			return invalidInstance;
		}
		EventInstance result = RuntimeManager.CreateInstance(eventName);
		if (result.isValid() && tf != null)
		{
			result.set3DAttributes(tf.To3DAttributes());
		}
		if (tf != null)
		{
			RuntimeManager.AttachInstanceToGameObject(result, tf, (Rigidbody)null);
		}
		return result;
	}

	public EventInstance CreateLoop(string eventName, Vector3 pos)
	{
		if (string.IsNullOrEmpty(eventName))
		{
			Debug.LogWarning(eventName + " is null or empty!");
			return invalidInstance;
		}
		EventInstance result = RuntimeManager.CreateInstance(eventName);
		if (result.isValid())
		{
			result.set3DAttributes(pos.To3DAttributes());
		}
		return result;
	}

	public void StopLooping(EventInstance instance, bool allowFadeOut = true)
	{
		if (instance.isValid())
		{
			instance.stop((!allowFadeOut) ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			instance.release();
		}
	}

	public void PlayOneShotCounter(string eventName, float cooldown = 3f, Transform tf = null)
	{
		if (string.IsNullOrEmpty(eventName))
		{
			return;
		}
		EventInstance eventInstance = RuntimeManager.CreateInstance(eventName);
		if (!eventInstance.isValid())
		{
			return;
		}
		if (tf != null)
		{
			eventInstance.set3DAttributes(tf.To3DAttributes());
		}
		if (!soundEventCounters.ContainsKey(eventName))
		{
			soundEventCounters.Add(eventName, new SoundCounter(cooldown));
		}
		else
		{
			if (Singleton<TimeKeeper>.Instance.TimeSinceUnscaled(soundEventCounters[eventName].timeLastPlayed) < soundEventCounters[eventName].cooldown)
			{
				soundEventCounters[eventName].count++;
			}
			else
			{
				soundEventCounters[eventName].count = 0;
			}
			soundEventCounters[eventName].timeLastPlayed = Singleton<TimeKeeper>.Instance.CurrentTimeUnscaled;
		}
		eventInstance.setParameterByName("counter", soundEventCounters[eventName].count);
		eventInstance.start();
		eventInstance.release();
	}

	public void PlayOneShotAndSetParameter(string eventName, string parameterName, float value, Transform tf = null)
	{
		if (string.IsNullOrEmpty(eventName))
		{
			return;
		}
		EventInstance eventInstance = RuntimeManager.CreateInstance(eventName);
		if (eventInstance.isValid())
		{
			eventInstance.setParameterByName(parameterName, value);
			if (tf != null)
			{
				eventInstance.set3DAttributes(tf.To3DAttributes());
			}
			eventInstance.start();
			eventInstance.release();
		}
	}

	public void PlayOneShotAndSetParameterAt(string eventName, string parameterName, float value, Vector3 position)
	{
		if (!string.IsNullOrEmpty(eventName))
		{
			EventInstance eventInstance = RuntimeManager.CreateInstance(eventName);
			if (eventInstance.isValid())
			{
				eventInstance.setParameterByName(parameterName, value);
				eventInstance.set3DAttributes(position.To3DAttributes());
				eventInstance.start();
				eventInstance.release();
			}
		}
	}

	public void SetEventInstanceParameter(EventInstance eventInstance, string name, float value)
	{
		if (!string.IsNullOrEmpty(name) && eventInstance.isValid())
		{
			eventInstance.setParameterByName(name, value);
		}
	}

	public void PlayMenuMusic(bool victory = false)
	{
		if (menuMusic.isValid())
		{
			menuMusic.getPlaybackState(out var state);
			if (state == PLAYBACK_STATE.PLAYING)
			{
				return;
			}
		}
		menuMusic = CreateLoop("event:/music/menu");
		menuMusic.setParameterByName("musicSection", victory ? 1 : 0);
		menuMusic.start();
	}

	public void ProgressMenuMusic()
	{
		if (menuMusic.isValid())
		{
			menuMusic.setParameterByName("musicSection", 0f);
		}
	}

	public void StopMenuMusic()
	{
		if (menuMusic.isValid())
		{
			menuMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
		}
	}

	public void PlayGameplayMusic()
	{
		StopMenuMusic();
		StopGameplayMusic();
		someoneHasWonThisMatch = false;
		if (firstMatchOfSession)
		{
			for (int i = 0; i < gameplayMusicPlaylist.Count; i++)
			{
				string value = gameplayMusicPlaylist[i];
				int index = Random.Range(i, gameplayMusicPlaylist.Count);
				gameplayMusicPlaylist[i] = gameplayMusicPlaylist[index];
				gameplayMusicPlaylist[index] = value;
			}
			gameplayMusicIndex = 0;
		}
		else
		{
			gameplayMusicIndex++;
		}
		gameplayMusic = PlayLooping(gameplayMusicPlaylist[gameplayMusicIndex % gameplayMusicPlaylist.Count], base.transform);
		if (gameplayMusic.isValid())
		{
			gameplayMusic.setParameterByName("musicSection", 0f);
		}
		firstMatchOfSession = false;
	}

	public void GameplayMusicVictory()
	{
		gameplayMusic.setParameterByName("musicSection", 1f);
	}

	public void GameplayMusicEnd()
	{
		gameplayMusic.setParameterByName("musicSection", 2f);
	}

	public void StopGameplayMusic()
	{
		if (gameplayMusic.isValid())
		{
			gameplayMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
		}
	}

	public void PlaySting()
	{
		if (stingRequestsTimestamps.Count > 0)
		{
			stingRequestsTimestamps[0] = Singleton<TimeKeeper>.Instance.CurrentTimeUnscaled;
		}
		else
		{
			stingRequestsTimestamps.Add(Singleton<TimeKeeper>.Instance.CurrentTimeUnscaled);
		}
	}

	public void SetGameplayMusicState(GameplayMusicState state)
	{
		switch (state)
		{
		case GameplayMusicState.RoundEnd:
			if (gameplayMusic.isValid())
			{
				gameplayMusic.setParameterByName("roundEnd", 1f);
			}
			break;
		case GameplayMusicState.Match:
			if (gameplayMusic.isValid())
			{
				gameplayMusic.setParameterByName("roundEnd", 0f);
			}
			break;
		}
	}

	public void SetTimeScale(float t)
	{
		RuntimeManager.StudioSystem.setParameterByName("timeScale", t);
	}

	public void StopAllWorldSounds()
	{
		Bus bus = RuntimeManager.GetBus("bus:/SFX/Diegetic");
		if (bus.isValid())
		{
			bus.stopAllEvents(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
		}
		Bus bus2 = RuntimeManager.GetBus("bus:/SFX/Atmos");
		if (bus2.isValid())
		{
			bus2.stopAllEvents(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
		}
	}

	public void WinMatch()
	{
		if (!someoneHasWonThisMatch)
		{
			PlayOneShot("event:/ui/circlewipe_victory");
			PlayOneShot("event:/music/sting_victory");
			StopGameplayMusic();
			someoneHasWonThisMatch = true;
		}
	}
}
