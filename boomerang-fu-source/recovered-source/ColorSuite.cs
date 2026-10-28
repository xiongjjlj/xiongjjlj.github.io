using UnityEngine;

[ExecuteInEditMode]
[ImageEffectTransformsToLDR]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Color Adjustments/Color Suite")]
public class ColorSuite : MonoBehaviour
{
	public enum DitherMode
	{
		Off,
		Ordered,
		Triangular
	}

	[SerializeField]
	private float _colorTemp;

	[SerializeField]
	private float _colorTint;

	[SerializeField]
	private bool _toneMapping;

	[SerializeField]
	private float _exposure = 1f;

	[SerializeField]
	private float _saturation = 1f;

	[SerializeField]
	private AnimationCurve _rCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private AnimationCurve _gCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private AnimationCurve _bCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private AnimationCurve _cCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private DitherMode _ditherMode;

	[SerializeField]
	private Shader shader;

	private Material _material;

	private Texture2D _lutTexture;

	public float colorTemp
	{
		get
		{
			return _colorTemp;
		}
		set
		{
			_colorTemp = value;
		}
	}

	public float colorTint
	{
		get
		{
			return _colorTint;
		}
		set
		{
			_colorTint = value;
		}
	}

	public bool toneMapping
	{
		get
		{
			return _toneMapping;
		}
		set
		{
			_toneMapping = value;
		}
	}

	public float exposure
	{
		get
		{
			return _exposure;
		}
		set
		{
			_exposure = value;
		}
	}

	public float saturation
	{
		get
		{
			return _saturation;
		}
		set
		{
			_saturation = value;
		}
	}

	public AnimationCurve redCurve
	{
		get
		{
			return _rCurve;
		}
		set
		{
			_rCurve = value;
			UpdateLUT();
		}
	}

	public AnimationCurve greenCurve
	{
		get
		{
			return _gCurve;
		}
		set
		{
			_gCurve = value;
			UpdateLUT();
		}
	}

	public AnimationCurve blueCurve
	{
		get
		{
			return _bCurve;
		}
		set
		{
			_bCurve = value;
			UpdateLUT();
		}
	}

	public AnimationCurve rgbCurve
	{
		get
		{
			return _cCurve;
		}
		set
		{
			_cCurve = value;
			UpdateLUT();
		}
	}

	public DitherMode ditherMode
	{
		get
		{
			return _ditherMode;
		}
		set
		{
			_ditherMode = value;
		}
	}

	private static Color EncodeRGBM(float r, float g, float b)
	{
		float num = Mathf.Max(Mathf.Max(r, g), Mathf.Max(b, 1E-06f));
		num = Mathf.Ceil(num * 255f) / 255f;
		return new Color(r / num, g / num, b / num, num);
	}

	private static float StandardIlluminantY(float x)
	{
		return 2.87f * x - 3f * x * x - 0.27509508f;
	}

	private static Vector3 CIExyToLMS(float x, float y)
	{
		float num = 1f;
		float num2 = num * x / y;
		float num3 = num * (1f - x - y) / y;
		float x2 = 0.7328f * num2 + 0.4296f * num - 0.1624f * num3;
		float y2 = -0.7036f * num2 + 1.6975f * num + 0.0061f * num3;
		float z = 0.003f * num2 + 0.0136f * num + 0.9834f * num3;
		return new Vector3(x2, y2, z);
	}

	private void Setup()
	{
		if (_material == null)
		{
			_material = new Material(shader);
			_material.hideFlags = HideFlags.DontSave;
		}
		if (_lutTexture == null)
		{
			_lutTexture = new Texture2D(512, 1, TextureFormat.ARGB32, mipChain: false, linear: true);
			_lutTexture.hideFlags = HideFlags.DontSave;
			_lutTexture.wrapMode = TextureWrapMode.Clamp;
			UpdateLUT();
		}
	}

	private void UpdateLUT()
	{
		for (int i = 0; i < _lutTexture.width; i++)
		{
			float time = 1f / (float)(_lutTexture.width - 1) * (float)i;
			float r = _cCurve.Evaluate(_rCurve.Evaluate(time));
			float g = _cCurve.Evaluate(_gCurve.Evaluate(time));
			float b = _cCurve.Evaluate(_bCurve.Evaluate(time));
			_lutTexture.SetPixel(i, 0, EncodeRGBM(r, g, b));
		}
		_lutTexture.Apply();
	}

	private Vector3 CalculateColorBalance()
	{
		float x = 0.31271f - _colorTemp * ((_colorTemp < 0f) ? 0.1f : 0.05f);
		float y = StandardIlluminantY(x) + _colorTint * 0.05f;
		Vector3 vector = new Vector3(0.949237f, 1.03542f, 1.08728f);
		Vector3 vector2 = CIExyToLMS(x, y);
		return new Vector3(vector.x / vector2.x, vector.y / vector2.y, vector.z / vector2.z);
	}

	private void Start()
	{
		Setup();
	}

	private void OnValidate()
	{
		Setup();
		UpdateLUT();
	}

	private void Reset()
	{
		Setup();
		UpdateLUT();
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		bool flag = QualitySettings.activeColorSpace == ColorSpace.Linear;
		Setup();
		if (flag)
		{
			_material.EnableKeyword("COLORSPACE_LINEAR");
		}
		else
		{
			_material.DisableKeyword("COLORSPACE_LINEAR");
		}
		if (_colorTemp != 0f || _colorTint != 0f)
		{
			_material.EnableKeyword("BALANCING_ON");
			_material.SetVector("_Balance", CalculateColorBalance());
		}
		else
		{
			_material.DisableKeyword("BALANCING_ON");
		}
		if (_toneMapping & flag)
		{
			_material.EnableKeyword("TONEMAPPING_ON");
			_material.SetFloat("_Exposure", _exposure);
		}
		else
		{
			_material.DisableKeyword("TONEMAPPING_ON");
		}
		_material.SetTexture("_Curves", _lutTexture);
		_material.SetFloat("_Saturation", _saturation);
		if (_ditherMode == DitherMode.Ordered)
		{
			_material.EnableKeyword("DITHER_ORDERED");
			_material.DisableKeyword("DITHER_TRIANGULAR");
		}
		else if (_ditherMode == DitherMode.Triangular)
		{
			_material.DisableKeyword("DITHER_ORDERED");
			_material.EnableKeyword("DITHER_TRIANGULAR");
		}
		else
		{
			_material.DisableKeyword("DITHER_ORDERED");
			_material.DisableKeyword("DITHER_TRIANGULAR");
		}
		Graphics.Blit(source, destination, _material);
	}
}
