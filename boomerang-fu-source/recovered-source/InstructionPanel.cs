using System.Collections;
using DG.Tweening;
using Rewired;
using UnityEngine;

public class InstructionPanel : MonoBehaviour
{
	public GameObject iconKeyboard;

	public GameObject iconController;

	public GameObject moveKeyboard;

	public GameObject moveController;

	private CanvasGroup canvasGroup;

	private Rewired.Player rewiredPlayer;

	private void Awake()
	{
		canvasGroup = GetComponent<CanvasGroup>();
		canvasGroup.alpha = 0f;
		base.gameObject.SetActive(value: false);
	}

	public void Show(Rewired.Player rewiredPlayer, float delay = 0f)
	{
		if (rewiredPlayer == null)
		{
			return;
		}
		this.rewiredPlayer = rewiredPlayer;
		base.gameObject.SetActive(value: true);
		canvasGroup.alpha = 0f;
		if (Singleton<GameManager>.Instance.DoesPlayerHaveValidJoystick(rewiredPlayer))
		{
			moveKeyboard.SetActive(value: false);
			iconKeyboard.SetActive(value: false);
			moveController.SetActive(value: true);
			iconController.SetActive(value: true);
			ButtonPrompt[] componentsInChildren = base.transform.GetComponentsInChildren<ButtonPrompt>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Init(rewiredPlayer);
			}
		}
		else
		{
			moveKeyboard.SetActive(value: true);
			iconKeyboard.SetActive(value: true);
			moveController.SetActive(value: false);
			iconController.SetActive(value: false);
			ButtonPrompt[] componentsInChildren = base.transform.GetComponentsInChildren<ButtonPrompt>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Init(rewiredPlayer);
			}
		}
		StartCoroutine(ShowAnimated(delay + 0.1f));
	}

	private IEnumerator ShowAnimated(float delay)
	{
		yield return new WaitForSecondsRealtime(delay);
		canvasGroup.alpha = 0f;
		canvasGroup.DOFade(1f, 0.16f);
		base.transform.localScale = Vector3.one;
		base.transform.DOScale(0.8f, 0.16f).SetEase(Ease.OutBack).From(isRelative: false);
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
	}
}
