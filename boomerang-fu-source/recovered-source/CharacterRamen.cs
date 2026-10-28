using UnityEngine;

public class CharacterRamen : CharacterCoffee
{
	public GameObject[] hair;

	public override bool IsSliced
	{
		get
		{
			return base.IsSliced;
		}
		set
		{
			base.IsSliced = value;
			for (int i = 0; i < bodyNormal.Length; i++)
			{
				bodyNormal[i].SetActive(!value);
			}
			for (int j = 0; j < bodySliced.Length; j++)
			{
				bodySliced[j].SetActive(value);
			}
			if (!value && ShouldCurrentHatHideHair())
			{
				ShowCurrentHat();
			}
		}
	}

	public override void AddHat(HatType targetHatType)
	{
		base.AddHat(targetHatType);
		for (int i = 0; i < hair.Length; i++)
		{
			hair[i].SetActive(!ShouldCurrentHatHideHair());
		}
	}

	public override void RemoveCurrentHat()
	{
		base.RemoveCurrentHat();
		for (int i = 0; i < hair.Length; i++)
		{
			hair[i].SetActive(value: true);
		}
	}

	public override void ShowCurrentHat()
	{
		base.ShowCurrentHat();
		for (int i = 0; i < hair.Length; i++)
		{
			hair[i].SetActive(!ShouldCurrentHatHideHair());
		}
	}

	private bool ShouldCurrentHatHideHair()
	{
		if (_currentHat == null || hair == null)
		{
			return false;
		}
		if (_currentHat.hatType == HatType.Crown || _currentHat.hatType == HatType.None)
		{
			return false;
		}
		if (_currentHat.hatType == HatType.Fork || _currentHat.hatType == HatType.Antlers || _currentHat.hatType == HatType.CatEars)
		{
			return true;
		}
		if (_currentHat.hatAttachment == Hat.HatAttachment.HatJoint && _currentHat.hideHair)
		{
			return true;
		}
		return false;
	}

	protected override void PrintSpillDecals()
	{
		decalOffset = -0.24f;
		base.PrintSpillDecals();
	}
}
