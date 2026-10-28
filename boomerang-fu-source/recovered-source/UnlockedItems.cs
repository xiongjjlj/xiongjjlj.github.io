using System;
using System.Collections.Generic;

[Serializable]
public class UnlockedItems
{
	public List<UnlockedCharacterHats> unlockedCharacterHats = new List<UnlockedCharacterHats>
	{
		new UnlockedCharacterHats(CharacterType.Avocado),
		new UnlockedCharacterHats(CharacterType.Bacon),
		new UnlockedCharacterHats(CharacterType.Banana),
		new UnlockedCharacterHats(CharacterType.Burger),
		new UnlockedCharacterHats(CharacterType.Bread),
		new UnlockedCharacterHats(CharacterType.Carrot),
		new UnlockedCharacterHats(CharacterType.Coffee),
		new UnlockedCharacterHats(CharacterType.Donut),
		new UnlockedCharacterHats(CharacterType.Eggplant),
		new UnlockedCharacterHats(CharacterType.HotSauce),
		new UnlockedCharacterHats(CharacterType.Milk),
		new UnlockedCharacterHats(CharacterType.PaddlePop),
		new UnlockedCharacterHats(CharacterType.Burger),
		new UnlockedCharacterHats(CharacterType.Pineapple),
		new UnlockedCharacterHats(CharacterType.Sushi),
		new UnlockedCharacterHats(CharacterType.Watermelon)
	};

	public List<CharacterType> unlockedCharacters = new List<CharacterType>
	{
		CharacterType.Avocado,
		CharacterType.Bacon,
		CharacterType.Banana,
		CharacterType.Bread,
		CharacterType.Carrot,
		CharacterType.Coffee,
		CharacterType.Donut,
		CharacterType.Eggplant,
		CharacterType.Milk,
		CharacterType.PaddlePop,
		CharacterType.Sushi,
		CharacterType.Watermelon
	};

	public int matchesPlayedSinceLastCharacterUnlock;

	public int totalMatchesPlayed;

	public int totalProgress;

	public List<int> hatTypesUnlockedSoFar = new List<int>();

	public List<CharacterType> newCharactersToHighlight = new List<CharacterType>();
}
