using System.Collections;
using DG.Tweening;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.UI;

public class UIProgressBar : MonoBehaviour
{
	public float progress;

	private Image background;

	public RectTransform fillBar;

	private static float fillWidth = 820f;

	private static float fillSpeed = 0.8f;

	private Color backgroundColor;

	public ParticleSystem progressPFX;

	public EventInstance progressRiserInstance;

	private void Start()
	{
		background = GetComponent<Image>();
		backgroundColor = background.color;
	}

	public void Init(float currentProgress)
	{
		progress = currentProgress % 1f;
		fillBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, progress * fillWidth);
		base.transform.DOComplete();
		base.transform.localScale = Vector3.zero;
		base.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
	}

	public IEnumerator Animate(float prevProgress, float targetProgress)
	{
		progress = prevProgress % 1f;
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/progress_show");
		Singleton<UIManager>.Instance.CameraShake(new Vector2(0f, 1f), 10f, 0.15f);
		yield return new WaitForSecondsRealtime(0.5f);
		progressRiserInstance = Singleton<AudioManager>.Instance.CreateLoop("event:/ui/progress_riser");
		if (progressRiserInstance.isValid())
		{
			progressRiserInstance.start();
		}
		progressPFX.Play();
		while (prevProgress < targetProgress)
		{
			progress += Time.unscaledDeltaTime * fillSpeed;
			prevProgress += Time.unscaledDeltaTime * fillSpeed;
			fillBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Clamp01(progress) * fillWidth);
			if (progress >= 1f)
			{
				progressPFX.Stop(withChildren: false, ParticleSystemStopBehavior.StopEmittingAndClear);
				Singleton<AudioManager>.Instance.StopLooping(progressRiserInstance);
				base.transform.DOComplete();
				base.transform.localScale = Vector3.one * 1.1f;
				base.transform.DOScale(1f, 0.3f).SetEase(Ease.InSine);
				background.color = Color.white;
				Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/progress_complete");
				Singleton<UIManager>.Instance.CameraShake(new Vector2(0f, 1.5f), 10f, 0.2f);
				yield return new WaitForSecondsRealtime(0.1f);
				yield return background.DOColor(backgroundColor, 0.4f);
				yield return new WaitForSecondsRealtime(0.4f);
				if (Singleton<GameManager>.Instance.nextCharactersToUnlock.Count + Singleton<GameManager>.Instance.nextHatsToUnlock.Count > 0)
				{
					UIMenuUnlocked uIMenuUnlocked = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Unlocked) as UIMenuUnlocked;
					if (Singleton<GameManager>.Instance.nextCharactersToUnlock.Count > 0)
					{
						uIMenuUnlocked.UnlockCharacter(Singleton<GameManager>.Instance.nextCharactersToUnlock[0]);
						Singleton<GameManager>.Instance.nextCharactersToUnlock.RemoveAt(0);
					}
					else if (Singleton<GameManager>.Instance.nextHatsToUnlock.Count > 0)
					{
						uIMenuUnlocked.UnlockHat(Singleton<GameManager>.Instance.nextHatsToUnlock[0]);
						Singleton<GameManager>.Instance.nextHatsToUnlock.RemoveAt(0);
					}
					Singleton<UIManager>.Instance.AddOverlayMenu(uIMenuUnlocked);
					while (Singleton<UIManager>.Instance.GetCurrentMenu() != null && Singleton<UIManager>.Instance.GetCurrentMenu().MenuID == UIMenuBase.MenuType.Unlocked)
					{
						yield return null;
					}
				}
				progress %= 1f;
				fillBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Clamp01(progress) * fillWidth);
				if (prevProgress < targetProgress)
				{
					progressPFX.Play();
					progressRiserInstance = Singleton<AudioManager>.Instance.CreateLoop("event:/ui/progress_riser");
					if (progressRiserInstance.isValid())
					{
						progressRiserInstance.start();
					}
				}
			}
			yield return null;
		}
		progressPFX.Stop(withChildren: false);
		Singleton<AudioManager>.Instance.StopLooping(progressRiserInstance);
		progress = targetProgress % 1f;
		fillBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, progress * fillWidth);
		yield return new WaitForSecondsRealtime(0.6f);
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/progress_show");
		base.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack);
		yield return new WaitForSecondsRealtime(0.2f);
		base.gameObject.SetActive(value: false);
	}
}
