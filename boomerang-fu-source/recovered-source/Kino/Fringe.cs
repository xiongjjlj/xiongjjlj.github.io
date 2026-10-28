using UnityEngine;

namespace Kino
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("Kino Image Effects/Fringe")]
	public class Fringe : MonoBehaviour
	{
		public enum QualityLevel
		{
			Low,
			High
		}

		[SerializeField]
		[Range(0f, 1f)]
		private float _lateralShift = 0.3f;

		[SerializeField]
		[Range(0f, 1f)]
		private float _axialStrength = 0.8f;

		[SerializeField]
		[Range(0f, 1f)]
		private float _axialShift = 0.3f;

		[SerializeField]
		private QualityLevel _axialQuality;

		[SerializeField]
		private Shader _shader;

		private Material _material;

		public float lateralShift
		{
			get
			{
				return _lateralShift;
			}
			set
			{
				_lateralShift = value;
			}
		}

		public float axialStrength
		{
			get
			{
				return _axialStrength;
			}
			set
			{
				_axialStrength = value;
			}
		}

		public float axialShift
		{
			get
			{
				return _axialShift;
			}
			set
			{
				_axialShift = value;
			}
		}

		public QualityLevel axialQuality
		{
			get
			{
				return _axialQuality;
			}
			set
			{
				_axialQuality = value;
			}
		}

		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (_material == null)
			{
				_material = new Material(_shader);
				_material.hideFlags = HideFlags.DontSave;
			}
			Camera component = GetComponent<Camera>();
			Vector4 value = new Vector4(component.aspect, 1f / component.aspect, 1f, 0f);
			_material.SetVector("_CameraAspect", value);
			_material.SetFloat("_LateralShift", _lateralShift);
			_material.SetFloat("_AxialStrength", _axialStrength);
			_material.SetFloat("_AxialShift", _axialShift);
			if (_axialStrength == 0f)
			{
				_material.DisableKeyword("AXIAL_SAMPLE_LOW");
				_material.DisableKeyword("AXIAL_SAMPLE_HIGH");
			}
			else if (_axialQuality == QualityLevel.Low)
			{
				_material.EnableKeyword("AXIAL_SAMPLE_LOW");
				_material.DisableKeyword("AXIAL_SAMPLE_HIGH");
			}
			else
			{
				_material.DisableKeyword("AXIAL_SAMPLE_LOW");
				_material.EnableKeyword("AXIAL_SAMPLE_HIGH");
			}
			Graphics.Blit(source, destination, _material, 0);
		}
	}
}
