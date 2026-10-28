using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIButtonAnnouncementStyle : UIButtonStyle
{
	[Serializable]
	public class HideEvent : UnityEvent
	{
	}

	public RectTransform wrapper;

	public Image outlineImage;

	public Image buttonImage;

	public GameObject hideAnnouncementsPrompt;

	public GameObject characterContainer;

	public RectTransform[] characters;

	public AnimationCurve characterAnimCurve;

	public Color announceColor;

	private List<Vector2> characterInitPositions = new List<Vector2>();

	[SerializeField]
	private HideEvent onHideEvent = new HideEvent();

	public HideEvent OnHideEvent
	{
		get
		{
			return onHideEvent;
		}
		set
		{
			onHideEvent = value;
		}
	}

	public override void Awake()
	{
		base.Awake();
		for (int i = 0; i < characters.Length; i++)
		{
			characterInitPositions.Add(characters[i].localPosition);
		}
		outlineImage.color = Singleton<ColorManager>.Instance.buttonDarkBorderedNormalColor;
		buttonImage.color = outlineImage.color;
		hideAnnouncementsPrompt.SetActive(value: false);
	}

	protected override void OnEnable()
	{
		base.OnEnable();
	}

	private void Update()
	{
		if (isSelected && Singleton<UIManager>.Instance.GetButtonDownFromEventSystem("UIRestore"))
		{
			onHideEvent.Invoke();
		}
	}

	public override void OnSelect(BaseEventData eventData)
	{
		base.OnSelect(eventData);
		outlineImage.color = Singleton<ColorManager>.Instance.buttonDarkSelectedColor;
		buttonImage.color = outlineImage.color;
		hideAnnouncementsPrompt.SetActive(value: true);
		characterContainer.SetActive(value: true);
		for (int i = 0; i < characters.Length; i++)
		{
			characters[i].localPosition = characterInitPositions[i] - new Vector2(0f, 120f);
			characters[i].DOLocalMove(characterInitPositions[i], 0.3f).SetDelay((float)i * 0.1f).SetEase(characterAnimCurve);
		}
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		base.OnDeselect(eventData);
		outlineImage.color = announceColor;
		buttonImage.color = outlineImage.color;
		hideAnnouncementsPrompt.SetActive(value: false);
		characterContainer.SetActive(value: false);
	}

	public override void UpdateButtonColors()
	{
		if (selectable == null || text == null || !Application.isPlaying || !Singleton<ColorManager>.InstanceExists)
		{
			return;
		}
		if (isSelected)
		{
			if (text != null)
			{
				text.color = Singleton<ColorManager>.Instance.buttonDarkSelectedTextColor;
			}
			selectable.image.color = Singleton<ColorManager>.Instance.buttonDarkSelectedColor;
		}
		else
		{
			if (text != null)
			{
				text.color = Singleton<ColorManager>.Instance.buttonDarkTextColor;
			}
			selectable.image.color = announceColor;
		}
	}
}
