using FMODUnity;
using UnityEngine;

public class WobblyObstacle : InteractiveObstacle
{
	public Transform root;

	public Rigidbody spring;

	private Transform springTarget;

	public bool scanForCollisions;

	[Space(12f)]
	public float drag = 2.5f;

	public float springForce = 80f;

	public float maxSpringDistance = 10f;

	public float inputForceModifier = 1f;

	[EventRef]
	public string soundEvent;

	private Vector3 localDistance;

	private Vector3 localVelocity;

	[SerializeField]
	private bool isAtRest = true;

	[Space(12f)]
	[Header("Particle FX")]
	public PFXType pfxType;

	public Vector3 pfxPositionOffset = Vector3.zero;

	public Color pfxColor;

	public float pfxForceThreshold = 45f;

	protected override void Start()
	{
		base.Start();
		if (spring.transform.childCount == 0)
		{
			Debug.Log("spring needs at least one child");
		}
		springTarget = spring.transform.GetChild(0);
		springTarget.SetParent(base.transform);
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (scanForCollisions)
		{
			Projectile component = other.GetComponent<Projectile>();
			if (component != null)
			{
				Interact((base.transform.position - other.transform.position).normalized * component.velocity.magnitude);
			}
		}
	}

	public override void Interact(Vector2 force)
	{
		base.Interact(force);
		if (force.sqrMagnitude > 1f)
		{
			isAtRest = false;
		}
		force *= inputForceModifier;
		spring.AddForce(force, ForceMode.Impulse);
		if (!string.IsNullOrEmpty(soundEvent))
		{
			Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter(soundEvent, "velocity", 1f, base.transform);
		}
		if (pfxType != PFXType.None && force.magnitude > pfxForceThreshold)
		{
			Singleton<ParticleManager>.Instance.PlayPFX(pfxType, base.transform.TransformPoint(pfxPositionOffset), pfxColor);
		}
	}

	private void FixedUpdate()
	{
		if (!isAtRest)
		{
			spring.rotation = base.transform.rotation;
			localDistance = springTarget.InverseTransformDirection(springTarget.position - spring.position);
			if (localDistance.magnitude > 0.05f)
			{
				spring.AddRelativeForce(localDistance * springForce);
			}
			localVelocity = spring.transform.InverseTransformDirection(spring.velocity);
			if (localVelocity.magnitude > 0.05f)
			{
				spring.AddRelativeForce(-localVelocity * drag);
			}
			if ((spring.position - springTarget.position).magnitude > maxSpringDistance)
			{
				spring.MovePosition(springTarget.position + Vector3.ClampMagnitude(spring.position - springTarget.position, maxSpringDistance));
				spring.AddRelativeForce(localDistance * springForce);
			}
			root.transform.rotation = Quaternion.LookRotation(root.transform.position - spring.position, root.transform.up);
			if (spring.IsSleeping())
			{
				isAtRest = true;
			}
		}
	}
}
