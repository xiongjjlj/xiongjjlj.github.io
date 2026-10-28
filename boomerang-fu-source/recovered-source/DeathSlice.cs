using UnityEngine;

public class DeathSlice : Debris3D
{
	[SerializeField]
	private Transform parent;

	private Color sliceColor;

	public bool isSticky;

	public bool orientUpwards;

	public bool orientRandom;

	protected override void Awake()
	{
		base.Awake();
		InitDebris();
	}

	public void InitDebris(Color sliceColor)
	{
		initPos = base.transform.localPosition;
		debrisInitRotation = debris.localRotation;
		this.sliceColor = sliceColor;
	}

	public void Slice(Vector3 sliceDirection)
	{
		base.transform.SetParent(null, worldPositionStays: true);
		base.transform.rotation = parent.transform.rotation;
		if (orientUpwards)
		{
			base.transform.rotation = Quaternion.Euler(0f, base.transform.rotation.eulerAngles.y, base.transform.rotation.eulerAngles.z);
		}
		if (orientRandom)
		{
			base.transform.Rotate(new Vector3(0f, 0f, Random.Range(-0.4f, 0.4f)));
		}
		base.gameObject.SetActive(value: true);
		ApplyForce(sliceDirection.normalized);
	}

	public override void Reset()
	{
		base.transform.rotation = parent.transform.rotation;
		base.transform.SetParent(parent, worldPositionStays: true);
		base.Reset();
	}

	protected override void OnBounce()
	{
		base.OnBounce();
		if (isSticky)
		{
			rb.velocity *= 0.3f;
		}
	}

	public override void GetSquished()
	{
		Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Splatter, base.transform.position, sliceColor);
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/death/squished_small", base.transform.position);
		base.GetSquished();
	}
}
