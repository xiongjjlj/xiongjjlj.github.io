using UnityEngine;

public class UICircleWipeTester : MonoBehaviour
{
	private UICircleWipe wipe;

	public Transform target;

	public Vector2 targetPos;

	private void Start()
	{
		wipe = GetComponent<UICircleWipe>();
	}

	private void Update()
	{
		if (target != null)
		{
			if (target.gameObject.activeSelf)
			{
				Vector2 focusPosition = Singleton<Camera3D>.Instance.gameCamera.WorldToScreenPoint(target.position + wipe.wipeFocusOffset);
				wipe.SetFocusPosition(focusPosition);
				wipe.SetProgress(0.95f);
			}
		}
		else
		{
			wipe.SetFocusPosition(targetPos);
			wipe.SetProgress(0.95f);
		}
	}
}
