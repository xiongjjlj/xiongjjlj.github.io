using UnityEngine;

[RequireComponent(typeof(InterpolatedTransformUpdater))]
public class InterpolatedTransform : MonoBehaviour
{
	private struct TransformData
	{
		public Vector3 position;

		public Quaternion rotation;

		public Vector3 scale;

		public TransformData(Vector3 position, Quaternion rotation, Vector3 scale)
		{
			this.position = position;
			this.rotation = rotation;
			this.scale = scale;
		}
	}

	private TransformData[] m_lastTransforms;

	private int m_newTransformIndex;

	private void OnEnable()
	{
		ForgetPreviousTransforms();
	}

	public void ForgetPreviousTransforms()
	{
		m_lastTransforms = new TransformData[2];
		TransformData transformData = new TransformData(base.transform.localPosition, base.transform.localRotation, base.transform.localScale);
		m_lastTransforms[0] = transformData;
		m_lastTransforms[1] = transformData;
		m_newTransformIndex = 0;
	}

	public void ReparentPreviousTransforms(Transform oldParent, Transform newParent)
	{
		for (int i = 0; i < m_lastTransforms.Length; i++)
		{
			if (oldParent == null && newParent == null)
			{
				ForgetPreviousTransforms();
			}
			else if (oldParent == null)
			{
				m_lastTransforms[i].position = newParent.InverseTransformPoint(m_lastTransforms[i].position);
				m_lastTransforms[i].rotation = Quaternion.Inverse(newParent.rotation) * m_lastTransforms[i].rotation;
				m_lastTransforms[i].scale = base.transform.localScale;
			}
			else if (newParent == null)
			{
				m_lastTransforms[i].position = oldParent.TransformPoint(m_lastTransforms[i].position);
				m_lastTransforms[i].rotation = base.transform.rotation;
				m_lastTransforms[i].scale = base.transform.localScale;
			}
			else
			{
				m_lastTransforms[i].position = newParent.InverseTransformPoint(oldParent.TransformPoint(m_lastTransforms[i].position));
				m_lastTransforms[i].rotation = base.transform.localRotation;
				m_lastTransforms[i].scale = base.transform.localScale;
			}
		}
	}

	private void FixedUpdate()
	{
		TransformData transformData = m_lastTransforms[m_newTransformIndex];
		base.transform.localPosition = transformData.position;
		base.transform.localRotation = transformData.rotation;
		base.transform.localScale = transformData.scale;
	}

	public void LateFixedUpdate()
	{
		m_newTransformIndex = OldTransformIndex();
		m_lastTransforms[m_newTransformIndex] = new TransformData(base.transform.localPosition, base.transform.localRotation, base.transform.localScale);
	}

	private void Update()
	{
		TransformData transformData = m_lastTransforms[m_newTransformIndex];
		TransformData transformData2 = m_lastTransforms[OldTransformIndex()];
		base.transform.localPosition = Vector3.Lerp(transformData2.position, transformData.position, InterpolationController.InterpolationFactor);
		base.transform.localRotation = Quaternion.Slerp(transformData2.rotation, transformData.rotation, InterpolationController.InterpolationFactor);
		base.transform.localScale = Vector3.Lerp(transformData2.scale, transformData.scale, InterpolationController.InterpolationFactor);
	}

	private int OldTransformIndex()
	{
		if (m_newTransformIndex != 0)
		{
			return 0;
		}
		return 1;
	}
}
