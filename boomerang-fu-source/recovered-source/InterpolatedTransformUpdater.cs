using UnityEngine;

public class InterpolatedTransformUpdater : MonoBehaviour
{
	private InterpolatedTransform m_interpolatedTransform;

	private void Awake()
	{
		m_interpolatedTransform = GetComponent<InterpolatedTransform>();
	}

	private void FixedUpdate()
	{
		m_interpolatedTransform.LateFixedUpdate();
	}
}
