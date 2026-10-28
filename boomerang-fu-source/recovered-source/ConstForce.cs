using PigeonCoopToolkit.Effects.Trails;
using UnityEngine;

public class ConstForce : MonoBehaviour
{
	public float speed;

	private void Start()
	{
	}

	private void Update()
	{
		SmokePlume[] components = GetComponents<SmokePlume>();
		for (int i = 0; i < components.Length; i++)
		{
			components[i].ConstantForce = base.transform.forward * speed;
		}
	}
}
