using System.Collections;
using UnityEngine;

public class CharacterPlayer : Character
{
	private Player player;

	private Decoy decoy;

	private int team;

	private static Color dangerColor = new Color(1f, 0.1f, 0.1f);

	public GameObject[] bodyNormal;

	public GameObject[] bodySliced;

	public DeathSlice deathSlice;

	private Vector3 deathSliceInitPos;

	[Header("Player Materials")]
	public Material _bodyMatTeamOne;

	public Material _bodyMatTeamTwo;

	[SerializeField]
	protected Color _discColor;

	private MaterialPropertyBlock dangerPropertyBlock;

	private int dangerShaderPropertyID;

	public GameObject uiShadowPrefab;

	[SerializeField]
	private GameObject uiShadow;

	public override bool IsBurnt
	{
		get
		{
			return base.IsBurnt;
		}
		set
		{
			if (base.IsBurnt == value)
			{
				return;
			}
			if (value)
			{
				for (int i = 0; i < meleeDiscRenderers.Length; i++)
				{
					Singleton<ColorManager>.Instance.SetDiscColor(meleeDiscRenderers[i], Singleton<ColorManager>.Instance.burntDiscColor);
				}
			}
			else
			{
				for (int j = 0; j < meleeDiscRenderers.Length; j++)
				{
					Singleton<ColorManager>.Instance.SetDiscColor(meleeDiscRenderers[j], DiscColor);
				}
			}
			base.IsBurnt = value;
		}
	}

	public override bool IsHoldingGoldenDisc
	{
		get
		{
			return base.IsHoldingGoldenDisc;
		}
		set
		{
			if (base.IsHoldingGoldenDisc != value)
			{
				if (value)
				{
					Singleton<ColorManager>.Instance.SetDiscColor(meleeDiscRenderers[0], Singleton<ColorManager>.Instance.goldenDiscColor);
				}
				else
				{
					Singleton<ColorManager>.Instance.SetDiscColor(meleeDiscRenderers[0], DiscColor);
				}
				base.IsHoldingGoldenDisc = value;
			}
		}
	}

	public Color DiscColor
	{
		get
		{
			if (decoy != null && decoy.owner != null && decoy.owner.team > 0)
			{
				return Singleton<ColorManager>.Instance.teamColors[decoy.owner.team];
			}
			if (player != null && player.team > 0)
			{
				return Singleton<ColorManager>.Instance.teamColors[player.team];
			}
			return _discColor;
		}
	}

	public override Material BodyMatNormal
	{
		get
		{
			if (team == 1)
			{
				return _bodyMatTeamOne;
			}
			if (team == 2)
			{
				return _bodyMatTeamTwo;
			}
			return base.BodyMatNormal;
		}
	}

	public override bool IsSliced
	{
		get
		{
			return base.IsSliced;
		}
		set
		{
			base.IsSliced = value;
			for (int i = 0; i < bodyNormal.Length; i++)
			{
				bodyNormal[i].SetActive(!value);
			}
			for (int j = 0; j < bodySliced.Length; j++)
			{
				bodySliced[j].SetActive(value);
			}
		}
	}

	public override float DangerEffect
	{
		get
		{
			return base.DangerEffect;
		}
		set
		{
			if (value != base.DangerEffect)
			{
				base.DangerEffect = value;
				if (dangerPropertyBlock == null)
				{
					dangerPropertyBlock = new MaterialPropertyBlock();
				}
				else
				{
					dangerPropertyBlock.Clear();
				}
				dangerPropertyBlock.SetColor(dangerShaderPropertyID, Color.Lerp(Color.black, dangerColor, value));
				for (int i = 0; i < colorableBits.Length; i++)
				{
					colorableBits[i].SetPropertyBlock(dangerPropertyBlock);
				}
			}
		}
	}

	public override void Reset()
	{
		base.Reset();
		IsSliced = false;
		if (deathSlice != null)
		{
			deathSlice.Reset();
			deathSlice.transform.localPosition = deathSliceInitPos;
		}
	}

	public override void InitCharacter(CharacterMode mode)
	{
		dangerShaderPropertyID = Shader.PropertyToID("_EmissionColor");
		base.InitCharacter(mode);
		if (deathSlice != null)
		{
			deathSliceInitPos = deathSlice.transform.localPosition;
		}
		if (characterMode == CharacterMode.Gameplay)
		{
			if (deathSlice != null)
			{
				deathSlice.InitDebris(Color);
			}
			if (actor != null)
			{
				if (deathSlice != null)
				{
					Physics2D.IgnoreCollision(deathSlice.GetComponent<Collider2D>(), actor.physicsCollider);
				}
				player = actor as Player;
				decoy = actor as Decoy;
			}
			if (uiShadow != null)
			{
				uiShadow.SetActive(value: false);
			}
		}
		else if (characterMode == CharacterMode.UI)
		{
			rightDisc.SetActive(value: false);
			leftDisc.SetActive(value: false);
			Reset();
			if (uiShadow != null)
			{
				uiShadow.SetActive(value: true);
			}
			else
			{
				uiShadow = Object.Instantiate(uiShadowPrefab, base.transform);
			}
		}
	}

	public void SetTeam(int team)
	{
		this.team = team;
		UpdateColors();
	}

	public override void UpdateColors()
	{
		base.UpdateColors();
		for (int i = 0; i < colorableBits.Length; i++)
		{
			colorableBits[i].material = BodyMatNormal;
		}
		for (int j = 0; j < meleeDiscRenderers.Length; j++)
		{
			Singleton<ColorManager>.Instance.SetDiscColor(meleeDiscRenderers[j], DiscColor);
		}
		bodySquished.transform.Find("Squished").GetComponent<Renderer>().material.SetColor("_Color", Color);
	}

	public override void SetMeleeDiscs(int numDiscs)
	{
		base.SetMeleeDiscs(numDiscs);
		if (IsSquished)
		{
			numDiscs = 0;
		}
		switch (numDiscs)
		{
		case 0:
			rightDisc.SetActive(value: false);
			leftDisc.SetActive(value: false);
			break;
		case 1:
			rightDisc.SetActive(value: true);
			leftDisc.SetActive(value: false);
			break;
		case 2:
			rightDisc.SetActive(value: true);
			leftDisc.SetActive(value: true);
			break;
		}
	}

	public override void SetMiniDiscs(int numGhostDiscs)
	{
		base.SetMiniDiscs(numGhostDiscs);
		if (numGhostDiscs > 0 && numGhostDiscs < 3)
		{
			SetMeleeDiscs(numGhostDiscs);
			if (actor != null)
			{
				anim.SetInteger("HasDiscs", numGhostDiscs);
			}
		}
		switch (numGhostDiscs)
		{
		case 0:
		case 3:
			if (IsHoldingGoldenDisc)
			{
				Singleton<ColorManager>.Instance.SetDiscColor(meleeDiscRenderers[0], Singleton<ColorManager>.Instance.goldenDiscColor, isMiniDisc: false, isGoldenDisc: true);
			}
			else
			{
				Singleton<ColorManager>.Instance.SetDiscColor(meleeDiscRenderers[0], DiscColor);
			}
			Singleton<ColorManager>.Instance.SetDiscColor(meleeDiscRenderers[1], DiscColor);
			break;
		case 1:
			meleeDiscRenderers[0].material = Singleton<ColorManager>.Instance.discMaterialGhost;
			Singleton<ColorManager>.Instance.SetDiscColor(meleeDiscRenderers[1], DiscColor);
			break;
		case 2:
			Singleton<ColorManager>.Instance.SetDiscColor(meleeDiscRenderers[0], DiscColor);
			meleeDiscRenderers[1].material = Singleton<ColorManager>.Instance.discMaterialGhost;
			break;
		}
		IsHoldingGoldenDisc = IsHoldingGoldenDisc;
	}

	protected override void Update()
	{
		base.Update();
		if (characterMode == CharacterMode.Gameplay && actor != null && player != null)
		{
			actor.dashTrail.transform.position = new Vector3(actor.dashTrail.transform.position.x, actor.dashTrail.transform.position.y, neckJoint.position.z);
			player.shield.transform.position = new Vector3(player.shield.transform.position.x, player.shield.transform.position.y, neckJoint.position.z);
		}
	}

	public override void Slice(Vector3 sliceVector)
	{
		base.Slice(sliceVector);
		if (deathSlice != null)
		{
			if (characterType == CharacterType.Avocado)
			{
				deathSlice.Slice(actor.transform.up);
			}
			else
			{
				deathSlice.Slice(sliceVector);
			}
		}
		StartCoroutine(DelayFaceplant());
	}

	public override void EvaporateSlice()
	{
		base.EvaporateSlice();
		if (deathSlice != null && deathSlice.gameObject.activeSelf)
		{
			Singleton<ParticleManager>.Instance.PlayPFX(PFXType.SpawnBurst, deathSlice.transform.position);
		}
	}

	public override void FallInHazard()
	{
		if (IsSliced)
		{
			base.IsVisible = false;
			if (deathSlice != null)
			{
				Renderer[] componentsInChildren = deathSlice.transform.GetComponentsInChildren<Renderer>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].enabled = true;
				}
			}
		}
		else
		{
			base.IsVisible = false;
		}
	}

	protected IEnumerator DelayFaceplant()
	{
		yield return new WaitForSeconds(slicedFaceplantDelay);
		Faceplant();
	}

	protected virtual void Faceplant()
	{
		if (base.IsVisible && !IsSquished)
		{
			Singleton<ParticleManager>.Instance.PlayPFX(PFXType.SpawnBurst, actor.shadow.transform.position);
			Singleton<AudioManager>.Instance.PlayOneShotAt("event:/combat/faceplant", base.transform.position);
		}
	}
}
