using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Startup : MonoBehaviour
{
	public GameObject camera;

	public RectTransform spinner;

	public RectTransform fmodLogo;

	public RectTransform[] logos;

	private static float logoInitialDelay = 0.5f;

	private static float logoDisplayDuration = 0.7f;

	private static float showDuration = 0.3f;

	private static float hideDuration = 0.3f;

	private static float pauseBetweenLogosDuration = 0.05f;

	private int logoCount;

	public AnimationCurve showCurve;

	public AnimationCurve hideCurve;

	private void Awake()
	{
		Singleton<SettingsManager>.Instance.gameObject.AddComponent<SteamManager>();
	}

	private void Start()
	{
		Object.DontDestroyOnLoad(base.gameObject);
		Singleton<DataManager>.Instance.LoadPlatformData();
		Singleton<SettingsManager>.Instance.LoadOptions(OnOptionsLoaded);
		for (int i = 1; i < logos.Length; i++)
		{
			logos[i].GetComponent<CanvasGroup>().alpha = 0f;
		}
		StartCoroutine(ShowLogos());
	}

	private void OnOptionsLoaded(GameOptions gameOptions)
	{
		Debug.Log("OnOptionsLoaded");
		Singleton<SettingsManager>.Instance.LoadModifiedMatchSettings(OnMatchSettingsLoaded);
	}

	private void OnMatchSettingsLoaded(MatchSettings matchSettings)
	{
		Debug.Log("OnMatchSettingsLoaded");
	}

	private IEnumerator ShowLogos()
	{
		yield return new WaitForSecondsRealtime(logoInitialDelay);
		int numLogos = logos.Length;
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/startup");
		spinner.gameObject.SetActive(value: true);
		fmodLogo.gameObject.SetActive(value: true);
		for (int i = 0; i < numLogos; i++)
		{
			if (i == 0)
			{
				yield return new WaitForSecondsRealtime(showDuration);
				yield return StartCoroutine(LoadGameScene());
				if (spinner.gameObject.activeSelf)
				{
					spinner.DOScale(Vector3.zero, hideDuration).SetEase(Ease.InBack);
				}
			}
			else
			{
				yield return StartCoroutine(ShowLogo(logos[i]));
			}
			if (i < numLogos - 1)
			{
				yield return new WaitForSecondsRealtime(logoDisplayDuration);
				yield return StartCoroutine(HideLogo(logos[i]));
				yield return new WaitForSecondsRealtime(pauseBetweenLogosDuration);
			}
			else
			{
				yield return StartCoroutine(HideLogo(logos[i]));
				yield return new WaitForSecondsRealtime(pauseBetweenLogosDuration);
				Singleton<UIManager>.Instance.Init();
				camera.SetActive(value: false);
				base.gameObject.SetActive(value: false);
			}
			logoCount++;
		}
	}

	private IEnumerator ShowLogo(RectTransform logo)
	{
		logo.gameObject.SetActive(value: true);
		logo.GetComponent<CanvasGroup>().DOFade(1f, showDuration);
		yield return new WaitForSecondsRealtime(showDuration);
	}

	private IEnumerator HideLogo(RectTransform logo)
	{
		logo.GetComponent<CanvasGroup>().DOFade(0f, hideDuration);
		yield return new WaitForSecondsRealtime(hideDuration);
		logo.gameObject.SetActive(value: false);
	}

	private IEnumerator LoadGameScene()
	{
		float timer = 0f;
		if (!SceneManager.GetSceneByName("GameScene").isLoaded)
		{
			AsyncOperation loadOperation = SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Additive);
			loadOperation.allowSceneActivation = false;
			while (!loadOperation.isDone)
			{
				if (!(loadOperation.progress < 0.9f))
				{
					loadOperation.allowSceneActivation = true;
				}
				timer += Time.unscaledDeltaTime;
				yield return null;
			}
		}
		SceneManager.SetActiveScene(SceneManager.GetSceneByName("GameScene"));
		Singleton<GameManager>.Instance.Setup();
		yield return null;
	}
}
