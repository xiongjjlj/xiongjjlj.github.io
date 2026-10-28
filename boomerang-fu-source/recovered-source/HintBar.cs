using System.Collections;
using System.Collections.Generic;
using I2.Loc;
using TMPro;
using UnityEngine;

public class HintBar : MonoBehaviour
{
	public enum HintType
	{
		None,
		Dash,
		Attack,
		Throw,
		Telekinesis,
		Teleport,
		ChipOut,
		ControlledDisc,
		Decoy
	}

	public Dictionary<HintType, int> hintCounter = new Dictionary<HintType, int>();

	public Player player;

	public HintType currentHint;

	public ButtonPrompt buttonPrompt;

	public TextMeshProUGUI text;

	public Animator anim;

	private float hintTimer;

	private static float minTimeBetweenHints = 3f;

	private RectTransform parentRect;

	private void Start()
	{
		parentRect = base.transform.parent.GetComponent<RectTransform>();
	}

	public void Init(Player player)
	{
		this.player = player;
		hintCounter.Clear();
		hintCounter = new Dictionary<HintType, int>
		{
			{
				HintType.Dash,
				0
			},
			{
				HintType.Attack,
				0
			},
			{
				HintType.Throw,
				0
			},
			{
				HintType.Telekinesis,
				0
			},
			{
				HintType.Teleport,
				0
			},
			{
				HintType.ChipOut,
				0
			},
			{
				HintType.ControlledDisc,
				0
			},
			{
				HintType.Decoy,
				0
			}
		};
		GetComponent<RectTransform>().localScale = Vector3.one;
		currentHint = HintType.None;
		hintTimer = 0f;
	}

	private void Update()
	{
		if (hintTimer >= 0f)
		{
			hintTimer -= Time.deltaTime;
		}
	}

	private void LateUpdate()
	{
		if (currentHint != HintType.None)
		{
			FollowCandidate();
		}
	}

	private void FollowCandidate()
	{
		if (player != null)
		{
			Vector2 screenPoint = Singleton<Camera3D>.Instance.gameCamera.WorldToScreenPoint(player.transform.position);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, Singleton<Camera3D>.Instance.gameCamera, out var localPoint);
			base.transform.localPosition = localPoint + new Vector2(0f, 100f);
		}
	}

	public void ShowHint(HintType hintType, float duration = 5f)
	{
		if ((currentHint != HintType.None || hintTimer > 0f) && hintTimer > 0f)
		{
			return;
		}
		RewiredController component = player.GetComponent<RewiredController>();
		if (component == null || hintCounter[hintType] > 2)
		{
			Debug.Log("Too many " + hintType.ToString() + " hints. I've already shown " + hintCounter[hintType]);
		}
		else if (!(player.alertBar != null) || player.alertBar.currentAlert == AlertBar.AlertType.None)
		{
			StopAllCoroutines();
			FollowCandidate();
			switch (hintType)
			{
			case HintType.Dash:
				buttonPrompt.Init(component.rewiredPlayer, "Dash");
				text.text = LocalizationManager.GetTranslation("hint_dash");
				break;
			case HintType.Attack:
				buttonPrompt.Init(component.rewiredPlayer, "Swipe");
				text.text = LocalizationManager.GetTranslation("hint_slash");
				break;
			case HintType.Telekinesis:
				buttonPrompt.Init(component.rewiredPlayer, "Throw");
				text.text = LocalizationManager.GetTranslation("hint_pullback");
				break;
			case HintType.Teleport:
				buttonPrompt.Init(component.rewiredPlayer, "Dash");
				text.text = LocalizationManager.GetTranslation("hint_teleport");
				break;
			case HintType.Throw:
				buttonPrompt.Init(component.rewiredPlayer, "Throw");
				text.text = LocalizationManager.GetTranslation("hint_throw");
				break;
			case HintType.ChipOut:
				buttonPrompt.Init(component.rewiredPlayer, "Swipe");
				text.text = LocalizationManager.GetTranslation("hint_struggle");
				break;
			case HintType.ControlledDisc:
				buttonPrompt.Init(component.rewiredPlayer, "Throw");
				text.text = LocalizationManager.GetTranslation("hint_telekinesis");
				break;
			case HintType.Decoy:
				buttonPrompt.Init(component.rewiredPlayer, "Dash");
				text.text = LocalizationManager.GetTranslation("powerup_decoy");
				break;
			}
			hintCounter[hintType]++;
			currentHint = hintType;
			anim.ResetTrigger("Hide");
			anim.SetTrigger("Show");
			HideHint(duration);
			hintTimer = minTimeBetweenHints;
		}
	}

	public void HideHint(float delay = 0f)
	{
		StopAllCoroutines();
		StartCoroutine(HideHintAfterDelay(delay));
	}

	private IEnumerator HideHintAfterDelay(float delay)
	{
		if (delay > 0f)
		{
			yield return new WaitForSecondsRealtime(delay);
		}
		anim.ResetTrigger("Show");
		anim.SetTrigger("Hide");
		currentHint = HintType.None;
		yield return new WaitForSecondsRealtime(0.2f);
	}
}
