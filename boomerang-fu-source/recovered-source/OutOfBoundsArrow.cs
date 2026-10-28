using System.Collections;
using UnityEngine;

public class OutOfBoundsArrow : MonoBehaviour
{
	private float turningSpeed = 10f;

	private Quaternion lastRotation;

	private bool isEnabled;

	public SpriteRenderer sprite;

	public float alpha;

	private void Start()
	{
		base.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		if (!Singleton<LevelManager>.Instance.IsInBounds(base.transform.position))
		{
			Vector2 vector = VectorExt.ClosestPointOnBoxColliderEdge(base.transform.position, Singleton<LevelManager>.Instance.levelBoundary);
			lastRotation = VectorExt.RotationZFromVectorDirection(vector - (Vector2)base.transform.position);
		}
		base.transform.rotation = lastRotation;
		sprite.color = ColorExt.SetAlpha(sprite.color, alpha);
	}

	public void Enable()
	{
		base.gameObject.SetActive(value: true);
		isEnabled = true;
		alpha = 0f;
		Update();
	}

	public void Disable()
	{
		if (base.gameObject.activeSelf)
		{
			isEnabled = false;
			StartCoroutine(FadeOut());
		}
	}

	private IEnumerator FadeOut()
	{
		yield return new WaitForSecondsRealtime(0.25f);
		if (!isEnabled)
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
