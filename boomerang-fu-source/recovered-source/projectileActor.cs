using System;
using UnityEngine;
using UnityEngine.UI;

public class projectileActor : MonoBehaviour
{
	[Serializable]
	public class projectile
	{
		public string name;

		public Rigidbody bombPrefab;

		public bool Torque;

		public float min;

		public float max;

		public float Tor_min;

		public float Tor_max;
	}

	public Transform spawnLocator;

	public projectile[] bombList;

	private string FauxName;

	public Text UiText;

	public int bombType;

	public ParticleSystem muzzleflare;

	public bool swarmMissileLauncher;

	private int projectileSimFire = 1;

	public bool MinorRotate;

	public bool MajorRotate;

	private int seq;

	private void Start()
	{
		UiText.text = bombList[bombType].name.ToString();
		if (swarmMissileLauncher)
		{
			projectileSimFire = 5;
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.A))
		{
			Switch(-1);
		}
		if (Input.GetButtonDown("Fire2") || Input.GetKeyDown(KeyCode.D))
		{
			Switch(1);
		}
		if (Input.GetButtonDown("Fire1"))
		{
			Fire();
		}
	}

	public void Switch(int value)
	{
		bombType += value;
		if (bombType < 0)
		{
			bombType = bombList.Length - 1;
		}
		else if (bombType >= bombList.Length)
		{
			bombType = 0;
		}
		UiText.text = bombList[bombType].name.ToString();
	}

	public void Fire()
	{
		muzzleflare.Play();
		Rigidbody rigidbody = UnityEngine.Object.Instantiate(bombList[bombType].bombPrefab, spawnLocator.position, Quaternion.identity);
		rigidbody.AddForce(spawnLocator.forward * UnityEngine.Random.Range(bombList[bombType].min, bombList[bombType].max));
		if (bombList[bombType].Torque)
		{
			rigidbody.AddTorque(spawnLocator.up * UnityEngine.Random.Range(bombList[bombType].Tor_min, bombList[bombType].Tor_max));
		}
		if (MinorRotate)
		{
			RandomizeRotation();
		}
		if (MajorRotate)
		{
			Major_RandomizeRotation();
		}
	}

	private void RandomizeRotation()
	{
		if (seq == 0)
		{
			seq++;
			base.transform.Rotate(0f, 1f, 0f);
		}
		else if (seq == 1)
		{
			seq++;
			base.transform.Rotate(1f, 1f, 0f);
		}
		else if (seq == 2)
		{
			seq++;
			base.transform.Rotate(1f, -3f, 0f);
		}
		else if (seq == 3)
		{
			seq++;
			base.transform.Rotate(-2f, 1f, 0f);
		}
		else if (seq == 4)
		{
			seq++;
			base.transform.Rotate(1f, 1f, 1f);
		}
		else if (seq == 5)
		{
			seq = 0;
			base.transform.Rotate(-1f, -1f, -1f);
		}
	}

	private void Major_RandomizeRotation()
	{
		if (seq == 0)
		{
			seq++;
			base.transform.Rotate(0f, 5f, 0f);
		}
		else if (seq == 1)
		{
			seq++;
			base.transform.Rotate(0f, -10f, 0f);
		}
		else if (seq == 2)
		{
			seq++;
			base.transform.Rotate(0f, 5f, 0f);
		}
		else if (seq == 3)
		{
			seq++;
			base.transform.Rotate(5f, 0f, 0f);
		}
		else if (seq == 4)
		{
			seq++;
			base.transform.Rotate(-10f, 0f, 0f);
		}
		else if (seq == 5)
		{
			seq = 0;
			base.transform.Rotate(5f, 0f, 0f);
		}
	}
}
