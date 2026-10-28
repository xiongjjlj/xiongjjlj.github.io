using UnityEngine;

[ExecuteInEditMode]
public class TextureTiling : MonoBehaviour
{
	public Texture texture;

	public float textureToMeshZ = 2f;

	private Vector3 prevScale = Vector3.one;

	private float prevTextureToMeshZ = -1f;

	private void Start()
	{
		prevScale = base.gameObject.transform.lossyScale;
		prevTextureToMeshZ = textureToMeshZ;
		UpdateTiling();
	}

	private void Update()
	{
		if (base.gameObject.transform.lossyScale != prevScale || !Mathf.Approximately(textureToMeshZ, prevTextureToMeshZ))
		{
			UpdateTiling();
		}
		prevScale = base.gameObject.transform.lossyScale;
		prevTextureToMeshZ = textureToMeshZ;
	}

	[ContextMenu("UpdateTiling")]
	private void UpdateTiling()
	{
		float num = 10f;
		float num2 = 10f;
		float num3 = (float)texture.width / (float)texture.height * textureToMeshZ;
		base.gameObject.GetComponent<Renderer>().material.mainTextureScale = new Vector2(num * base.gameObject.transform.lossyScale.x / num3, num2 * base.gameObject.transform.lossyScale.z / textureToMeshZ);
	}
}
