using UnityEngine;

public class SplatDecal : Decal
{
	public Renderer rend;

	public MaterialPropertyBlock block;

	public LayerMask reparentLayers;

	public void SetColor(Color targetColor)
	{
		if (block == null)
		{
			block = new MaterialPropertyBlock();
		}
		else
		{
			block.Clear();
		}
		block.SetColor("_Color", targetColor);
		rend.SetPropertyBlock(block);
	}

	protected override void OnEnable()
	{
		Collider2D collider2D = Physics2D.OverlapPoint(base.transform.position, reparentLayers);
		if (collider2D != null)
		{
			if (TransformExt.IsUniformScale(collider2D.transform))
			{
				base.transform.SetParent(collider2D.transform, worldPositionStays: true);
			}
			else
			{
				Debug.LogWarning("Decal tried to parent with an object that has non-uniform scale!");
				base.gameObject.SetActive(value: false);
			}
		}
		base.OnEnable();
		transitionDuration *= Random.Range(0.9f, 1.2f);
	}
}
