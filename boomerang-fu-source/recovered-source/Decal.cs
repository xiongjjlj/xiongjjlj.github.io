using UnityEngine;

public class Decal : MonoBehaviour
{
	private enum SplatState
	{
		Stopped,
		TransitionIn,
		Playing,
		TransitionOut
	}

	public Transform decal;

	private Vector3 originalScale;

	public float minSize;

	public float maxSize;

	public AnimationCurve scaleInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	public AnimationCurve scaleOutCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

	public float scaleInDuration = 1f;

	public float lifetime = 20f;

	public float scaleOutDuration = 0.5f;

	private float timeElapsed;

	protected float transitionDuration;

	[SerializeField]
	private SplatState splatState;

	protected virtual void OnEnable()
	{
		float num = Random.Range(minSize, maxSize);
		originalScale = new Vector3(num, num, 3f);
		timeElapsed = 0f;
		splatState = SplatState.TransitionIn;
		base.transform.Rotate(0f, 0f, Random.Range(-90f, 90f));
		transitionDuration = scaleInDuration;
		decal.transform.localScale = new Vector3(originalScale.x * scaleInCurve.Evaluate(0f), originalScale.y * scaleInCurve.Evaluate(0f), originalScale.z);
	}

	private void Update()
	{
		switch (splatState)
		{
		case SplatState.Stopped:
			break;
		case SplatState.TransitionIn:
			if (timeElapsed < transitionDuration)
			{
				timeElapsed += Time.deltaTime;
				decal.transform.localScale = new Vector3(originalScale.x * scaleInCurve.Evaluate(timeElapsed / transitionDuration), originalScale.y * scaleInCurve.Evaluate(timeElapsed / transitionDuration), originalScale.z);
				break;
			}
			timeElapsed = 0f;
			decal.transform.localScale = originalScale * scaleInCurve.Evaluate(1f);
			if (lifetime > 0f)
			{
				splatState = SplatState.Playing;
			}
			else
			{
				splatState = SplatState.Stopped;
			}
			break;
		case SplatState.Playing:
			if (timeElapsed < lifetime)
			{
				timeElapsed += Time.deltaTime;
				break;
			}
			timeElapsed = 0f;
			transitionDuration = scaleOutDuration;
			splatState = SplatState.TransitionOut;
			break;
		case SplatState.TransitionOut:
			if (timeElapsed < transitionDuration)
			{
				timeElapsed += Time.deltaTime;
				decal.transform.localScale = new Vector3(originalScale.x * scaleOutCurve.Evaluate(timeElapsed / transitionDuration), originalScale.y * scaleOutCurve.Evaluate(timeElapsed / transitionDuration), originalScale.z);
				break;
			}
			timeElapsed = 0f;
			decal.transform.localScale = originalScale * scaleOutCurve.Evaluate(1f);
			splatState = SplatState.Stopped;
			Singleton<DecalManager>.Instance.RecycleDecal(base.gameObject);
			break;
		}
	}

	public void Init()
	{
		decal.transform.localScale = originalScale;
	}

	public void SetLocalScale(Vector3 size)
	{
		originalScale = size;
	}

	public void SetRotation(Quaternion rotation)
	{
		base.transform.rotation = rotation;
		base.transform.Rotate(0f, 0f, Random.Range(-30f, 30f));
	}

	public void SetScaleInDuration(float duration)
	{
		transitionDuration = duration * Random.Range(0.9f, 1.2f);
	}
}
