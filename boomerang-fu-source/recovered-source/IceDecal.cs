using UnityEngine;

public class IceDecal : Decal
{
	public Renderer rend;

	public MaterialPropertyBlock block;

	protected override void OnEnable()
	{
		base.OnEnable();
		if (block == null)
		{
			block = new MaterialPropertyBlock();
		}
		else
		{
			block.Clear();
		}
		rend.SetPropertyBlock(block);
	}
}
