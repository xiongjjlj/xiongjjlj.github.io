using UnityEngine;

public class Grenade : MonoBehaviour
{
	public Player discOwner;

	public Disc parentDisc;

	private Renderer grenadeRenderer;

	[HideInInspector]
	public float explosiveTimer;

	private bool blinkOn;

	private float blinkTimer;

	private float blinkRateInit = 0.25f;

	private float blinkRate;

	private int blinkCounter;

	public ParticleSystem blinkPFX;

	private ParticleSystem.MainModule blinkPFXMain;

	public Color blinkColorFire;

	public Color blinkColorSlime;

	private MaterialPropertyBlock blinkPropertyBlock;

	private int blinkShaderPropertyID;

	private void Start()
	{
		blinkPFXMain = blinkPFX.main;
		blinkShaderPropertyID = Shader.PropertyToID("_Color");
	}

	private void OnEnable()
	{
		blinkRate = blinkRateInit;
		blinkCounter = 0;
		explosiveTimer = 1.33f;
	}

	private void Update()
	{
		RunExplosiveTimer();
	}

	private void RunExplosiveTimer()
	{
		if (explosiveTimer > 0f)
		{
			explosiveTimer -= Time.deltaTime;
			blinkRate = Mathf.Lerp(0.03f, blinkRateInit, Easing.EaseIn(explosiveTimer / 1.5f));
		}
		else
		{
			explosiveTimer = 0f;
			Explode();
		}
		if (blinkTimer < blinkRate)
		{
			blinkTimer += Time.deltaTime;
			return;
		}
		Blink();
		blinkTimer = 0f;
	}

	private void Explode()
	{
		float respawnTime = 1.75f;
		if (parentDisc != null)
		{
			Explosion explosion;
			if (parentDisc.discPowerup.HasPowerup(PowerupType.SlimeDisc))
			{
				explosion = Singleton<GameManager>.Instance.explosionFreezePrefab.Spawn(base.transform.position + new Vector3(0f, 0f, -1f), Quaternion.identity);
				if (parentDisc.isMiniDisc)
				{
					Singleton<ParticleManager>.Instance.PlayPFX(PFXType.ExplosionFreezeMini, base.transform.position);
				}
				else
				{
					Singleton<ParticleManager>.Instance.PlayPFX(PFXType.ExplosionFreeze, base.transform.position);
				}
			}
			else if (parentDisc.discPowerup.HasPowerup(PowerupType.FireDisc))
			{
				explosion = Singleton<GameManager>.Instance.explosionFirePrefab.Spawn(base.transform.position + new Vector3(0f, 0f, -1f), Quaternion.identity);
				if (parentDisc.isMiniDisc)
				{
					Singleton<ParticleManager>.Instance.PlayPFX(PFXType.ExplosionMini, base.transform.position);
				}
				else
				{
					Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Explosion, base.transform.position);
					Singleton<ParticleManager>.Instance.PlayPFX(PFXType.ExplosionFire, base.transform.position);
				}
			}
			else
			{
				explosion = Singleton<GameManager>.Instance.explosionPrefab.Spawn(base.transform.position + new Vector3(0f, 0f, -1f), Quaternion.identity);
				if (parentDisc.isMiniDisc)
				{
					Singleton<ParticleManager>.Instance.PlayPFX(PFXType.ExplosionMini, base.transform.position);
				}
				else
				{
					Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Explosion, base.transform.position);
					Singleton<ParticleManager>.Instance.PlayPFX(PFXType.ExplosionDebris, base.transform.position);
				}
			}
			Singleton<GameManager>.Instance.activeExplosives.Add(explosion.gameObject);
			explosion.owner = discOwner;
			explosion.Init(parentDisc.isMiniDisc);
		}
		if (parentDisc != null)
		{
			parentDisc.Explode(respawnTime);
		}
		base.gameObject.SetActive(value: false);
	}

	private void Blink()
	{
		if (blinkPropertyBlock == null)
		{
			blinkPropertyBlock = new MaterialPropertyBlock();
		}
		else
		{
			blinkPropertyBlock.Clear();
		}
		if (blinkOn)
		{
			blinkOn = false;
			blinkPropertyBlock.SetColor(blinkShaderPropertyID, parentDisc.projectileColor);
		}
		else
		{
			if (parentDisc.discPowerup.HasPowerup(PowerupType.FireDisc))
			{
				blinkPFXMain.startColor = blinkColorFire;
			}
			else if (parentDisc.discPowerup.HasPowerup(PowerupType.SlimeDisc))
			{
				blinkPFXMain.startColor = blinkColorSlime;
			}
			blinkOn = true;
			blinkPFX.Emit(1);
			blinkPropertyBlock.SetColor(blinkShaderPropertyID, Color.white);
			Singleton<AudioManager>.Instance.PlayOneShotAndSetParameter("event:/powerups/grenade_blink", "counter", blinkCounter, base.transform);
			blinkCounter++;
		}
		parentDisc.discRenderer.SetPropertyBlock(blinkPropertyBlock);
	}
}
