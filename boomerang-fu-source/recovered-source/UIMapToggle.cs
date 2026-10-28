using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIMapToggle : Toggle
{
	[SerializeField]
	public LevelAsset levelAsset;

	private Image selectionImage;

	private Image mapBackground;

	private Image mapForeground;

	private GameObject lockIcon;

	public bool IsUnlocked => Singleton<GameManager>.Instance.IsDLCUnlocked(levelAsset.requiredDLC);

	public void SetLevelAsset(LevelAsset levelAsset)
	{
		this.levelAsset = levelAsset;
		selectionImage = base.transform.Find("Background/Selection").GetComponent<Image>();
		mapBackground = base.transform.Find("Background").GetComponent<Image>();
		mapForeground = base.transform.Find("Background/Graphic").GetComponent<Image>();
		lockIcon = base.transform.Find("Background/Lock").gameObject;
		if (levelAsset != null && levelAsset.mapSprite != null)
		{
			mapBackground.sprite = levelAsset.mapSprite;
			mapForeground.sprite = levelAsset.mapSprite;
		}
		if (levelAsset != null && Singleton<GameManager>.InstanceExists)
		{
			lockIcon.SetActive(!IsUnlocked);
			if (!IsUnlocked)
			{
				mapBackground.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
			}
		}
	}

	protected override void Start()
	{
		base.Start();
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		base.transform.DOComplete();
		base.transform.localPosition = new Vector3(base.transform.localPosition.x, base.transform.localPosition.y, 0f);
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
		selectionImage.color = Singleton<ColorManager>.Instance.buttonNormalSelectedColor;
		if (base.gameObject.activeInHierarchy)
		{
			base.transform.DOComplete();
			base.transform.localScale = Vector3.one;
			base.transform.DOScale(1.1f, 0.16f).From(isRelative: false);
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
	}

	public override void OnSubmit(BaseEventData eventData)
	{
		if (!IsUnlocked)
		{
			if (base.gameObject.activeInHierarchy)
			{
				Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/shake");
				base.transform.DOComplete();
				base.transform.DOPunchPosition(new Vector3(-15f, 0f, 0f), 0.4f, 15);
			}
			return;
		}
		base.OnSubmit(eventData);
		if (base.gameObject.activeInHierarchy)
		{
			base.transform.DOComplete();
			base.transform.localScale = Vector3.one;
			base.transform.DOScale(0.7f, 0.2f).From(isRelative: false).SetEase(Ease.OutBack);
		}
		Image obj = selectionImage;
		Color color = (selectionImage.color = Singleton<ColorManager>.Instance.buttonNormalSelectedColor);
		obj.color = color;
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
