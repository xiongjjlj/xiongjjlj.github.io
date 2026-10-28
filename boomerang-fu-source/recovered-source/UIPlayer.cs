using Rewired;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayer : MonoBehaviour
{
	public int playerID;

	public Rewired.Player rewiredPlayer;

	public int team;

	public PlayerType playerType;

	[SerializeField]
	protected Image circle;

	protected Transform characterOffset;

	protected bool layoutUpdateScheduled;

	private static Vector3 modelEulerAngles = new Vector3(80f, 0f, 90f);

	public Character character;

	public virtual void SetupUIPlayer(int playerID)
	{
		this.playerID = playerID;
		circle = base.transform.Find("Circle").GetComponent<Image>();
		characterOffset = circle.transform.Find("CharacterOffset");
	}

	public virtual void Reset()
	{
		SetTeam(0);
	}

	protected virtual void Disable()
	{
		base.gameObject.SetActive(value: false);
	}

	public virtual void SetTeam(int team)
	{
		this.team = team;
		if (character != null)
		{
			(character as CharacterPlayer).SetTeam(team);
		}
	}

	protected virtual void SetCharacterModel(Character newCharacter)
	{
		if (character != null && character != newCharacter)
		{
			character.Recycle();
		}
		if (newCharacter == null)
		{
			character = null;
		}
		if (character != newCharacter)
		{
			character = newCharacter.Spawn(characterOffset, Vector3.zero, Quaternion.Euler(modelEulerAngles));
			if (Singleton<SettingsManager>.Instance.GameOptions.botsWearHats || playerType == PlayerType.Human)
			{
				character.equippedHat = Singleton<GameManager>.Instance.GetLastSelectedHatForCharacter(character.characterType);
			}
			character.SetHat();
			if (character.characterMode == Character.CharacterMode.Gameplay)
			{
				character.InitCharacter(Character.CharacterMode.UI);
			}
			character.UpdateUIScaleAndPosition(circle.rectTransform.rect.width, 0.25f);
		}
	}

	private void LateUpdate()
	{
		if (layoutUpdateScheduled)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform.parent.GetComponent<RectTransform>());
			layoutUpdateScheduled = false;
			UpdateLayout();
		}
	}

	protected void OnEnable()
	{
		ScheduleLayoutUpdateForAllPlayers();
	}

	protected void OnDisable()
	{
		ScheduleLayoutUpdateForAllPlayers();
	}

	public void ScheduleLayoutUpdate()
	{
		layoutUpdateScheduled = true;
	}

	protected virtual void ScheduleLayoutUpdateForAllPlayers()
	{
	}

	protected virtual void UpdateLayout()
	{
		if (character != null)
		{
			character.UpdateUIScaleAndPosition(circle.rectTransform.rect.width, 0.25f);
		}
	}
}
