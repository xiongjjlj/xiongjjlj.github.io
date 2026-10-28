using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UICharacterGridCell : MonoBehaviour
{
	public enum SelectionState
	{
		Hidden,
		Deselected,
		Selected,
		Ready
	}

	public enum LockState
	{
		Unlocked,
		Locked
	}

	public int index;

	private Character character;

	public Character altCharacter;

	private bool altCharacterSelected;

	public Color borderColor;

	public Color backgroundColorCharacter;

	public static Color backgroundColorNeutral = new Color(0.85f, 0.85f, 0.85f, 1f);

	private Color lockedColor = new Color(1f, 1f, 1f, 0.2f);

	private Color noBorder = new Color(0.9607f, 0.9411f, 0.9254f, 1f);

	private static float altBackgroundOffset = 14f;

	public SelectionState selectionState = SelectionState.Deselected;

	public List<UILobbyPlayer> lobbyPlayersCurrentlySelecting = new List<UILobbyPlayer>(6);

	public Dictionary<int, UICharacterGridPlayerIcon> playerIconPool = new Dictionary<int, UICharacterGridPlayerIcon>(6);

	public List<UICharacterGridPlayerIcon> activePlayerIcons = new List<UICharacterGridPlayerIcon>(6);

	public LockState lockState;

	public RectTransform container;

	public Image background;

	public Image outline;

	public Image sprite;

	public GameObject padlock;

	public RectTransform altBackground;

	public Image newHint;

	public TextMeshProUGUI newHintText;

	private Sequence selectedTween;

	public static float Selected_Scale = 1.15f;

	public static float Hidden_Scale = 0.5f;

	public Character currentCharacter
	{
		get
		{
			if (altCharacter != null && altCharacterSelected)
			{
				return altCharacter;
			}
			return character;
		}
	}

	public void SetupCharacter(Character character)
	{
		this.character = character;
		sprite.enabled = true;
		outline.enabled = true;
		UIMenuLobby uIMenuLobby = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Lobby) as UIMenuLobby;
		if (uIMenuLobby != null)
		{
			for (int i = 0; i < 6; i++)
			{
				UICharacterGridPlayerIcon uICharacterGridPlayerIcon = Object.Instantiate(uIMenuLobby.characterGridPlayerIconPrefab, container, worldPositionStays: false);
				uICharacterGridPlayerIcon.SetPlayer(i, PlayerType.Human);
				playerIconPool.Add(uICharacterGridPlayerIcon.playerID, uICharacterGridPlayerIcon);
				uICharacterGridPlayerIcon.SetActive(setActive: false);
			}
		}
		CheckAltCharacter();
		CheckLockedState();
	}

	public void CheckAltCharacter()
	{
		if ((altCharacter == null || !Singleton<GameManager>.Instance.IsCharacterUnlocked(altCharacter.characterType)) && altBackground != null)
		{
			altBackground.gameObject.SetActive(value: false);
			newHint.gameObject.SetActive(value: false);
		}
		else
		{
			altBackground.gameObject.SetActive(value: true);
		}
		UpdateColors();
	}

	public void CheckLockedState()
	{
		if (!Singleton<GameManager>.Instance.IsCharacterUnlocked(character.characterType))
		{
			lockState = LockState.Locked;
		}
		if (lockState == LockState.Locked)
		{
			sprite.color = lockedColor;
			padlock.SetActive(value: true);
		}
		else
		{
			sprite.color = Color.white;
			padlock.SetActive(value: false);
		}
	}

	public void UpdateColors()
	{
		sprite.sprite = currentCharacter.lobbySpriteIdle;
		HSBColor hsbColor = HSBColor.FromColor(currentCharacter.Color);
		if (hsbColor.s > 0.25f)
		{
			hsbColor.b = Mathf.Min(hsbColor.b, 0.88f);
		}
		else
		{
			hsbColor.b = Mathf.Min(hsbColor.b, 0.7f);
		}
		borderColor = HSBColor.ToColor(hsbColor);
		hsbColor.b = 0.97f;
		hsbColor.s = Mathf.Min(hsbColor.s, 0.25f);
		backgroundColorCharacter = HSBColor.ToColor(hsbColor);
		outline.enabled = false;
		background.color = backgroundColorNeutral;
		for (int i = 0; i < playerIconPool.Count; i++)
		{
			playerIconPool[i].SetColor(borderColor);
		}
		if (!(altCharacter == null) && Singleton<GameManager>.Instance.IsCharacterUnlocked(altCharacter.characterType))
		{
			newHint.gameObject.SetActive(Singleton<SettingsManager>.Instance.UnlockedItems.newCharactersToHighlight.Contains(altCharacter.characterType));
			if (selectionState == SelectionState.Ready)
			{
				outline.enabled = false;
				background.color = backgroundColorCharacter;
				sprite.sprite = currentCharacter.lobbySpriteHappy;
				sprite.color = new Color(1f, 1f, 1f, 0.75f);
			}
		}
	}

	private void SetTeamCharacter(UILobbyPlayer lobbyPlayer)
	{
		if (lobbyPlayer == null)
		{
			character = null;
		}
		else
		{
			character = lobbyPlayer.character;
		}
		if (character == null)
		{
			outline.enabled = false;
			background.color = backgroundColorNeutral;
			sprite.enabled = false;
			return;
		}
		sprite.enabled = true;
		sprite.sprite = currentCharacter.lobbySpriteIdle;
		sprite.color = Color.white;
		HSBColor hSBColor = HSBColor.FromColor(Singleton<ColorManager>.Instance.teamColors[lobbyPlayer.team]);
		hSBColor.b = 0.97f;
		hSBColor.s = Mathf.Min(hSBColor.s, 0.33f);
		outline.color = noBorder;
		background.color = Singleton<ColorManager>.Instance.GetTeamBackgroundColor(lobbyPlayer.team);
	}

	public void OnSelect(UILobbyPlayer lobbyPlayer)
	{
		if (lockState == LockState.Unlocked)
		{
			if (lobbyPlayersCurrentlySelecting.Count == 0)
			{
				container.DOScale(Selected_Scale, 0.1f).SetEase(Ease.OutBack);
			}
			else
			{
				DOTween.Sequence().Append(container.DOScale(1f, 0.05f).SetEase(Ease.OutQuad)).Append(container.DOScale(Selected_Scale, 0.1f).SetEase(Ease.OutBack));
			}
		}
		selectionState = SelectionState.Selected;
		if (lobbyPlayer == null)
		{
			return;
		}
		if (!lobbyPlayersCurrentlySelecting.Contains(lobbyPlayer))
		{
			lobbyPlayersCurrentlySelecting.Add(lobbyPlayer);
		}
		UICharacterGridPlayerIcon uICharacterGridPlayerIcon = playerIconPool[lobbyPlayer.playerID];
		if (uICharacterGridPlayerIcon != null)
		{
			uICharacterGridPlayerIcon.SetActive(setActive: true);
			if (!activePlayerIcons.Contains(uICharacterGridPlayerIcon))
			{
				activePlayerIcons.Add(uICharacterGridPlayerIcon);
				UpdateAllPlayerIconPositions();
			}
			uICharacterGridPlayerIcon.SetPlayer(lobbyPlayer.playerID, lobbyPlayer.playerType);
			uICharacterGridPlayerIcon.StartBouncing();
		}
		if (lobbyPlayersCurrentlySelecting.Count > 0)
		{
			outline.color = borderColor;
			outline.enabled = true;
		}
		background.color = backgroundColorNeutral;
		sprite.sprite = currentCharacter.lobbySpriteIdle;
	}

	public void OnReady(UILobbyPlayer lobbyPlayer)
	{
		base.gameObject.SetActive(value: true);
		selectionState = SelectionState.Ready;
		DOTween.Sequence().Append(container.DOScale(Selected_Scale * 1.15f, 0.1f).SetEase(Ease.InQuad)).Append(container.DOScale(Selected_Scale, 0.1f).SetEase(Ease.OutQuad));
		if (!(lobbyPlayer == null))
		{
			if (!lobbyPlayersCurrentlySelecting.Contains(lobbyPlayer))
			{
				lobbyPlayersCurrentlySelecting.Add(lobbyPlayer);
			}
			BootAllOtherPlayersExceptMe(lobbyPlayer);
			UpdateAllPlayerIconPositions();
			outline.enabled = false;
			background.color = backgroundColorCharacter;
			sprite.sprite = currentCharacter.lobbySpriteHappy;
			sprite.color = new Color(1f, 1f, 1f, 0.75f);
			playerIconPool[lobbyPlayer.playerID].StopBouncing();
		}
	}

	public void OnDeselect(UILobbyPlayer lobbyPlayer)
	{
		if (lobbyPlayer == null)
		{
			return;
		}
		if (lobbyPlayersCurrentlySelecting.Contains(lobbyPlayer))
		{
			lobbyPlayersCurrentlySelecting.Remove(lobbyPlayer);
		}
		UICharacterGridPlayerIcon uICharacterGridPlayerIcon = playerIconPool[lobbyPlayer.playerID];
		if (uICharacterGridPlayerIcon != null)
		{
			uICharacterGridPlayerIcon.SetActive(setActive: false);
			if (activePlayerIcons.Contains(uICharacterGridPlayerIcon))
			{
				activePlayerIcons.Remove(uICharacterGridPlayerIcon);
				UpdateAllPlayerIconPositions();
			}
		}
		if (lobbyPlayersCurrentlySelecting.Count == 0)
		{
			selectionState = SelectionState.Deselected;
			container.DOScale(1f, 0.15f);
			outline.enabled = false;
		}
		background.color = backgroundColorNeutral;
		sprite.sprite = currentCharacter.lobbySpriteIdle;
		if (lockState == LockState.Locked)
		{
			sprite.color = lockedColor;
		}
		else
		{
			sprite.color = Color.white;
		}
	}

	public void Fill(UILobbyPlayer lobbyPlayer, bool tween = true)
	{
		if (!(lobbyPlayer == null))
		{
			SetTeamCharacter(lobbyPlayer);
			background.enabled = true;
			container.DOComplete();
			if (tween)
			{
				container.localScale = Hidden_Scale * Vector3.one;
				container.DOScale(Vector3.one, 0.125f).SetEase(Ease.OutQuad);
			}
			else
			{
				container.localScale = Vector3.one;
			}
		}
	}

	public void Empty(bool tween = true)
	{
		SetTeamCharacter(null);
		container.DOComplete();
		if (tween)
		{
			container.localScale = Vector3.one;
			container.DOScale(Hidden_Scale, 0.05f).SetEase(Ease.InQuad).OnComplete(HideCell);
		}
		else
		{
			HideCell();
		}
	}

	private void HideCell()
	{
		background.enabled = false;
	}

	public void JiggleInDirection(MoveDirection direction)
	{
		Vector2 vector = Vector2.zero;
		switch (direction)
		{
		case MoveDirection.Left:
			vector = new Vector2(-1f, 0f);
			break;
		case MoveDirection.Right:
			vector = new Vector2(1f, 0f);
			break;
		case MoveDirection.Up:
			vector = new Vector2(0f, 1f);
			break;
		case MoveDirection.Down:
			vector = new Vector2(0f, -1f);
			break;
		}
		DOTween.Sequence().Append(container.DOLocalMove(vector * 8f, 0.05f)).Append(container.DOLocalMove(Vector3.zero, 0.1f));
	}

	public void BootAllOtherPlayersExceptMe(UILobbyPlayer lobbyPlayer)
	{
		UILobbyPlayer[] array = new UILobbyPlayer[lobbyPlayersCurrentlySelecting.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = lobbyPlayersCurrentlySelecting[i];
		}
		for (int j = 0; j < array.Length; j++)
		{
			if (array[j] != lobbyPlayer)
			{
				array[j].SelectClosestAvailableGridCell(this);
			}
		}
	}

	private void UpdateAllPlayerIconPositions()
	{
		for (int i = 0; i < activePlayerIcons.Count; i++)
		{
			if (activePlayerIcons[i].isActive)
			{
				activePlayerIcons[i].SetPositionByIndex(this, i);
			}
		}
	}

	public void OnSelectAltCharacter()
	{
		if (!(altCharacter == null) && Singleton<GameManager>.Instance.IsCharacterUnlocked(altCharacter.characterType))
		{
			altCharacterSelected = !altCharacterSelected;
			if (Singleton<SettingsManager>.Instance.UnlockedItems.newCharactersToHighlight.Contains(altCharacter.characterType))
			{
				Singleton<SettingsManager>.Instance.UnlockedItems.newCharactersToHighlight.Remove(altCharacter.characterType);
			}
			UpdateColors();
			RectTransform component = outline.GetComponent<RectTransform>();
			component.DOComplete();
			component.anchoredPosition = Vector3.one * altBackgroundOffset;
			component.DOAnchorPos(Vector3.zero, 0.2f).SetEase(Ease.OutQuad);
			if (altBackground != null)
			{
				altBackground.DOComplete();
				altBackground.anchoredPosition = Vector3.one * (0f - altBackgroundOffset) * 1.5f;
				altBackground.DOAnchorPos(Vector2.one * altBackgroundOffset, 0.275f);
			}
			if (lobbyPlayersCurrentlySelecting.Count > 0)
			{
				outline.color = borderColor;
				outline.enabled = true;
			}
			background.color = backgroundColorNeutral;
			sprite.sprite = currentCharacter.lobbySpriteIdle;
		}
	}
}
