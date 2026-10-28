using UnityEngine;

public class LevelRandomChunk : MonoBehaviour
{
	public enum ChunkRotation
	{
		None,
		BottomLeft,
		TopLeft,
		TopRight,
		BottomRight
	}

	public GameObject placeholder;

	public ChunkRotation chunkRotation;

	private void Start()
	{
		placeholder.SetActive(value: false);
		Object.Instantiate(Singleton<LevelManager>.Instance.levelChunks[Random.Range(0, Singleton<LevelManager>.Instance.levelChunks.Length)], base.transform.position, base.transform.rotation, base.transform);
		float z = 0f;
		switch (chunkRotation)
		{
		case ChunkRotation.None:
			z = -90f * (float)Random.Range(0, 4);
			break;
		case ChunkRotation.BottomLeft:
			z = 0f;
			break;
		case ChunkRotation.TopLeft:
			z = -90f;
			break;
		case ChunkRotation.TopRight:
			z = -180f;
			break;
		case ChunkRotation.BottomRight:
			z = -270f;
			break;
		}
		base.transform.Rotate(new Vector3(0f, 0f, z));
	}
}
