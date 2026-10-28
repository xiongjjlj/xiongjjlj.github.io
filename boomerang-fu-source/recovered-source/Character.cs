using DG.Tweening;
using UnityEngine;

public class Character : MonoBehaviour
{
	public enum CharacterMode
	{
		Gameplay,
		UI
	}

	public CharacterMode characterMode;

	[Header("Details")]
	public CharacterType characterType;

	public Vector3 characterOffset = new Vector3(0.4f, 0f, 0f);

	public Vector3 shadowOffset = new Vector3(0f, 0f, 0f);

	public FacialExpression idleFace;

	public FacialExpression blinkFace = FacialExpression.blinkcontent;

	public FacialExpression victoryFace = FacialExpression.happy;

	public FacialExpression spawnFace = FacialExpression.aggressive;

	private FacialExpression currentFacialExpression;

	public float facialExpressionTimer;

	[HideInInspector]
	public Actor actor;

	[HideInInspector]
	public Animator anim;

	public AnimatorOverrideController animControllerGameplay;

	public AnimatorOverrideController animControllerUI;

	[Header("Materials")]
	[SerializeField]
	public Color colorDefault;

	[SerializeField]
	public Color colorTeamOne;

	[SerializeField]
	public Color colorTeamTwo;

	public Renderer[] colorableBits;

	public Sprite lobbySpriteIdle;

	public Sprite lobbySpriteHappy;

	public Sprite scoresSpriteIdle;

	public Material _bodyMatNormal;

	public Material bodyMatBurnt;

	[SerializeField]
	protected Renderer[] visibleBits;

	[Header("Components")]
	public Renderer faceplate;

	public Transform leftHandJoint;

	public Transform rightHandJoint;

	public Transform neckJoint;

	public Transform hipJoint;

	public Transform hatJoint;

	public Transform faceJoint;

	protected Hat _currentHat;

	private GameObject currentHatInstance;

	private Debris3D currentHatDebrisInstance;

	public HatType equippedHat;

	private MaterialPropertyBlock faceplatePropertyBlock;

	[HideInInspector]
	public float hipJointPosZ = 0.889f;

	public GameObject leftDisc;

	public GameObject rightDisc;

	protected Renderer[] meleeDiscRenderers;

	[Header("State")]
	[SerializeField]
	protected bool _isVisible;

	[SerializeField]
	protected bool _isSliced;

	[SerializeField]
	protected float _dangerEffect;

	[SerializeField]
	protected bool _isBurnt;

	[SerializeField]
	protected bool _isSlimed;

	protected int snowShaderPropertyID = -1;

	private MaterialPropertyBlock snowPropertyBlock;

	[SerializeField]
	protected bool _hasGoldenDisc;

	[SerializeField]
	protected bool _isSquished;

	[Header("Death")]
	public GameObject[] squishableBits;

	public GameObject bodySquished;

	[Header("Delays")]
	public float groundSlamDelayOnSpawn = 0.4333f;

	public float slicedFaceplantDelay = 0.75f;

	[Header("Shadow")]
	public float shadowScale = 1f;

	[HideInInspector]
	public virtual Color Color
	{
		get
		{
			if (actor == null)
			{
				return colorDefault;
			}
			if (actor != null && actor.team == 1)
			{
				return colorTeamOne;
			}
			if (actor != null && actor.team == 2)
			{
				return colorTeamTwo;
			}
			return colorDefault;
		}
	}

	public virtual Material BodyMatNormal => _bodyMatNormal;

	public Hat CurrentHat => _currentHat;

	public bool IsVisible
	{
		get
		{
			return _isVisible;
		}
		set
		{
			if (_isVisible == value)
			{
				return;
			}
			if (value)
			{
				if (actor != null)
				{
					actor.shadow.SetActive(value: true);
				}
				IsSliced = IsSliced;
				IsBurnt = IsBurnt;
				IsSquished = IsSquished;
				for (int i = 0; i < visibleBits.Length; i++)
				{
					visibleBits[i].enabled = true;
				}
				for (int j = 0; j < meleeDiscRenderers.Length; j++)
				{
					meleeDiscRenderers[j].enabled = true;
				}
				if (actor != null && actor.actorState != Actor.ActorState.Dead)
				{
					ShowCurrentHat();
				}
			}
			else
			{
				if (actor != null)
				{
					actor.shadow.SetActive(value: false);
				}
				for (int k = 0; k < visibleBits.Length; k++)
				{
					visibleBits[k].enabled = false;
				}
				for (int l = 0; l < meleeDiscRenderers.Length; l++)
				{
					meleeDiscRenderers[l].enabled = false;
				}
				HideCurrentHat();
			}
			_isVisible = value;
		}
	}

	public virtual bool IsSliced
	{
		get
		{
			return _isSliced;
		}
		set
		{
			if (!_isSliced & value)
			{
				Singleton<AudioManager>.Instance.PlayOneShotAt("event:/death/sliced", base.transform.position);
			}
			_isSliced = value;
		}
	}

	public virtual float DangerEffect
	{
		get
		{
			return _dangerEffect;
		}
		set
		{
			_dangerEffect = value;
		}
	}

	public virtual bool IsBurnt
	{
		get
		{
			return _isBurnt;
		}
		set
		{
			if (_isBurnt == value)
			{
				return;
			}
			if (value)
			{
				for (int i = 0; i < colorableBits.Length; i++)
				{
					colorableBits[i].material = bodyMatBurnt;
				}
			}
			else
			{
				if (actor != null)
				{
					anim.SetBool("IsBurnt", value: false);
				}
				for (int j = 0; j < colorableBits.Length; j++)
				{
					colorableBits[j].material = BodyMatNormal;
				}
			}
			_isBurnt = value;
		}
	}

	public bool IsSlimed
	{
		get
		{
			return _isSlimed;
		}
		set
		{
			if (_isSlimed == value)
			{
				return;
			}
			if (snowPropertyBlock == null)
			{
				snowPropertyBlock = new MaterialPropertyBlock();
			}
			else
			{
				snowPropertyBlock.Clear();
			}
			if (value)
			{
				for (int i = 0; i < colorableBits.Length; i++)
				{
					snowPropertyBlock.SetFloat(snowShaderPropertyID, 0.425f);
					colorableBits[i].SetPropertyBlock(snowPropertyBlock);
				}
			}
			else
			{
				for (int j = 0; j < colorableBits.Length; j++)
				{
					snowPropertyBlock.SetFloat(snowShaderPropertyID, 0f);
					colorableBits[j].SetPropertyBlock(snowPropertyBlock);
				}
			}
			_isSlimed = value;
		}
	}

	public virtual bool IsHoldingGoldenDisc
	{
		get
		{
			return _hasGoldenDisc;
		}
		set
		{
			_hasGoldenDisc = value;
		}
	}

	public virtual bool IsSquished
	{
		get
		{
			return _isSquished;
		}
		set
		{
			if (_isSquished == value)
			{
				return;
			}
			if (value)
			{
				if (actor != null)
				{
					actor.shadow.SetActive(value: false);
				}
				for (int i = 0; i < squishableBits.Length; i++)
				{
					squishableBits[i].SetActive(value: false);
				}
				bodySquished.SetActive(value: true);
				bodySquished.transform.DOComplete();
				bodySquished.transform.localScale = Vector3.zero;
				bodySquished.transform.DOScale(0.8f, 0.25f).SetEase(Ease.OutBack);
				SetMeleeDiscs(0);
				Singleton<AudioManager>.Instance.PlayOneShotAt("event:/death/squished", base.transform.position);
			}
			else
			{
				for (int j = 0; j < squishableBits.Length; j++)
				{
					squishableBits[j].SetActive(value: true);
				}
				bodySquished.SetActive(value: false);
				if (actor != null)
				{
					actor.shadow.SetActive(value: true);
				}
			}
			_isSquished = value;
		}
	}

	public virtual void InitCharacter(CharacterMode mode)
	{
		snowShaderPropertyID = Shader.PropertyToID("_SnowThr");
		anim = GetComponentInChildren<Animator>();
		characterMode = mode;
		if (leftDisc != null && rightDisc != null)
		{
			meleeDiscRenderers = new Renderer[2]
			{
				rightDisc.GetComponent<Renderer>(),
				leftDisc.GetComponent<Renderer>()
			};
		}
		visibleBits = base.transform.GetComponentsInChildren<Renderer>(includeInactive: true);
		SetHat();
		switch (mode)
		{
		case CharacterMode.Gameplay:
			base.transform.localScale = Vector3.one;
			anim.runtimeAnimatorController = animControllerGameplay;
			anim.updateMode = AnimatorUpdateMode.Normal;
			LayerExt.MoveFromLayerToLayer(base.transform, LayerMask.NameToLayer("UI"), LayerMask.NameToLayer("Default"));
			break;
		case CharacterMode.UI:
			anim.runtimeAnimatorController = animControllerUI;
			anim.updateMode = AnimatorUpdateMode.UnscaledTime;
			LayerExt.MoveFromLayerToLayer(base.transform, LayerMask.NameToLayer("Default"), LayerMask.NameToLayer("UI"));
			if (anim.gameObject.activeSelf)
			{
				anim.SetFloat("IdleOffset", Random.Range(0f, 0.99f));
			}
			break;
		}
		SetFacialExpression(idleFace);
	}

	public virtual void Reset()
	{
		IsHoldingGoldenDisc = false;
		IsVisible = true;
		IsBurnt = false;
		IsSlimed = false;
		IsSquished = false;
		IsSliced = false;
		bodySquished.SetActive(value: false);
		if (actor != null)
		{
			anim.SetBool("IsSliced", value: false);
			anim.SetBool("IsFalling", value: false);
		}
		SetHat();
		if (_currentHat != null && currentHatDebrisInstance != null)
		{
			currentHatDebrisInstance.Reset();
		}
	}

	public void SetHat()
	{
		if (!(hatJoint != null) || !(faceJoint != null))
		{
			return;
		}
		if (equippedHat != HatType.None)
		{
			AddHat(equippedHat);
		}
		else if (actor != null && actor as Player != null)
		{
			if (Singleton<GameManager>.Instance.IsLeadingPlayer(actor as Player) && !Singleton<SettingsManager>.Instance.trailerMode && Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.HideAndSeek)
			{
				AddHat(HatType.Crown);
			}
			else
			{
				RemoveCurrentHat();
			}
		}
		else
		{
			RemoveCurrentHat();
		}
	}

	public void BounceCurrentHat()
	{
		if (currentHatInstance != null)
		{
			currentHatInstance.transform.DOComplete();
			currentHatInstance.transform.DOScale(0.8f, 0.15f).From(isRelative: false).SetEase(Ease.OutBack);
		}
	}

	public void SquashTallHat(bool squash)
	{
		if (!(currentHatInstance == null) && _currentHat != null && _currentHat.isTall)
		{
			if (squash)
			{
				currentHatInstance.transform.localScale = new Vector3(1f, 0.4f, 1f);
			}
			else
			{
				currentHatInstance.transform.localScale = Vector3.one;
			}
		}
	}

	protected virtual void Update()
	{
		if (characterMode == CharacterMode.UI)
		{
			facialExpressionTimer -= Time.unscaledDeltaTime;
		}
		else
		{
			facialExpressionTimer -= Time.deltaTime;
		}
		if (!(facialExpressionTimer <= 0f))
		{
			return;
		}
		if (characterMode == CharacterMode.Gameplay && actor != null && actor is Player)
		{
			if (actor.actorState == Actor.ActorState.Dead)
			{
				SetFacialExpression(FacialExpression.dead);
			}
			else if ((actor as Player).activePowerup.HasPowerup(PowerupType.ReverseInputs))
			{
				SetFacialExpression(FacialExpression.bamboozled);
			}
			else if (actor.IsAiming)
			{
				SetFacialExpression(FacialExpression.focus);
			}
			else if (actor.IsFrozen)
			{
				SetFacialExpression(FacialExpression.retrieving);
			}
			else if (actor.actorState == Actor.ActorState.Falling || actor.isOnFire)
			{
				SetFacialExpression(FacialExpression.panic);
			}
			else if (Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.GoldenDisc && Singleton<SettingsManager>.Instance.matchType != SettingsManager.MatchType.KingOfTheHill && (float)Singleton<GameManager>.Instance.OpponentsLeftStandingNow() < 2f)
			{
				if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
				{
					if (Singleton<GameManager>.Instance.IsPlayerSeeker(actor as Player) && Singleton<GameManager>.Instance.HidersLeftStandingNow() > 0)
					{
						SetFacialExpression(FacialExpression.shocked);
					}
					else
					{
						SetFacialExpression(FacialExpression.happy);
					}
				}
				else if (actor.isShowingOff)
				{
					SetFacialExpression(victoryFace);
				}
				else
				{
					SetFacialExpression(FacialExpression.happy);
				}
			}
			else if ((actor as Player).isOutOfBounds && (actor as Player).outOfBoundsTimer < Player.outOfBoundsDuration * 0.5f)
			{
				SetFacialExpression(FacialExpression.worried);
			}
			else if (actor.IsStunned)
			{
				SetFacialExpression(FacialExpression.hurt);
			}
			else if (actor.IsDashing || actor.IsAttacking || actor.isLunging)
			{
				SetFacialExpression(FacialExpression.aggressive);
			}
			else
			{
				SetFacialExpression(FacialExpression.idle);
			}
		}
		else if (characterMode == CharacterMode.UI || actor is Decoy)
		{
			SetFacialExpression(FacialExpression.idle);
		}
	}

	public void SetFacialExpression(FacialExpression expression, float duration = 0.7f)
	{
		if (currentFacialExpression == FacialExpression.idle && expression == FacialExpression.idle)
		{
			if (Chance.PercentageTrue(0.2f))
			{
				SetFaceSheetOffset(blinkFace);
				facialExpressionTimer = Random.Range(0.07f, 0.1f);
				currentFacialExpression = FacialExpression.blink;
			}
			return;
		}
		currentFacialExpression = expression;
		if (expression == FacialExpression.idle)
		{
			SetFaceSheetOffset(idleFace);
		}
		else
		{
			SetFaceSheetOffset(expression);
		}
		if (duration > 0f)
		{
			facialExpressionTimer = Random.Range(duration, duration + 0.1f);
		}
		else
		{
			facialExpressionTimer = -1f;
		}
	}

	public virtual void SetFaceSheetOffset(FacialExpression expression)
	{
		float num = 4f;
		float num2 = (float)expression % num;
		float num3 = (int)Mathf.Floor((float)expression / num);
		Vector2 vector = new Vector2(num2 / num, (num - 1f) / num - num3 / num);
		if (faceplatePropertyBlock == null)
		{
			faceplatePropertyBlock = new MaterialPropertyBlock();
		}
		faceplatePropertyBlock.SetVector("_MainTex_ST", new Vector4(0.25f, 0.25f, vector.x, vector.y));
		faceplate.SetPropertyBlock(faceplatePropertyBlock);
	}

	public virtual void UpdateColors()
	{
	}

	public virtual void SetMeleeDiscs(int numDiscs)
	{
	}

	public virtual void SetMiniDiscs(int numGhostDiscs)
	{
	}

	public virtual void Slice(Vector3 sliceVector)
	{
		if (actor != null && actor.actorState == Actor.ActorState.Alive)
		{
			anim.SetBool("IsSliced", value: true);
		}
		IsSliced = true;
		LoseCurrentHat(sliceVector);
	}

	public virtual void LoseCurrentHat(Vector3 direction)
	{
		if (_currentHat == null)
		{
			return;
		}
		if (actor != null && _currentHat.hatDebrisPrefab != null)
		{
			if (currentHatDebrisInstance == null)
			{
				currentHatDebrisInstance = _currentHat.hatDebrisPrefab.Spawn(null, actor.transform.position, actor.transform.rotation);
				Physics2D.IgnoreCollision(currentHatDebrisInstance.GetComponent<Collider2D>(), actor.physicsCollider);
				currentHatDebrisInstance.InitDebris();
			}
			else
			{
				currentHatDebrisInstance.transform.position = actor.transform.position;
				currentHatDebrisInstance.Reset();
			}
			currentHatDebrisInstance.ApplyForce(direction.normalized);
		}
		HideCurrentHat();
	}

	public virtual void EvaporateSlice()
	{
	}

	public virtual void FallInHazard()
	{
		IsVisible = false;
	}

	public float GetBodyHeight()
	{
		Vector3 center = colorableBits[0].bounds.center;
		Vector3 extents = colorableBits[0].bounds.extents;
		Vector2[] obj = new Vector2[8]
		{
			WorldToGUIPoint(new Vector3(center.x - extents.x, center.y - extents.y, center.z - extents.z)),
			WorldToGUIPoint(new Vector3(center.x + extents.x, center.y - extents.y, center.z - extents.z)),
			WorldToGUIPoint(new Vector3(center.x - extents.x, center.y - extents.y, center.z + extents.z)),
			WorldToGUIPoint(new Vector3(center.x + extents.x, center.y - extents.y, center.z + extents.z)),
			WorldToGUIPoint(new Vector3(center.x - extents.x, center.y + extents.y, center.z - extents.z)),
			WorldToGUIPoint(new Vector3(center.x + extents.x, center.y + extents.y, center.z - extents.z)),
			WorldToGUIPoint(new Vector3(center.x - extents.x, center.y + extents.y, center.z + extents.z)),
			WorldToGUIPoint(new Vector3(center.x + extents.x, center.y + extents.y, center.z + extents.z))
		};
		Vector2 lhs = obj[0];
		Vector2 lhs2 = obj[0];
		Vector2[] array = obj;
		foreach (Vector2 rhs in array)
		{
			lhs = Vector2.Min(lhs, rhs);
			lhs2 = Vector2.Max(lhs2, rhs);
		}
		return lhs2.y - lhs.y;
	}

	public static Vector2 WorldToGUIPoint(Vector3 world)
	{
		Vector2 result = Singleton<Camera3D>.Instance.gameCamera.WorldToScreenPoint(world);
		result.y = (float)Screen.height - result.y;
		return result;
	}

	public virtual void AddHat(HatType targetHatType)
	{
		if (_currentHat != null && _currentHat.hatType != targetHatType)
		{
			RemoveCurrentHat();
		}
		if (_currentHat == null)
		{
			_currentHat = Singleton<GameManager>.Instance.GetHat(targetHatType);
			if (_currentHat != null && _currentHat.hatPrefab != null)
			{
				Transform parent = hatJoint;
				if (_currentHat.hatAttachment == Hat.HatAttachment.Faceplate)
				{
					parent = faceJoint;
				}
				currentHatInstance = _currentHat.hatPrefab.Spawn(parent, Vector3.zero);
				currentHatInstance.transform.localScale = Vector3.one;
				LayerExt.MoveFromLayerToLayer(currentHatInstance.transform, currentHatInstance.layer, base.gameObject.layer);
			}
		}
		if (_currentHat != null && _currentHat.hatType == targetHatType)
		{
			currentHatInstance.SetActive(value: true);
		}
	}

	public virtual void RemoveCurrentHat()
	{
		if (_currentHat != null && currentHatInstance != null)
		{
			currentHatInstance.Recycle();
			if (currentHatDebrisInstance != null)
			{
				currentHatDebrisInstance.Reset();
				currentHatDebrisInstance.Recycle();
			}
			_currentHat = null;
		}
	}

	public virtual void ShowCurrentHat()
	{
		if (_currentHat != null && currentHatInstance != null)
		{
			currentHatInstance.SetActive(value: true);
		}
	}

	protected virtual void HideCurrentHat()
	{
		if (_currentHat != null && currentHatInstance != null)
		{
			currentHatInstance.SetActive(value: false);
		}
	}

	public Vector3 GetHatInstancePos()
	{
		if (currentHatInstance == null)
		{
			return Vector3.zero;
		}
		return currentHatInstance.transform.position;
	}

	public void UpdateUIScaleAndPosition(float parentWidth, float relativeScale)
	{
		base.transform.localScale = Vector3.one * relativeScale * parentWidth;
		base.transform.localPosition = new Vector3(0f, (0f - parentWidth) * relativeScale, -100f);
	}
}
