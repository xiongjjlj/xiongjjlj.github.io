using UnityEngine;

[ExecuteInEditMode]
public class XRayReplacement : MonoBehaviour
{
	public Shader XRayShader;

	private void OnEnable()
	{
		GetComponent<Camera>().SetReplacementShader(XRayShader, "XRay");
	}
}
