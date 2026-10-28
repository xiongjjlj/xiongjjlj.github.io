using Moments;
using UnityEngine;

public class GIFRecorder : MonoBehaviour
{
	public Recorder recorder;

	public float m_Progress;

	public string m_LastFile = "";

	public bool m_IsSaving;

	private float time;

	private bool saveStarted;

	private int m_lastID;

	public bool isCreatingPalette;

	private void Start()
	{
		recorder = GetComponent<Recorder>();
		recorder.OnPreProcessingDone = OnProcessingDone;
		recorder.OnFileSaveProgress = OnFileSaveProgress;
		recorder.OnFileSaved = OnFileSaved;
	}

	private void OnProcessingDone(int id)
	{
		m_lastID = id;
		m_IsSaving = true;
		saveStarted = true;
		isCreatingPalette = false;
		UIMenuRoundEnd uIMenuRoundEnd = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.RoundEnd) as UIMenuRoundEnd;
		if (uIMenuRoundEnd != null && uIMenuRoundEnd.replayButton != null)
		{
			uIMenuRoundEnd.OnReplayProcessingDone();
		}
	}

	private void OnFileSaveProgress(int id, float percent)
	{
		if (id == m_lastID)
		{
			m_Progress = 0.05f + percent * 0.95f;
		}
	}

	private void OnFileSaved(int id, string filepath)
	{
		m_LastFile = filepath;
		if (id == m_lastID)
		{
			m_IsSaving = false;
			isCreatingPalette = false;
		}
	}

	private void OnDestroy()
	{
	}

	public void StartRecording()
	{
		if (!Singleton<SettingsManager>.Instance.showcaseMode)
		{
			recorder.FlushMemory();
			recorder.Record();
		}
	}

	public void PauseRecording()
	{
		recorder.Pause();
	}

	public void UnPauseRecording()
	{
		recorder.Record();
	}

	public void SaveRecordingToDisk()
	{
		if (!Singleton<SettingsManager>.Instance.showcaseMode)
		{
			recorder.Save();
			m_Progress = 0f;
			Debug.Log("Gif save process started!");
			isCreatingPalette = true;
			time = Time.realtimeSinceStartup;
		}
	}

	public void Update()
	{
		if (saveStarted && !m_IsSaving)
		{
			Debug.Log("Save time: " + (Time.realtimeSinceStartup - time));
			saveStarted = false;
		}
	}
}
