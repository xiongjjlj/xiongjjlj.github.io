using UnityEngine;
using UnityEngine.UI;

public class OffscreenPointer : MonoBehaviour
{
	public Player pointerTarget;

	private Vector3 targetZOffset = new Vector3(0f, 0f, -1f);

	private Vector2 targetViewportPos;

	private Vector3 targetScreenPos;

	private Vector2 pointerCanvasPos;

	private Vector3 pointerScreenPos;

	private Vector2 directionToTarget;

	private Image pointerImage;

	private RectTransform canvasRect;

	private RectTransform rt;

	public void AssignTarget(Player target)
	{
		pointerTarget = target;
		pointerImage = GetComponent<Image>();
		pointerImage.color = target.character.Color;
		canvasRect = base.transform.parent.GetComponent<RectTransform>();
		rt = base.transform.GetComponent<RectTransform>();
		pointerImage.enabled = false;
	}

	private void Update()
	{
		if (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing)
		{
			if (!(pointerTarget != null))
			{
				return;
			}
			targetViewportPos = Singleton<Camera3D>.Instance.gameCamera.WorldToViewportPoint(pointerTarget.transform.position + targetZOffset);
			if ((targetViewportPos.x > -0f && targetViewportPos.x < 1f && targetViewportPos.y > -0f && targetViewportPos.y < 1f) || pointerTarget.actorState == Actor.ActorState.Dead)
			{
				pointerImage.enabled = false;
				return;
			}
			Vector2 vector = new Vector2(Mathf.Clamp(targetViewportPos.x, 0.03f, 0.97f), Mathf.Clamp(targetViewportPos.y, 0.03f, 0.97f));
			pointerCanvasPos = new Vector2(vector.x * canvasRect.sizeDelta.x - canvasRect.sizeDelta.x * 0.5f, vector.y * canvasRect.sizeDelta.y - canvasRect.sizeDelta.y * 0.5f);
			rt.anchoredPosition = pointerCanvasPos;
			targetScreenPos = Singleton<Camera3D>.Instance.gameCamera.WorldToScreenPoint(pointerTarget.transform.position + targetZOffset);
			pointerScreenPos = Singleton<Camera3D>.Instance.gameCamera.WorldToScreenPoint(base.transform.position);
			directionToTarget = (targetScreenPos - pointerScreenPos).normalized;
			Vector2 zero = Vector2.zero;
			if (targetScreenPos.x > (float)Singleton<Camera3D>.Instance.gameCamera.pixelWidth)
			{
				zero.x = targetScreenPos.x - (float)Singleton<Camera3D>.Instance.gameCamera.pixelWidth;
			}
			else if (targetScreenPos.x < 0f)
			{
				zero.x = targetScreenPos.x;
			}
			if (targetScreenPos.y > (float)Singleton<Camera3D>.Instance.gameCamera.pixelHeight)
			{
				zero.y = targetScreenPos.y - (float)Singleton<Camera3D>.Instance.gameCamera.pixelHeight;
			}
			else if (targetScreenPos.y < 0f)
			{
				zero.y = targetScreenPos.y;
			}
			pointerImage.color = new Color(pointerImage.color.r, pointerImage.color.g, pointerImage.color.b, zero.magnitude * 0.07f);
			base.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(directionToTarget.y, directionToTarget.x) * 57.29578f);
			pointerImage.enabled = true;
		}
		else
		{
			pointerImage.enabled = false;
		}
	}
}
