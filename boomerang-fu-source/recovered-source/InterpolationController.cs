using UnityEngine;

public class InterpolationController : MonoBehaviour
{
	private float[] m_lastFixedUpdateTimes;

	private int m_newTimeIndex;

	private static float m_interpolationFactor;

	public static float InterpolationFactor => m_interpolationFactor;

	public void Start()
	{
		m_lastFixedUpdateTimes = new float[2];
		m_newTimeIndex = 0;
	}

	public void FixedUpdate()
	{
		m_newTimeIndex = OldTimeIndex();
		m_lastFixedUpdateTimes[m_newTimeIndex] = Time.fixedTime;
	}

	public void Update()
	{
		float num = m_lastFixedUpdateTimes[m_newTimeIndex];
		float num2 = m_lastFixedUpdateTimes[OldTimeIndex()];
		if (num != num2)
		{
			m_interpolationFactor = (Time.time - num) / (num - num2);
		}
		else
		{
			m_interpolationFactor = 1f;
		}
	}

	private int OldTimeIndex()
	{
		if (m_newTimeIndex != 0)
		{
			return 0;
		}
		return 1;
	}
}
