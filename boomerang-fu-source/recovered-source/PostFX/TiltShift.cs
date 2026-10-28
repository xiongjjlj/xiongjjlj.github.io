using UnityEngine;

namespace PostFX
{
	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
	[AddComponentMenu("Image Effects/Tilt Shift")]
	public class TiltShift : MonoBehaviour
	{
		public bool Preview;

		[Range(-1f, 1f)]
		public float Offset;

		[Range(0f, 20f)]
		public float Area = 1f;

		[Range(0f, 20f)]
		public float Spread = 1f;

		[Range(8f, 64f)]
		public int Samples = 32;

		[Range(0f, 2f)]
		public float Radius = 1f;

		public bool UseDistortion = true;

		[Range(0f, 20f)]
		public float CubicDistortion = 5f;

		[Range(0.01f, 2f)]
		public float DistortionScale = 1f;

		public Shader Shader;

		protected Material m_Material;

		protected Vector4 m_GoldenRot;

		public Material Material
		{
			get
			{
				if (m_Material == null)
				{
					m_Material = new Material(Shader);
					m_Material.hideFlags = HideFlags.HideAndDontSave;
				}
				return m_Material;
			}
		}

		private void Start()
		{
			if (!SystemInfo.supportsImageEffects)
			{
				Debug.LogWarning("Image effects aren't supported on this device");
				base.enabled = false;
				return;
			}
			if (!Shader || !Shader.isSupported)
			{
				Debug.LogWarning("The shader is null or unsupported on this device");
				base.enabled = false;
			}
			float num = Mathf.Cos(2.3999631f);
			float num2 = Mathf.Sin(2.3999631f);
			m_GoldenRot.Set(num, num2, 0f - num2, num);
		}

		private void OnDisable()
		{
			if ((bool)m_Material)
			{
				Object.DestroyImmediate(m_Material);
			}
		}

		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (UseDistortion)
			{
				Material.EnableKeyword("USE_DISTORTION");
			}
			else
			{
				Material.DisableKeyword("USE_DISTORTION");
			}
			Material.SetVector("_GoldenRot", m_GoldenRot);
			Material.SetVector("_Gradient", new Vector3(Offset, Area, Spread));
			Material.SetVector("_Distortion", new Vector2(CubicDistortion, DistortionScale));
			Material.SetVector("_Params", new Vector4(Samples, Radius, 1f / (float)source.width, 1f / (float)source.height));
			Graphics.Blit(source, destination, Material, (!Preview) ? 1 : 0);
		}
	}
}
