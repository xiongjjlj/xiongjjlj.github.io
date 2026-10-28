using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour, ISelectHandler, IEventSystemHandler, IDeselectHandler, ISubmitHandler
{
	public Color textColorDeselected;

	public Color textColorSelected;

	public GameObject helperText;

	public Animator anim;

	private TextMeshProUGUI text;

	private Image background;

	public Animator highlightAnim;

	private void Start()
	{
		highlightAnim = GetComponent<Animator>();
		background = GetComponent<Image>();
		text = base.transform.Find("Text").GetComponent<TextMeshProUGUI>();
		background.enabled = false;
		text.color = textColorDeselected;
		if (anim != null)
		{
			anim.SetBool("Selected", value: false);
		}
		if (helperText != null)
		{
			helperText.SetActive(value: false);
		}
	}

	public void OnSelect(BaseEventData eventData)
	{
		background.enabled = true;
		text.color = textColorSelected;
		if (anim != null)
		{
			anim.SetBool("Selected", value: true);
		}
		if (helperText != null)
		{
			helperText.SetActive(value: true);
		}
		if (highlightAnim != null)
		{
			highlightAnim.SetTrigger("Highlighted");
		}
	}

	public void OnDeselect(BaseEventData eventData)
	{
		background.enabled = false;
		text.color = textColorDeselected;
		if (anim != null)
		{
			anim.SetBool("Selected", value: false);
		}
		if (helperText != null)
		{
			helperText.SetActive(value: false);
		}
	}

	public void OnSubmit(BaseEventData eventData)
	{
	}
}
