using UnityEngine;

public class ScrollingUV : MonoBehaviour
{
	[SerializeField]
	private string textureName = "_MainTex";

	public Vector2 scrollSpeed;

	private Material mat;

	private Vector2 offset;

	private void Awake()
	{
		mat = GetComponent<Renderer>().material;
	}

	private void Update()
	{
		offset += Time.deltaTime * scrollSpeed;
		mat.mainTextureOffset = offset;
	}
}
