using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PowerupText : MonoBehaviour
{
	public PowerupType powerupType;

	private Text textComponent;

	private string content;

	private float effectTimer;

	private float effectDuration = 2f;

	private Vector3 drift = new Vector3(0f, 2f, 0f);

	private RectTransform bgComponent;

	private Image bgComponentImage;

	public RectTransform iconComponent;

	public Image icon;

	private CanvasGroup canvasGroup;

	private bool isReleased;

	public bool isShowing;

	private IEnumerator ShowHideRoutine;

	private float showTimer;

	private float showDuration = 0.5f;

	private Vector3 hidePos;

	private Vector3 hideScale;

	private Vector3 showPos;

	private Vector3 showScale;

	public void Init()
	{
		drift = base.transform.up * 2f;
		isReleased = false;
		isShowing = false;
		showTimer = 0f;
		showPos = base.transform.position;
		showScale = base.transform.localScale;
		hidePos = base.transform.position - base.transform.up * 0.5f;
		hideScale = showScale * 0.75f;
		SetLabel(powerupType);
		effectTimer = 0f;
		bgComponent = base.transform.Find("BG").GetComponent<RectTransform>();
		bgComponentImage = bgComponent.GetComponent<Image>();
		iconComponent = base.transform.Find("Icon").GetComponent<RectTransform>();
		icon = iconComponent.GetComponent<Image>();
		canvasGroup = GetComponent<CanvasGroup>();
		textComponent = base.transform.Find("BG/Text").GetComponent<Text>();
		canvasGroup.alpha = 0f;
		textComponent.text = content;
		bgComponent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textComponent.preferredWidth + 20f);
	}

	private void Update()
	{
		if (isReleased)
		{
			if (effectTimer < effectDuration)
			{
				base.transform.position = Vector3.Lerp(base.transform.position, base.transform.TransformPoint(drift), Easing.EaseOut(effectTimer / effectDuration));
				canvasGroup.alpha = Mathf.Lerp(1f, 0f, Easing.EaseIn(effectTimer / effectDuration));
				effectTimer += Time.deltaTime;
			}
			else
			{
				Singleton<Camera3D>.Instance.RemoveCameraTarget(base.transform);
				Object.Destroy(base.gameObject);
			}
		}
	}

	public void Release(Color releaseColor)
	{
		SetLabel(powerupType);
		textComponent.color = releaseColor;
		if (!isShowing)
		{
			textComponent = base.transform.Find("BG/Text").GetComponent<Text>();
			textComponent.text = content;
			bgComponent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textComponent.preferredWidth + 40f);
			iconComponent.anchoredPosition = new Vector2(0f - (bgComponent.rect.width * 0.5f + 20f), iconComponent.anchoredPosition.y);
		}
		isReleased = true;
		Vector3 position = base.transform.position;
		base.transform.SetParent(null, worldPositionStays: true);
		base.transform.position = position;
		base.transform.rotation = Quaternion.Euler(-20f, 0f, 0f);
		Singleton<Camera3D>.Instance.AddCameraTarget(base.transform, 0.95f, 0.15f);
	}

	public void SetAlpha(float factor)
	{
		canvasGroup.alpha = Mathf.Lerp(0f, 1f, Easing.QuartIn(factor));
	}

	public void Show()
	{
		textComponent.text = content;
		bgComponent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textComponent.preferredWidth + 20f);
		if (ShowHideRoutine != null)
		{
			StopCoroutine(ShowHideRoutine);
		}
		ShowHideRoutine = ShowText();
		StartCoroutine(ShowHideRoutine);
	}

	public void Hide()
	{
		if (ShowHideRoutine != null)
		{
			StopCoroutine(ShowHideRoutine);
		}
		ShowHideRoutine = HideText();
		StartCoroutine(ShowHideRoutine);
	}

	private IEnumerator ShowText()
	{
		isShowing = true;
		while (showTimer < showDuration)
		{
			showTimer += Time.deltaTime;
			canvasGroup.alpha = Mathf.Lerp(0f, 1f, Easing.EaseIn(showTimer / showDuration));
			base.transform.position = Vector3.Lerp(hidePos, showPos, Easing.EaseOut(showTimer / showDuration));
			base.transform.localScale = Vector3.Lerp(hideScale, showScale, Easing.EaseOut(showTimer / showDuration));
			yield return null;
		}
		showTimer = showDuration;
	}

	private IEnumerator HideText()
	{
		isShowing = false;
		while (showTimer > 0f)
		{
			showTimer -= Time.deltaTime;
			canvasGroup.alpha = Mathf.Lerp(0f, 1f, Easing.EaseIn(showTimer / (showDuration * 0.5f)));
			base.transform.position = Vector3.Lerp(hidePos, showPos, Easing.EaseOut(showTimer / showDuration));
			yield return null;
		}
		showTimer = 0f;
	}

	public void ScaleToCameraZoom()
	{
		Vector3 localScale = GetComponent<RectTransform>().localScale;
		GetComponent<RectTransform>().localScale = localScale * (0.66f + 1.25f * (Singleton<Camera3D>.Instance.currentZoom / Singleton<Camera3D>.Instance.maximumZoom));
	}

	public void SetLabel(PowerupType powerup)
	{
		content = powerup.ToString().ToUpper();
		switch (powerup)
		{
		case PowerupType.None:
			content = "RANDOM";
			break;
		case PowerupType.DashThroughWalls:
			content = "DASH THROUGH WALLS";
			break;
		case PowerupType.MoveFaster:
			content = "CAFFEINATED";
			break;
		case PowerupType.ReverseInputs:
			content = "BAMBOOZLED";
			break;
		case PowerupType.ExplosiveDisc:
			content = "EXPLOSIVE BOOMERANG";
			break;
		case PowerupType.SlimeDisc:
			content = "ICE BOOMERANG";
			break;
		case PowerupType.MultiDisc:
			content = "MULTI BOOMERANG";
			break;
		case PowerupType.Shield:
			content = "SHIELD";
			break;
		case PowerupType.TeleportDisc:
			content = "TELEPORT BOOMERANG";
			break;
		case PowerupType.ExtraDisc:
			content = "EXTRA BOOMERANG";
			break;
		case PowerupType.FireDisc:
			content = "FIRE BOOMERANG";
			break;
		case PowerupType.Disguise:
			content = "DISGUISE";
			break;
		case PowerupType.TimeImmune:
			content = "TIME WARP";
			break;
		case PowerupType.ControlledDisc:
			content = "TELEKINESIS";
			break;
		case PowerupType.ShrinkingBounds:
			content = "BATTLE ROYALE";
			break;
		}
		if (textComponent != null)
		{
			textComponent.text = content;
			bgComponent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textComponent.preferredWidth + 20f);
		}
	}
}
