using UnityEngine;

public class CharacterDefault : Character
{
	public GameObject leftHand;

	public GameObject rightHand;

	public Renderer[] renderers;

	public override bool IsBurnt
	{
		get
		{
			return base.IsBurnt;
		}
		set
		{
			base.IsBurnt = value;
			if (value)
			{
				base.IsVisible = false;
			}
		}
	}

	public override bool IsSquished
	{
		get
		{
			return base.IsSquished;
		}
		set
		{
			base.IsSquished = value;
			if (value)
			{
				base.IsVisible = false;
			}
		}
	}

	public override void UpdateColors()
	{
		base.UpdateColors();
		Color color = Singleton<GameManager>.Instance.characterColors[actor.characterID][actor.team];
		Renderer[] array = renderers;
		foreach (Renderer renderer in array)
		{
			for (int j = 0; j < renderer.materials.Length; j++)
			{
				if (renderer.materials[j].name == "m_disc (Instance)")
				{
					renderer.materials[j].color = color;
					renderer.materials[j].SetColor("_EmissionColor", color * Mathf.LinearToGammaSpace(0.03f));
				}
				else if (renderer.materials[j].name == "m_char (Instance)")
				{
					renderer.materials[j].color = color - new Color(0.2f, 0.2f, 0.2f);
				}
				else if (renderer.materials[j].name == "Sprites-Default (Instance)" || renderer.materials[j].name == "Fade (Instance)")
				{
					renderer.materials[j].color = color;
				}
				else if (renderer.materials[j].name == "Shield (Instance)")
				{
					renderer.materials[j].color = new Color(color.r, color.g, color.b, 0.25f);
				}
			}
		}
		UpdateHeadbandColor();
	}

	private void UpdateHeadbandColor(bool isAI = false)
	{
		Renderer[] array = renderers;
		foreach (Renderer renderer in array)
		{
			for (int j = 0; j < renderer.materials.Length; j++)
			{
				if (renderer.materials[j].name == "m_headband (Instance)")
				{
					if (isAI || !actor.GetComponent<RewiredController>())
					{
						renderer.materials[j].color = new Color(0.7f, 0.7f, 0.7f);
					}
					else if (Singleton<SettingsManager>.Instance.teamMatch)
					{
						renderer.materials[j].color = Singleton<ColorManager>.Instance.teamColors[actor.team];
					}
					else
					{
						renderer.materials[j].color = new Color(0.075f, 0.075f, 0.075f);
					}
				}
			}
		}
	}

	public override void SetMeleeDiscs(int numDiscs)
	{
		switch (numDiscs)
		{
		case 0:
			rightHand.SetActive(value: true);
			leftHand.SetActive(value: true);
			rightDisc.SetActive(value: false);
			leftDisc.SetActive(value: false);
			break;
		case 1:
			rightHand.SetActive(value: false);
			leftHand.SetActive(value: true);
			rightDisc.SetActive(value: true);
			leftDisc.SetActive(value: false);
			break;
		case 2:
			rightHand.SetActive(value: false);
			leftHand.SetActive(value: false);
			rightDisc.SetActive(value: true);
			leftDisc.SetActive(value: true);
			break;
		}
	}

	public override void Slice(Vector3 sliceVector)
	{
		base.Slice(sliceVector);
		base.IsVisible = false;
	}
}
