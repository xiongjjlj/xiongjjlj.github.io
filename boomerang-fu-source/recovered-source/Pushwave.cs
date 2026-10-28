using System.Collections;
using UnityEngine;

public class Pushwave : MonoBehaviour
{
	public float decayTime = 1f;

	public float expandDuration = 0.5f;

	public Color color = new Color(1f, 1f, 1f);

	private float expandTimer;

	private float wavesize;

	public Player discOwner;

	private GameObject appearance;

	private Renderer appearanceRenderer;

	private Player player;

	private void Awake()
	{
		appearance = base.transform.Find("Appearance").gameObject;
		appearanceRenderer = appearance.GetComponent<Renderer>();
		wavesize = GetComponent<CircleCollider2D>().radius * 2f;
		appearanceRenderer.material.color = color;
		StartCoroutine(Decay());
	}

	private void Update()
	{
		if (expandTimer < expandDuration)
		{
			float t = expandTimer / expandDuration;
			float num = Mathf.Lerp(5f, wavesize * 1f, t);
			appearance.transform.localScale = new Vector3(num, num, 1f);
		}
		if (expandTimer < decayTime)
		{
			ColorExt.SetAlpha(appearanceRenderer.material, Mathf.Lerp(0.2f, 0f, Easing.ExpoIn(expandTimer / decayTime)));
		}
		expandTimer += Time.deltaTime;
	}

	private void OnTriggerEnter2D(Collider2D collider)
	{
		if (collider.gameObject.layer == LayerMask.NameToLayer("Player"))
		{
			player = collider.GetComponent<Player>();
			if (!player.IsDashing)
			{
				player.SetKiller(discOwner, Actor.KillerType.Push, 2f);
				Vector2 vector = collider.transform.position - base.transform.position;
				player.StartPushback(vector.normalized * 25f * (3f - vector.magnitude / (wavesize * 0.5f)), 0.5f, 0.25f);
			}
		}
	}

	private IEnumerator Decay()
	{
		yield return new WaitForSeconds(decayTime);
		Singleton<GameManager>.Instance.activeExplosives.Remove(base.gameObject);
		Object.Destroy(base.gameObject);
	}
}
