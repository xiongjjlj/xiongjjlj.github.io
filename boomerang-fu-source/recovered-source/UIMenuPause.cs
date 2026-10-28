using DG.Tweening;
using UnityEngine;

public class UIMenuPause : UIMenuBase
{
	private CanvasGroup canvasGroup;

	private void Awake()
	{
		canvasGroup = GetComponent<CanvasGroup>();
	}

	public override void Init()
	{
		canvasGroup.alpha = 0f;
		canvasGroup.DOFade(1f, 0.15f);
		Singleton<UIManager>.Instance.ZoomFrom(1.111f, 0.15f);
		Singleton<Camera3D>.Instance.LerpFOVTo(17.5f, 0.15f);
		base.Init();
	}

	public void UIResponse_Continue()
	{
		Singleton<UIManager>.Instance.PopMenu();
		Singleton<UIManager>.Instance.Unpause();
		Singleton<Camera3D>.Instance.ResetFOV(0.15f);
	}

	public void UIResponse_Quit()
	{
		Singleton<UIManager>.Instance.QuitMatch();
	}
}
