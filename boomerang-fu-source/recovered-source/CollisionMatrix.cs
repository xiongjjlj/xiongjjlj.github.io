using UnityEngine;

public class CollisionMatrix : MonoBehaviour
{
	public static LayerMask GetCollisionMask(int inputLayer)
	{
		int num = 0;
		for (int i = 0; i < 32; i++)
		{
			num |= (int)(((!Physics2D.GetIgnoreLayerCollision(inputLayer, i)) ? 1u : 0u) << i);
		}
		return num;
	}
}
