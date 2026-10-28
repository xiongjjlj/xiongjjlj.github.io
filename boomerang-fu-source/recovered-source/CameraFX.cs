using System.Collections;
using UnityEngine;
using UnityEngine.PostProcessing.Utilities;
using UnityStandardAssets.ImageEffects;

public class CameraFX : Singleton<CameraFX>
{
	private PostProcessingController postProcessing;

	private BlurOptimized blur;

	public Color TransitionColorDefault = Color.black;

	public float DurationEnter = 0.5f;

	public float DurationExit = 0.5f;

	public Color TransitionColorShowcase = Color.black;

	public bool StartSceneOnEnterState = true;

	private Coroutine _transitionCoroutine;

	private float _step;

	public Material _transitionEnterMaterial;

	public Material _transitionExitMaterial;

	public Shader circleWipe;

	private BasicBlit _blit;

	private int _material_StepID;

	private void Start()
	{
		_material_StepID = Shader.PropertyToID("_Step");
		_blit = base.gameObject.AddComponent<BasicBlit>();
		_blit.enabled = false;
		CreateMaterials(TransitionColorDefault);
		postProcessing = GetComponent<PostProcessingController>();
		blur = GetComponent<BlurOptimized>();
		blur.enabled = false;
		CheckQualitySettings();
		UnBlur();
	}

	public void ResetCamera()
	{
		Singleton<Camera3D>.Instance.Reset();
		UpdateVignette(0f);
	}

	public void UpdateVignette(float amt)
	{
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing && postProcessing != null)
		{
			if (Singleton<SettingsManager>.Instance.showcaseMode)
			{
				postProcessing.vignette.intensity = 0.5f;
			}
			else
			{
				postProcessing.vignette.intensity = 0.4f + amt * 0.06f;
			}
		}
	}

	public void Blur()
	{
		if (Singleton<SettingsManager>.Instance.currentPlatform != SettingsManager.Platform.Switch)
		{
			blur.enabled = true;
		}
	}

	public void UnBlur()
	{
		if (Singleton<SettingsManager>.Instance.currentPlatform != SettingsManager.Platform.Switch)
		{
			blur.enabled = false;
		}
	}

	public void CreateMaterials(Color targetColor)
	{
		_transitionEnterMaterial = new Material(circleWipe);
		_transitionEnterMaterial.SetColor("_BackgroundColor", targetColor);
		_transitionExitMaterial = new Material(circleWipe);
		_transitionExitMaterial.SetColor("_BackgroundColor", targetColor);
	}

	public void TransitionEnter()
	{
		Transition(_transitionEnterMaterial, DurationEnter, 1f, 0f, easeOut: false);
	}

	public void TransitionExit()
	{
		Transition(_transitionExitMaterial, DurationExit, 0f, 1f, easeOut: true);
	}

	private void Transition(Material material, float duration, float startValue, float endValue, bool easeOut)
	{
		if (_transitionEnterMaterial == null)
		{
			Debug.LogWarning("TransitionsFX not initialized yet. You're probably calling TransitionEnter/Exit from an Awake method. Please call it from a Start method instead.");
			return;
		}
		if (_transitionCoroutine != null)
		{
			StopCoroutine(_transitionCoroutine);
		}
		_transitionCoroutine = StartCoroutine(TransitionRoutine(material, duration, startValue, endValue, easeOut));
	}

	private IEnumerator TransitionRoutine(Material material, float duration, float startValue, float endValue, bool easeOut)
	{
		_blit.enabled = true;
		_step = startValue;
		_blit.CurrentMaterial = material;
		_blit.CurrentMaterial.SetFloat(_material_StepID, _step);
		float t = 0f;
		while (t <= 1f)
		{
			t += Time.unscaledDeltaTime / duration;
			if (easeOut)
			{
				_step = Mathf.Lerp(startValue, endValue, Easing.EaseOut(t));
			}
			else
			{
				_step = Mathf.Lerp(startValue, endValue, Easing.EaseIn(t));
			}
			material.SetFloat(_material_StepID, _step);
			yield return null;
		}
		_step = endValue;
		material.SetFloat(_material_StepID, _step);
		if (endValue == 0f)
		{
			_blit.enabled = false;
		}
	}

	public void CheckQualitySettings()
	{
		if (!(postProcessing == null))
		{
			int qualityLevel = QualitySettings.GetQualityLevel();
			postProcessing.enableVignette = qualityLevel > 0;
			postProcessing.enableAmbientOcclusion = qualityLevel > 1;
		}
	}
}
