using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIPowerupToggle : Toggle
{
	[SerializeField]
	public PowerupType powerupType;

	[SerializeField]
	public TextMeshProUGUI powerupLabel;

	private Image selectionImage;

	protected override void Awake()
	{
		base.Awake();
		selectionImage = base.transform.Find("Background/Selection").GetComponent<Image>();
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		base.transform.DOComplete();
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		OnDeselect(null);
	}

	public override void OnSelect(BaseEventData eventData)
	{
		base.OnSelect(eventData);
		selectionImage.enabled = true;
		selectionImage.color = (base.isOn ? Singleton<ColorManager>.Instance.buttonNormalSelectedColor : Singleton<ColorManager>.Instance.buttonNormalColor);
		if (base.gameObject.activeInHierarchy)
		{
			base.transform.DOComplete();
			base.transform.localScale = Vector3.one;
			base.transform.DOScale(1.1f, 0.16f).From(isRelative: false);
		}
		if (powerupLabel != null)
		{
			powerupLabel.gameObject.SetActive(value: true);
			powerupLabel.text = Powerup.GetPowerupNameString(powerupType);
			powerupLabel.rectTransform.DOComplete();
			powerupLabel.rectTransform.anchoredPosition = new Vector2(powerupLabel.rectTransform.anchoredPosition.x, (base.transform as RectTransform).anchoredPosition.y);
			powerupLabel.rectTransform.DOAnchorPos(new Vector2(-10f, powerupLabel.rectTransform.anchoredPosition.y), 0.2f).SetEase(Ease.OutSine).From(isRelative: false);
		}
		if (Singleton<UIManager>.Instance.UserChangedSelection && eventData != null)
		{
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/nope");
		}
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		base.OnDeselect(eventData);
		selectionImage.enabled = false;
		if (powerupLabel != null)
		{
			powerupLabel.gameObject.SetActive(value: false);
		}
	}

	public override void OnSubmit(BaseEventData eventData)
	{
		base.OnSubmit(eventData);
		if (base.gameObject.activeInHierarchy)
		{
			base.transform.DOComplete();
			base.transform.localScale = Vector3.one;
			base.transform.DOScale(0.7f, 0.2f).From(isRelative: false).SetEase(Ease.OutBack);
		}
		selectionImage.color = (base.isOn ? Singleton<ColorManager>.Instance.buttonNormalSelectedColor : Singleton<ColorManager>.Instance.buttonNormalColor);
		if (base.isOn)
		{
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/on");
		}
		else
		{
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/off");
		}
	}
}
