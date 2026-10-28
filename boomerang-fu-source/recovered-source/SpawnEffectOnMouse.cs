using System;
using UnityEngine;
using UnityEngine.UI;

public class SpawnEffectOnMouse : MonoBehaviour
{
	[Serializable]
	public class chainEffect
	{
		[NonSerialized]
		public int firstvariableisinvisible;

		public string name;

		public bool isPlayed;

		public float Yoffset;

		public bool RotateRandomizer;

		public GameObject Effect;
	}

	public chainEffect[] chainEffectList;

	public int activevariation;

	public Text UIText;

	private Ray ray;

	private RaycastHit hit;

	private void Start()
	{
		CheckName();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.D) || Input.GetButtonDown("Fire2"))
		{
			NextEffect(increase: true);
		}
		if (Input.GetKeyDown(KeyCode.A))
		{
			NextEffect(increase: false);
		}
		ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out hit) && Input.GetButtonDown("Fire1"))
		{
			UnityEngine.Object.Instantiate(chainEffectList[activevariation].Effect, new Vector3(hit.point.x, hit.point.y + chainEffectList[activevariation].Yoffset, hit.point.z), Quaternion.FromToRotation(Vector3.up, hit.normal));
		}
	}

	private void NextEffect(bool increase)
	{
		if (increase)
		{
			activevariation++;
			if (activevariation >= chainEffectList.Length)
			{
				activevariation = 0;
			}
		}
		if (!increase)
		{
			activevariation--;
			if (activevariation < 0)
			{
				activevariation = chainEffectList.Length - 1;
			}
		}
		CheckName();
	}

	private void CheckName()
	{
		UIText.text = "#" + activevariation + " " + chainEffectList[activevariation].name;
	}
}
