using UnityEngine;

public class ColorManager : Singleton<ColorManager>
{
	[Header("Global colors")]
	public Color[] teamColors = new Color[3]
	{
		new Color(0f, 0f, 0f),
		new Color(1f, 0.42f, 0.42f),
		new Color(0.7f, 0.42f, 1f)
	};

	public Color teamOneBackgroundColor;

	public Color teamTwoBackgroundColor;

	[Header("Disc Materials")]
	public Color goldenDiscColor;

	public Color burntDiscColor;

	public Material discMaterial;

	public Material discMaterialMinidisc;

	public Material discMaterialGhost;

	public Material discMaterialGolden;

	[Header("UI")]
	public Color buttonNormalColor;

	public Color buttonNormalSelectedColor;

	public Color buttonNormalSelectedTextColor;

	public Color buttonDarkNormalColor;

	public Color buttonDarkTextColor;

	public Color buttonDarkSelectedColor;

	public Color buttonDarkSelectedTextColor;

	public Color buttonDarkBorderedNormalColor;

	public Color buttonSubmitColor;

	public Color buttonDarkSubmitColor;

	public Color buttonDarkBorderedSubmitColor;

	public Color buttonPopupNormalColor;

	public Color buttonPopupSelectedTextColor;

	public Color buttonLabelTextColor;

	public Color buttonLabelSelectedTextColor;

	public Color buttonTransparentColor;

	[Header("Powerups")]
	public Color powerupColor;

	private MaterialPropertyBlock discPropertyBlock;

	private int discColorPropertyID;

	private int discOccludedColorPropertyID;

	public void SetDiscColor(Renderer renderer, Color targetColor, bool isMiniDisc = false, bool isGoldenDisc = false)
	{
		if (discPropertyBlock == null)
		{
			discPropertyBlock = new MaterialPropertyBlock();
		}
		else
		{
			discPropertyBlock.Clear();
		}
		if (isMiniDisc)
		{
			renderer.material = discMaterialMinidisc;
			targetColor -= new Color(0f, 0f, 0f, 0.5f);
		}
		else if (isGoldenDisc)
		{
			renderer.material = discMaterialGolden;
		}
		else
		{
			renderer.material = discMaterial;
		}
		discPropertyBlock.SetColor(discColorPropertyID, targetColor);
		discPropertyBlock.SetColor(discOccludedColorPropertyID, ColorExt.SetAlpha(targetColor, 0.33f));
		renderer.SetPropertyBlock(discPropertyBlock);
	}

	public Color GetDustColorAtPosition(Vector3 position)
	{
		Color grey = Color.grey;
		if (Singleton<LevelManager>.Instance.level == null)
		{
			return grey;
		}
		if ((bool)Physics2D.OverlapPoint(position, 1 << LayerMask.NameToLayer("WetFloor")))
		{
			grey = Singleton<LevelManager>.Instance.level.waterColor;
		}
		else
		{
			Collider2D collider2D = Physics2D.OverlapCircle(position, 1f, Singleton<LevelManager>.Instance.floorLayers);
			grey = (((bool)collider2D && collider2D.CompareTag("MovingFloor")) ? Singleton<LevelManager>.Instance.level.movingPlatformColor : (collider2D ? Singleton<LevelManager>.Instance.level.dustColor : ((!Singleton<LevelManager>.Instance.level.isCloudLevel) ? Singleton<LevelManager>.Instance.level.waterColor : Singleton<LevelManager>.Instance.level.fogColor)));
		}
		if (Singleton<LevelManager>.Instance.level.isDarkLevel)
		{
			grey *= 0.75f;
		}
		return grey;
	}

	public Color GetTeamBackgroundColor(int team)
	{
		return team switch
		{
			1 => teamOneBackgroundColor, 
			2 => teamTwoBackgroundColor, 
			_ => UICharacterGridCell.backgroundColorNeutral, 
		};
	}

	public Color GetColorForCharacterType(CharacterType characterType)
	{
		Character[] characters = Singleton<GameManager>.Instance.characters;
		foreach (Character character in characters)
		{
			if (character.characterType == characterType)
			{
				return character.Color;
			}
		}
		return Color.magenta;
	}

	private void Start()
	{
		discColorPropertyID = Shader.PropertyToID("_Color");
		discOccludedColorPropertyID = Shader.PropertyToID("_OccludedColor");
	}
}
