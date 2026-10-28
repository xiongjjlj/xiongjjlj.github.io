using System;
using System.Collections;
using System.Collections.Generic;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AlertBar : MonoBehaviour
{
	public enum AlertType
	{
		None,
		InTheLead,
		Joined,
		Disconnected,
		DoubleKill,
		TripleKill,
		MultiKill,
		PlayerID
	}

	[Serializable]
	public class PowerupColor
	{
		public PowerupType powerupType;

		public Color color = Color.white;

		public Sprite sprite;
	}

	public AlertType currentAlert;

	public PowerupType currentPowerup;

	public Player player;

	public Image bg;

	public TextMeshProUGUI text;

	public Animator anim;

	public Image icon;

	public Image chevron;

	private RectTransform parentRect;

	public List<PowerupColor> powerupColors;

	private void Start()
	{
		parentRect = base.transform.parent.GetComponent<RectTransform>();
	}

	public void Init(Player player)
	{
		this.player = player;
		player.alertBar = this;
		GetComponent<RectTransform>().localScale = Vector3.one;
		currentAlert = AlertType.None;
		currentPowerup = PowerupType.None;
		text.color = Color.black;
		SetBackgroundToPlayerColor();
		icon.gameObject.SetActive(value: false);
	}

	public Color GetPowerupColor(PowerupType powerupType)
	{
		for (int i = 0; i < powerupColors.Count; i++)
		{
			if (powerupColors[i].powerupType == powerupType)
			{
				return powerupColors[i].color;
			}
		}
		Debug.Log("Powerup color not found");
		return Color.white;
	}

	private Sprite GetPowerupSprite(PowerupType powerupType)
	{
		for (int i = 0; i < powerupColors.Count; i++)
		{
			if (powerupColors[i].powerupType == powerupType)
			{
				return powerupColors[i].sprite;
			}
		}
		Debug.Log("Powerup sprite not found");
		return null;
	}

	private void Update()
	{
		if (currentAlert != AlertType.None || currentPowerup != PowerupType.None)
		{
			FollowPlayer();
		}
	}

	private void FollowPlayer()
	{
		if (player != null)
		{
			Vector2 screenPoint = Singleton<Camera3D>.Instance.gameCamera.WorldToScreenPoint(player.transform.position);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, Singleton<Camera3D>.Instance.gameCamera, out var localPoint);
			base.transform.localPosition = localPoint;
		}
	}

	private void SetBackgroundToPlayerColor()
	{
		if (!(player == null))
		{
			HSBColor hsbColor = HSBColor.FromColor(player.character.Color);
			hsbColor.b = 1f;
			hsbColor.s = Mathf.Min(hsbColor.s, 0.75f);
			bg.color = HSBColor.ToColor(hsbColor);
		}
	}

	private void SetBackgroundToPowerupColor(PowerupType powerupType)
	{
		bg.color = GetPowerupColor(powerupType);
	}

	public void ShowAlert(AlertType alertType, float duration = 1.5f, float delay = 0.1f)
	{
		if (player.actorState != Actor.ActorState.Alive || currentPowerup != PowerupType.None || Singleton<SettingsManager>.Instance.trailerMode)
		{
			return;
		}
		player.playerInput.HideCurrentHint();
		if (delay > 0f)
		{
			StartCoroutine(ShowAlertAfterDelay(alertType, duration, delay));
			return;
		}
		icon.gameObject.SetActive(value: false);
		SetBackgroundToPlayerColor();
		StopAllCoroutines();
		FollowPlayer();
		text.color = Color.black;
		switch (alertType)
		{
		case AlertType.InTheLead:
			text.text = LocalizationManager.GetTranslation("alert_inthelead");
			break;
		case AlertType.Joined:
			text.text = LocalizationManager.GetTranslation("ui_player_prefix") + (player.playerID + 1);
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/show_alert");
			break;
		case AlertType.Disconnected:
			text.text = LocalizationManager.GetTranslation("ui_player_prefix") + (player.playerID + 1) + LocalizationManager.GetTranslation("alert_disconnected");
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/show_alert");
			break;
		case AlertType.DoubleKill:
			text.text = LocalizationManager.GetTranslation("alert_doublekill");
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/show_alert");
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/doublekill");
			break;
		case AlertType.TripleKill:
			text.text = LocalizationManager.GetTranslation("alert_triplekill");
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/show_alert");
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/doublekill");
			break;
		case AlertType.MultiKill:
			text.text = LocalizationManager.GetTranslation("alert_killingspree");
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/show_alert");
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/doublekill");
			break;
		case AlertType.PlayerID:
			if ((bool)player.GetComponent<PlayerAI>())
			{
				text.text = LocalizationManager.GetTranslation("ui_bot");
			}
			else
			{
				text.text = LocalizationManager.GetTranslation("ui_player_prefix") + (player.playerID + 1);
			}
			break;
		}
		currentAlert = alertType;
		anim.ResetTrigger("Hide");
		anim.SetTrigger("Show");
		if (duration > 0f)
		{
			HideAlert(duration);
		}
	}

	private IEnumerator ShowAlertAfterDelay(AlertType alertType, float duration, float delay)
	{
		yield return new WaitForSecondsRealtime(delay);
		while (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Paused)
		{
			yield return null;
		}
		ShowAlert(alertType, duration, 0f);
	}

	public void ShowPowerup(PowerupType powerupType, float duration = 2f)
	{
		if (player.actorState == Actor.ActorState.Alive && !Singleton<SettingsManager>.Instance.trailerMode)
		{
			text.color = Color.white;
			SetBackgroundToPowerupColor(powerupType);
			Sprite powerupSprite = GetPowerupSprite(powerupType);
			if (powerupSprite != null)
			{
				icon.gameObject.SetActive(value: true);
				icon.sprite = powerupSprite;
			}
			StopAllCoroutines();
			FollowPlayer();
			text.text = Powerup.GetPowerupNameString(powerupType);
			currentPowerup = powerupType;
			anim.ResetTrigger("Hide");
			anim.SetTrigger("Show");
			if (duration > 0f)
			{
				HideAlert(duration);
			}
		}
	}

	public void HideAlert(float delay = 0f)
	{
		if (currentAlert != AlertType.None || currentPowerup != PowerupType.None)
		{
			StopAllCoroutines();
			StartCoroutine(HideAlertAfterDelay(delay));
		}
	}

	private IEnumerator HideAlertAfterDelay(float delay)
	{
		float t = 0f;
		while (t < delay)
		{
			if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Paused)
			{
				t += Time.unscaledDeltaTime;
			}
			yield return null;
		}
		anim.ResetTrigger("Show");
		anim.SetTrigger("Hide");
		currentAlert = AlertType.None;
		currentPowerup = PowerupType.None;
		yield return new WaitForSecondsRealtime(0.2f);
	}
}
