using I2.Loc;
using Moments;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIButtonReplay : UIButton
{
	private TextMeshProUGUI text;

	private LayoutElement thumbnailLayout;

	public RawImage thumbnailReplay;

	private GameObject spinner;

	private GameObject progressBar;

	private Recorder recorder;

	public bool replayStartedProcessing;

	public bool replayStartedSaving;

	public bool replaySaved;

	protected override void Awake()
	{
		base.Awake();
		text = base.transform.Find("Label/Text").GetComponent<TextMeshProUGUI>();
		thumbnailLayout = base.transform.Find("Outline/Thumbnail").GetComponent<LayoutElement>();
		thumbnailReplay = base.transform.Find("Outline/Thumbnail/ThumbnailReplay").GetComponent<RawImage>();
		spinner = base.transform.Find("Outline/Spinner").gameObject;
		progressBar = base.transform.Find("Outline/Thumbnail/ProgressBar").gameObject;
	}

	public void PrepareThumbnailReplay()
	{
		thumbnailReplay.enabled = false;
		spinner.SetActive(value: true);
		replayStartedSaving = false;
		replaySaved = false;
		base.interactable = false;
		base.enabled = true;
		if (recorder == null)
		{
			recorder = Singleton<Camera3D>.Instance.GetComponent<Recorder>();
		}
		thumbnailLayout.preferredWidth = thumbnailLayout.preferredHeight * recorder.AspectRatio;
		ThumbnailReplayReady();
	}

	public void ThumbnailReplayReady()
	{
		base.interactable = true;
		spinner.SetActive(value: false);
		thumbnailReplay.enabled = true;
		text.text = LocalizationManager.GetTranslation("ui_savereplay");
		SetProgressBar(0f);
		UIButtonStyle component = GetComponent<UIButtonStyle>();
		if (component != null)
		{
			component.Bounce();
		}
	}

	public void SaveReplay()
	{
		text.text = LocalizationManager.GetTranslation("ui_preparingreplay");
		base.enabled = false;
		spinner.SetActive(value: true);
	}

	public void OnReplayProcessingDone()
	{
		replayStartedProcessing = false;
		replayStartedSaving = true;
		SetProgressBar(0.1f);
		text.text = LocalizationManager.GetTranslation("ui_savingreplay");
	}

	public void OnReplaySaved()
	{
		text.text = LocalizationManager.GetTranslation("ui_savedreplay");
		spinner.SetActive(value: false);
		replayStartedSaving = false;
		replayStartedProcessing = false;
		replaySaved = true;
		base.enabled = true;
		SetProgressBar(1f);
	}

	public void SetProgressBar(float progress)
	{
		progress = Mathf.Clamp01(progress);
		if (progress == 0f)
		{
			progressBar.gameObject.SetActive(value: false);
		}
		else
		{
			progressBar.gameObject.SetActive(value: true);
		}
		progressBar.transform.localScale = new Vector3(progress, 1f, 1f);
	}
}
