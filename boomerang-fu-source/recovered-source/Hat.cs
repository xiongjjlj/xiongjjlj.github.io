using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Hat
{
	public enum HatAttachment
	{
		HatJoint,
		Faceplate
	}

	public HatType hatType;

	public GameObject hatPrefab;

	public Debris3D hatDebrisPrefab;

	public bool isTall;

	public bool hideHair;

	public HatAttachment hatAttachment;

	public List<CharacterType> bannedCharacters;

	public DownloadableContentID dlcID;
}
