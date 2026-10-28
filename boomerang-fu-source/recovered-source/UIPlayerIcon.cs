using DG.Tweening;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerIcon : MonoBehaviour
{
	public TextMeshProUGUI text;

	public Image background;

	public RectTransform rt;

	public CanvasGroup canvasGroup;

	public bool isActive;

	public int playerID;

	public PlayerType playerType;

	private Sequence bounceTween;

	public void SetPlayer(int playerID, PlayerType playerType)
	{
		this.playerID = playerID;
		this.playerType = playerType;
		if (playerType == PlayerType.Human)
		{
			if (Singleton<SettingsManager>.Instance.GameOptions.xinput)
			{
				text.text = LocalizationManager.GetTranslation("ui_player_prefix") + (playerID + 1);
			}
			else
			{
				text.text = " ";
			}
		}
		else
		{
			text.text = LocalizationManager.GetTranslation("ui_bot");
		}
	}

	public void SetActive(bool setActive)
	{
		canvasGroup.alpha = (setActive ? 1f : 0f);
		isActive = setActive;
		if (!isActive)
		{
			StopBouncing();
		}
	}

	public void SetAnchoredPosition(Vector2 pos)
	{
		rt.anchoredPosition = pos;
	}

	public void SetColor(Color color)
	{
		if (background != null)
		{
			background.color = color;
		}
	}

	public void StartBouncing()
	{
		StopBouncing();
		bounceTween = DOTween.Sequence();
		bounceTween.Append(rt.DOScale(1.1f, 0.4f).SetEase(Ease.InOutSine)).Append(rt.DOScale(1f, 0.4f).SetEase(Ease.InOutSine)).SetLoops(-1);
	}

	private void OnDisable()
	{
		StopBouncing();
	}

	public void StopBouncing()
	{
		if (bounceTween != null)
		{
			bounceTween.Kill();
		}
		rt.localScale = Vector3.one;
	}
}
