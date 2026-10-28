using System.Collections;
using UnityEngine;

public class PowerupBar : MonoBehaviour
{
	public Player player;

	public PowerupIcon[] powerupIcons;

	public int iconsShowing;

	private bool _isDisplayed;

	private RectTransform rectTransform;

	private RectTransform parentRect;

	private Canvas canvas;

	public bool IsDisplayed
	{
		get
		{
			return _isDisplayed;
		}
		set
		{
			if (canvas != null)
			{
				canvas.enabled = value;
			}
			_isDisplayed = value;
		}
	}

	private void Start()
	{
		rectTransform = GetComponent<RectTransform>();
		parentRect = base.transform.parent.GetComponent<RectTransform>();
		canvas = GetComponent<Canvas>();
	}

	public void Init(Player player, bool addShield = false)
	{
		this.player = player;
		PowerupType powerupType = player.activePowerup;
		if (addShield)
		{
			powerupType |= PowerupType.Shield;
		}
		SetupIcons(powerupType);
		rectTransform.localScale = Vector3.one;
		IsDisplayed = true;
	}

	private void Update()
	{
		if (IsDisplayed && player != null)
		{
			Vector2 screenPoint = Singleton<Camera3D>.Instance.gameCamera.WorldToScreenPoint(player.transform.position);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, Singleton<Camera3D>.Instance.gameCamera, out var localPoint);
			base.transform.localPosition = localPoint;
		}
	}

	public int GetNumIcons(PowerupType activePowerups)
	{
		int num = 0;
		for (int i = 0; i < powerupIcons.Length; i++)
		{
			if (activePowerups.HasPowerup(powerupIcons[i].powerupType))
			{
				num++;
			}
		}
		return num;
	}

	private void SetupIcons(PowerupType activePowerups)
	{
		if (activePowerups.HasPowerup(PowerupType.Shield))
		{
			Debug.Log("Show shield icon", base.gameObject);
		}
		iconsShowing = 0;
		for (int i = 0; i < powerupIcons.Length; i++)
		{
			if (activePowerups.HasPowerup(powerupIcons[i].powerupType))
			{
				powerupIcons[i].gameObject.SetActive(value: true);
				iconsShowing++;
			}
			else
			{
				powerupIcons[i].gameObject.SetActive(value: false);
			}
		}
		StartCoroutine(ShowIcons());
	}

	private IEnumerator ShowIcons()
	{
		for (int i = 0; i < powerupIcons.Length; i++)
		{
			if (powerupIcons[i].gameObject.activeSelf)
			{
				powerupIcons[i].Show();
				yield return new WaitForSecondsRealtime(0.15f);
			}
		}
	}

	public void HideBar(float delay = 0f)
	{
		if (base.gameObject.activeSelf)
		{
			StartCoroutine(HideIcons(delay));
		}
	}

	private IEnumerator HideIcons(float delay)
	{
		if (delay > 0f)
		{
			yield return new WaitForSecondsRealtime(delay);
		}
		for (int i = 0; i < powerupIcons.Length; i++)
		{
			if (powerupIcons[i].gameObject.activeSelf)
			{
				powerupIcons[i].Hide();
			}
		}
		yield return new WaitForSecondsRealtime(1f);
		IsDisplayed = false;
	}
}
