using UnityEngine;

namespace PolygonArsenal
{
	public class PolygonSoundSpawn : MonoBehaviour
	{
		public GameObject prefabSound;

		public bool destroyWhenDone = true;

		public bool soundPrefabIsChild;

		[Range(0.01f, 10f)]
		public float pitchRandomMultiplier = 1f;

		private void Start()
		{
			GameObject gameObject = Object.Instantiate(prefabSound, base.transform.position, Quaternion.identity);
			AudioSource component = gameObject.GetComponent<AudioSource>();
			if (soundPrefabIsChild)
			{
				gameObject.transform.SetParent(base.transform);
			}
			if (pitchRandomMultiplier != 1f)
			{
				if ((double)Random.value < 0.5)
				{
					component.pitch *= Random.Range(1f / pitchRandomMultiplier, 1f);
				}
				else
				{
					component.pitch *= Random.Range(1f, pitchRandomMultiplier);
				}
			}
			if (destroyWhenDone)
			{
				float t = component.clip.length / component.pitch;
				Object.Destroy(gameObject, t);
			}
		}
	}
}
