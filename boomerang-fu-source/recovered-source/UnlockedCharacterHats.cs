using System;
using System.Collections.Generic;

[Serializable]
public class UnlockedCharacterHats
{
	public CharacterType characterType;

	public List<int> unlockedHats = new List<int>(1) { 0 };

	public HatType lastSelectedHat;

	public UnlockedCharacterHats(CharacterType characterType)
	{
		this.characterType = characterType;
	}
}
