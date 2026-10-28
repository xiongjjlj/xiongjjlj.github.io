using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIDotNavigation : MonoBehaviour
{
	public Image[] dots;

	public Image cross;

	private static float dotMargin = 30f;

	private static float moveDur = 0.15f;

	private static float halfAlpha = 0.4f;

	private static float quarterAlpha = 0.2f;

	private static Ease ease = Ease.OutSine;

	[SerializeField]
	private int index;

	[SerializeField]
	private int numDots;

	public Sprite dot;

	public Sprite empty;

	private void Start()
	{
		cross.color = ColorExt.SetAlpha(cross.color, 0f);
	}

	public void Show(int numDots, int index)
	{
		this.numDots = numDots;
		this.index = index;
		base.gameObject.SetActive(value: true);
		if (numDots <= 1)
		{
			for (int i = 0; i < dots.Length; i++)
			{
				dots[i].color = ColorExt.SetAlpha(dots[i].color, 0f);
			}
		}
		else
		{
			InitDots();
		}
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
	}

	public void FlashCross()
	{
		cross.DOComplete();
		cross.color = ColorExt.SetAlpha(cross.color, 0f);
		DOTween.Sequence().Append(cross.DOFade(1f, 0.1f)).Append(cross.DOFade(0f, 0.1f));
	}

	private void InitDots()
	{
		dots[0].color = ColorExt.SetAlpha(dots[0].color, 0f);
		dots[1].color = ColorExt.SetAlpha(dots[1].color, (index > 1) ? quarterAlpha : 0f);
		dots[2].color = ColorExt.SetAlpha(dots[2].color, (index > 0) ? halfAlpha : 0f);
		dots[3].color = ColorExt.SetAlpha(dots[3].color, 1f);
		dots[4].color = ColorExt.SetAlpha(dots[4].color, (index < numDots - 1) ? halfAlpha : 0f);
		dots[5].color = ColorExt.SetAlpha(dots[5].color, (index < numDots - 2) ? quarterAlpha : 0f);
		dots[6].color = ColorExt.SetAlpha(dots[6].color, 0f);
		dots[0].rectTransform.localPosition = new Vector2((0f - dotMargin) * 3f, 0f);
		dots[1].rectTransform.localPosition = new Vector2((0f - dotMargin) * 2f, 0f);
		dots[2].rectTransform.localPosition = new Vector2(0f - dotMargin, 0f);
		dots[3].rectTransform.localPosition = new Vector2(0f, 0f);
		dots[4].rectTransform.localPosition = new Vector2(dotMargin, 0f);
		dots[5].rectTransform.localPosition = new Vector2(dotMargin * 2f, 0f);
		dots[6].rectTransform.localPosition = new Vector2(dotMargin * 3f, 0f);
		dots[0].sprite = dot;
		dots[1].sprite = ((index == 2) ? empty : dot);
		dots[2].sprite = ((index == 1) ? empty : dot);
		dots[3].sprite = ((index == 0) ? empty : dot);
	}

	public void MoveLeft()
	{
		CompleteTweens();
		InitDots();
		Mathf.Clamp(index--, 0, numDots - 1);
		dots[0].DOFade((index > 1) ? quarterAlpha : 0f, moveDur).SetEase(ease);
		dots[1].DOFade((index > 0) ? halfAlpha : 0f, moveDur).SetEase(ease);
		dots[2].DOFade(1f, moveDur).SetEase(ease);
		dots[3].DOFade((index < numDots - 1) ? halfAlpha : 0f, moveDur).SetEase(ease);
		dots[4].DOFade((index < numDots - 2) ? quarterAlpha : 0f, moveDur).SetEase(ease);
		dots[5].DOFade(0f, moveDur).SetEase(ease);
		dots[0].rectTransform.DOLocalMoveX((0f - dotMargin) * 2f, moveDur).SetEase(ease);
		dots[1].rectTransform.DOLocalMoveX(0f - dotMargin, moveDur).SetEase(ease);
		dots[2].rectTransform.DOLocalMoveX(0f, moveDur).SetEase(ease);
		dots[3].rectTransform.DOLocalMoveX(dotMargin, moveDur).SetEase(ease);
		dots[4].rectTransform.DOLocalMoveX(dotMargin * 2f, moveDur).SetEase(ease);
		dots[5].rectTransform.DOLocalMoveX(dotMargin * 3f, moveDur).SetEase(ease);
		dots[0].sprite = ((index == 2) ? empty : dot);
		dots[1].sprite = ((index == 1) ? empty : dot);
		dots[2].sprite = ((index == 0) ? empty : dot);
		dots[3].sprite = dot;
	}

	public void MoveRight()
	{
		CompleteTweens();
		InitDots();
		Mathf.Clamp(index++, 0, numDots - 1);
		dots[1].DOFade(0f, moveDur).SetEase(ease);
		dots[2].DOFade((index > 1) ? quarterAlpha : 0f, moveDur).SetEase(ease);
		dots[3].DOFade((index > 0) ? halfAlpha : 0f, moveDur).SetEase(ease);
		dots[4].DOFade(1f, moveDur).SetEase(ease);
		dots[5].DOFade((index < numDots - 1) ? halfAlpha : 0f, moveDur).SetEase(ease);
		dots[6].DOFade((index < numDots - 2) ? quarterAlpha : 0f, moveDur).SetEase(ease);
		dots[1].rectTransform.DOLocalMoveX((0f - dotMargin) * 3f, moveDur).SetEase(ease);
		dots[2].rectTransform.DOLocalMoveX((0f - dotMargin) * 2f, moveDur).SetEase(ease);
		dots[3].rectTransform.DOLocalMoveX(0f - dotMargin, moveDur).SetEase(ease);
		dots[4].rectTransform.DOLocalMoveX(0f, moveDur).SetEase(ease);
		dots[5].rectTransform.DOLocalMoveX(dotMargin, moveDur).SetEase(ease);
		dots[6].rectTransform.DOLocalMoveX(dotMargin * 2f, moveDur).SetEase(ease);
	}

	public void JumpToStart()
	{
		index = 0;
		CompleteTweens();
		InitDots();
	}

	public void JumpToEnd(int endIndex)
	{
		index = endIndex;
		CompleteTweens();
		InitDots();
	}

	private void CompleteTweens()
	{
		for (int i = 0; i < dots.Length; i++)
		{
			dots[i].DOComplete();
			dots[i].rectTransform.DOComplete();
		}
	}
}
