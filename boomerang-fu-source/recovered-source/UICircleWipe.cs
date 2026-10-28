using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
[DisallowMultipleComponent]
public class UICircleWipe : BaseMeshEffect
{
	private const string ShaderID = "FX/CircleWipe";

	private const string ShaderIDInverse = "FX/CircleWipeInverse";

	public AnimationCurve progressCurve;

	public float wipeInTime = 1f;

	public float wipeOutTime = 1f;

	public Vector3 wipeFocusOffset = Vector3.zero;

	public bool changeColor;

	public Color startColor;

	public Color endColor;

	public AnimationCurve colorCurve;

	protected float progress;

	protected Vector2 screenPosition = Vector2.zero;

	protected Vector4 shaderProperty_data = Vector4.zero;

	protected Color finalColor = Color.white;

	protected Shader shaderInstance;

	protected Image image;

	public bool inverse;

	public Shader Shader
	{
		get
		{
			if (shaderInstance == null)
			{
				if (inverse)
				{
					shaderInstance = Shader.Find("FX/CircleWipeInverse");
				}
				else
				{
					shaderInstance = Shader.Find("FX/CircleWipe");
				}
			}
			return shaderInstance;
		}
	}

	public override void ModifyMesh(Mesh mesh)
	{
	}

	public override void ModifyMesh(VertexHelper vh)
	{
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if (!(base.graphic == null))
		{
			base.graphic.material = null;
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		if (!(base.graphic == null))
		{
			if (Shader == null)
			{
				Debug.LogErrorFormat("Cannot find {0} shader", "FX/CircleWipe");
			}
			else
			{
				screenPosition = Screen.safeArea.center;
				base.graphic.material = new Material(Shader);
			}
		}
	}

	protected override void Start()
	{
		image = GetComponent<Image>();
	}

	public void SetFocusPosition(Vector2 _screenPosition)
	{
		_screenPosition.x -= (float)Singleton<Camera3D>.Instance.gameCamera.scaledPixelWidth * 0.5f;
		_screenPosition.y -= (float)Singleton<Camera3D>.Instance.gameCamera.scaledPixelHeight * 0.5f;
		screenPosition = _screenPosition;
	}

	public void SetProgress(float _progress)
	{
		progress = Mathf.Clamp(_progress, 0f, 1f);
		if (changeColor && image != null)
		{
			image.color = Color.Lerp(startColor, endColor, colorCurve.Evaluate(progress));
		}
		UpdateShader();
	}

	public void UpdateShader()
	{
		Material material = base.graphic.material;
		float x = Screen.safeArea.size.x;
		finalColor = base.graphic.color;
		shaderProperty_data.x = screenPosition.x;
		shaderProperty_data.y = screenPosition.y;
		shaderProperty_data.z = x - x * progressCurve.Evaluate(progress);
		shaderProperty_data.w = 0f;
		material.SetVector("_Data", shaderProperty_data);
		base.graphic.material = material;
	}

	public void Reset()
	{
		if (inverse)
		{
			SetProgress(0f);
		}
		else
		{
			SetProgress(1f);
		}
		base.gameObject.SetActive(value: false);
	}

	public IEnumerator TransitionIn(Transform target = null, float targetDelta = 1f)
	{
		Singleton<UIManager>.Instance.EnableScreenTransitionCanvas();
		base.gameObject.SetActive(value: true);
		Vector2 focusPosition = Screen.safeArea.center;
		if (target != null && target.gameObject.activeSelf)
		{
			focusPosition = Singleton<Camera3D>.Instance.gameCamera.WorldToScreenPoint(target.position + wipeFocusOffset);
		}
		SetFocusPosition(focusPosition);
		SetProgress(0f);
		yield return null;
		float delta = 0f;
		while (delta < targetDelta)
		{
			Singleton<UIManager>.Instance.EnableScreenTransitionCanvas();
			if (target != null && target.gameObject.activeSelf)
			{
				focusPosition = Singleton<Camera3D>.Instance.gameCamera.WorldToScreenPoint(target.position + wipeFocusOffset);
				SetFocusPosition(focusPosition);
			}
			delta += Time.unscaledDeltaTime / wipeInTime;
			SetProgress(delta);
			yield return null;
		}
		SetProgress(targetDelta);
	}

	public IEnumerator TransitionOut(bool resetFocusPosition = true, float startDelta = 1f)
	{
		Singleton<UIManager>.Instance.EnableScreenTransitionCanvas();
		base.gameObject.SetActive(value: true);
		yield return null;
		if (resetFocusPosition)
		{
			SetFocusPosition(Screen.safeArea.center);
		}
		float delta = startDelta;
		while (delta > 0f)
		{
			delta -= Time.unscaledDeltaTime / wipeOutTime;
			SetProgress(delta);
			yield return null;
		}
		SetProgress(0f);
		yield return null;
		base.gameObject.SetActive(value: false);
		Singleton<UIManager>.Instance.DisableScreenTransitionCanvas();
	}

	public IEnumerator TransitionOutInverse(Transform target = null)
	{
		base.gameObject.SetActive(value: true);
		Vector2 focusPosition = Screen.safeArea.center;
		if (target != null && target.gameObject.activeSelf)
		{
			focusPosition = Singleton<Camera3D>.Instance.uiCamera.WorldToScreenPoint(target.position + wipeFocusOffset);
		}
		SetFocusPosition(focusPosition);
		SetProgress(1f);
		float delta = 1f;
		while (delta > 0f)
		{
			delta -= Time.unscaledDeltaTime / wipeOutTime;
			SetProgress(delta);
			yield return null;
		}
		SetProgress(0f);
		yield return null;
	}
}
