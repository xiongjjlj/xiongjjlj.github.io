using UnityEngine;

public class CollisionBehaviour : MonoBehaviour
{
	public virtual void OnTriggerEnter2D(Collider2D hit)
	{
	}

	public virtual void OnTriggerExit2D(Collider2D hit)
	{
	}

	public virtual void OnTriggerStay2D(Collider2D hit)
	{
	}

	public virtual void CollisionEnter2D(Vector2 normal, Collider2D collider)
	{
	}

	public virtual void CollisionExit2D(Vector2 normal, Collider2D collider)
	{
	}

	public virtual void CollisionStay2D(Vector2 point, Collider2D collider)
	{
	}
}
