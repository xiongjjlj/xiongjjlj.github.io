using UnityEngine;

namespace BoomerangFu.Common.PlatformTools
{
	public class PlatformObjectHider : MonoBehaviour
	{
		[SerializeField]
		[Tooltip("Show object only on selected platforms")]
		private bool showOnlyOnPlatforms;

		[SerializeField]
		private RuntimePlatform[] platforms;

		private void Awake()
		{
			ValidateObject();
		}

		private void ValidateObject()
		{
			bool active = !showOnlyOnPlatforms;
			for (int i = 0; i < platforms.Length; i++)
			{
				if (Application.platform == platforms[i])
				{
					active = showOnlyOnPlatforms;
					break;
				}
			}
			base.gameObject.SetActive(active);
		}
	}
}
