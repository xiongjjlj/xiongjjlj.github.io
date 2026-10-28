using UnityEngine;
using UnityEngine.UI;

public class ButtonPromptHold : MonoBehaviour
{
	public Sprite borderButton;

	public Sprite borderKey;

	public Sprite fillButton;

	public Sprite fillKey;

	public Color colorButton;

	public Color colorKey;

	private float targetFill;

	private Image fill;

	public Animator anim;

	private void Start()
	{
		fill = base.transform.Find("Fill").GetComponent<Image>();
	}

	private void OnEnable()
	{
		if (anim != null)
		{
			anim.SetTrigger("Highlighted");
		}
	}

	public void Init(bool isController)
	{
		if (isController)
		{
			GetComponent<Image>().sprite = borderButton;
			GetComponent<Image>().color = colorButton;
			fill.sprite = fillButton;
		}
		else
		{
			GetComponent<Image>().sprite = borderKey;
			GetComponent<Image>().color = colorKey;
			fill.sprite = fillKey;
		}
	}

	private void LateUpdate()
	{
		fill.fillAmount = Mathf.Lerp(fill.fillAmount, targetFill, Time.fixedDeltaTime * 15f);
	}

	public void SetFill(float fillAmt)
	{
		if (targetFill == 0f && fillAmt > 0f && anim != null)
		{
			anim.SetTrigger("Highlighted");
		}
		targetFill = fillAmt;
	}
}
