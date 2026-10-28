using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Switch : MonoBehaviour
{
	public Animator anim;

	public Renderer flashRenderer;

	public MovingObstacle[] movingObstacles;

	public MovingDoor[] movingDoors;

	public MovingFloorReactive[] movingFloors;

	private List<ExpressiveObstacle> expressiveObstacles;

	public float cooldownTimer;

	public float tutorialPromptTime = 8f;

	public bool oneShotOnly;

	public bool willCompleteTutorial;

	public bool isActive = true;

	public float cooldownDur = 0.5f;

	private Color initColor;

	private Color activatorColor;

	private float flashDuration = 0.75f;

	private IEnumerator flashRoutine;

	private void Start()
	{
		if (anim == null)
		{
			anim = GetComponent<Animator>();
		}
		anim.SetBool("Activated", value: false);
		if (flashRenderer == null)
		{
			flashRenderer = base.transform.Find("LeverSwitch/Lever").GetComponent<Renderer>();
		}
		initColor = flashRenderer.material.GetColor("_IllumColor");
		expressiveObstacles = new List<ExpressiveObstacle>(movingObstacles.Length);
		for (int i = 0; i < movingObstacles.Length; i++)
		{
			ExpressiveObstacle component = movingObstacles[i].GetComponent<ExpressiveObstacle>();
			if (component != null)
			{
				expressiveObstacles.Add(component);
			}
		}
		for (int j = 0; j < movingFloors.Length; j++)
		{
			ExpressiveObstacle component2 = movingFloors[j].GetComponent<ExpressiveObstacle>();
			if (component2 != null)
			{
				expressiveObstacles.Add(component2);
			}
		}
	}

	private void Update()
	{
		if (cooldownTimer > 0f)
		{
			cooldownTimer -= Time.deltaTime;
			if (cooldownTimer <= 0f)
			{
				anim.SetBool("Activated", value: false);
				if (flashRoutine != null)
				{
					StopCoroutine(flashRoutine);
				}
				flashRoutine = FlashColor();
				StartCoroutine(flashRoutine);
			}
		}
		if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing || !willCompleteTutorial || !Singleton<LevelManager>.Instance.level.isTutorialLevel || !isActive)
		{
			return;
		}
		tutorialPromptTime -= Time.deltaTime;
		if (tutorialPromptTime <= 0f)
		{
			activatorColor = Color.white * 0.9f;
			if (flashRoutine != null)
			{
				StopCoroutine(flashRoutine);
			}
			flashRoutine = FlashColor();
			StartCoroutine(flashRoutine);
			tutorialPromptTime = 1f;
			Singleton<ParticleManager>.Instance.PlayPFX(PFXType.DiscSpawn, base.transform.TransformPoint(new Vector2(0.4f, 1.65f)));
		}
	}

	private void Activate(Actor activator = null)
	{
		if (oneShotOnly)
		{
			isActive = false;
			Singleton<Camera3D>.Instance.RemoveCameraTarget(base.transform, 2f);
		}
		if (activator != null)
		{
			activatorColor = activator.character.Color;
			if (flashRoutine != null)
			{
				StopCoroutine(flashRoutine);
			}
			flashRenderer.material.SetColor("_IllumColor", activatorColor);
		}
		anim.SetBool("Activated", value: true);
		for (int i = 0; i < movingObstacles.Length; i++)
		{
			movingObstacles[i].Toggle(activator);
		}
		for (int j = 0; j < movingFloors.Length; j++)
		{
			movingFloors[j].Toggle(activator);
		}
		for (int k = 0; k < movingDoors.Length; k++)
		{
			movingDoors[k].ToggleTargetPosition(activator);
		}
		for (int l = 0; l < expressiveObstacles.Count; l++)
		{
			expressiveObstacles[l].SetFacialExpression(FacialExpression.happy, 0.5f);
		}
		Player player = activator as Player;
		if (player != null)
		{
			player.metadata.FlippedSwitches++;
			if (willCompleteTutorial)
			{
				player.hasCompletedTutorial = true;
				Singleton<LevelManager>.Instance.OnTutorialSwitchFlipped();
			}
		}
		cooldownTimer = cooldownDur;
		Singleton<AudioManager>.Instance.PlayOneShotAt("event:/environment/switch", base.transform.position);
		Singleton<Camera3D>.Instance.Shake(0.2f, 0.1f);
	}

	public void ActivateAfterDelay(float delay)
	{
		StartCoroutine(ActivateAfterDelayRoutine(delay));
	}

	private IEnumerator ActivateAfterDelayRoutine(float delay)
	{
		yield return new WaitForSeconds(delay);
		if (isActive)
		{
			Activate();
		}
	}

	private void OnTriggerEnter2D(Collider2D col)
	{
		if (cooldownTimer > 0f || !isActive || (bool)Physics2D.Raycast(base.transform.position, (col.transform.position - base.transform.position).normalized, (col.transform.position - base.transform.position).magnitude, 1 << LayerMask.NameToLayer("Obstacle")))
		{
			return;
		}
		if (col.gameObject.layer == LayerMask.NameToLayer("Projectile"))
		{
			Projectile component = col.GetComponent<Projectile>();
			if (Vector2.Dot(component.velocityStored.normalized, base.transform.up) < 0.1f || (component.movement == Projectile.Movement.Throwing && component.owner != null && (component.transform.position - component.owner.transform.position).magnitude < Projectile.projectileColliderRadius * 3f))
			{
				Activate(component.owner);
				Color projectileColor = component.projectileColor;
				Singleton<GameManager>.Instance.newVFXRico = Singleton<GameManager>.Instance.VFXRico.Spawn(base.transform.position, Quaternion.LookRotation(Vector3.forward, (col.transform.position - base.transform.position).normalized));
				Singleton<GameManager>.Instance.newVFXRico.Emit(projectileColor, 100f);
			}
		}
		else
		{
			Actor component2 = col.transform.parent.GetComponent<Actor>();
			if (component2 != null)
			{
				Activate(component2);
				Color projectileColor = component2.character.Color;
				Singleton<GameManager>.Instance.newVFXRico = Singleton<GameManager>.Instance.VFXRico.Spawn(base.transform.position, Quaternion.LookRotation(Vector3.forward, (col.transform.position - base.transform.position).normalized));
				Singleton<GameManager>.Instance.newVFXRico.Emit(projectileColor, 100f);
			}
		}
	}

	private IEnumerator FlashColor()
	{
		if (oneShotOnly)
		{
			initColor = flashRenderer.material.GetColor("_IllumColor");
		}
		flashRenderer.material.SetColor("_IllumColor", activatorColor);
		float t = 0f;
		while (t < flashDuration)
		{
			t += Time.deltaTime;
			flashRenderer.material.SetColor("_IllumColor", Color.Lerp(activatorColor, initColor, Easing.EaseOut(t / flashDuration)));
			yield return null;
		}
		flashRenderer.material.SetColor("_IllumColor", initColor);
	}
}
