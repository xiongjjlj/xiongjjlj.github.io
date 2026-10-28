using DG.Tweening;
using UnityEngine;

public class UIUnlockedPlayer : UIPlayer
{
	private CanvasGroup canvasGroup;

	private RectTransform rt;

	public void SetupUIPlayer(Character setCharacter, int team, HatType hatType = HatType.None)
	{
		base.SetupUIPlayer(0);
		rt = GetComponent<RectTransform>();
		canvasGroup = GetComponent<CanvasGroup>();
		SetCharacterModel(setCharacter);
		SetTeam(team);
		if (character != null)
		{
			character.actor = null;
			character.equippedHat = hatType;
			character.SetHat();
			base.gameObject.SetActive(value: false);
		}
		if (canvasGroup != null)
		{
			canvasGroup.DOComplete();
			canvasGroup.alpha = 0f;
		}
	}

	public void Show()
	{
		base.gameObject.SetActive(value: true);
		rt.DOComplete();
		rt.localScale = Vector3.one;
		rt.DOScale(Vector3.one * 0.1f, 0.4f).SetEase(Ease.OutBack).From(isRelative: false);
		rt.DOPunchRotation(new Vector3(0f, 0f, Random.Range(4f, 6f)), 0.65f);
		Singleton<UIManager>.Instance.CameraShake(new Vector2(0f, 1f), 10f, 0.15f);
		canvasGroup.DOFade(1f, 0.15f);
		character.SetFacialExpression(FacialExpression.content, 600f);
	}
}
