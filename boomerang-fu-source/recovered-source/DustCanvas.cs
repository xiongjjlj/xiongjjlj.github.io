using UnityEngine;

public class DustCanvas : MonoBehaviour
{
	public RectTransform shakeRoot;

	public Canvas[] dustFrames;

	private int currentFrameIndex;

	private float lastFrameTimestamp;

	private static float timeBetweenFrames = 0.05f;

	private void Start()
	{
		if (dustFrames.Length != 0)
		{
			dustFrames[0].enabled = true;
		}
		for (int i = 1; i < dustFrames.Length; i++)
		{
			dustFrames[i].enabled = false;
		}
	}

	private void Update()
	{
		if (Singleton<TimeKeeper>.Instance.TimeSinceUnscaled(lastFrameTimestamp) > timeBetweenFrames)
		{
			lastFrameTimestamp = Singleton<TimeKeeper>.Instance.CurrentTimeUnscaled;
			dustFrames[currentFrameIndex].enabled = false;
			currentFrameIndex++;
			if (currentFrameIndex >= dustFrames.Length)
			{
				currentFrameIndex = 0;
			}
			dustFrames[currentFrameIndex].enabled = true;
			if (shakeRoot != null)
			{
				shakeRoot.anchoredPosition = Random.insideUnitCircle * 100f;
			}
		}
	}
}
