using DG.Tweening;
using UnityEngine;

public class LevelBoundaryLine : MonoBehaviour
{
	public LineRenderer line;

	public Transform glow;

	private Renderer rend;

	public Color colorNormal;

	public Color colorNear;

	public Color colorOutside;

	public Color colorFlash;

	private const string colorProperty = "_Color";

	private const string textureProperty = "_MainTex";

	private float lineWidth;

	private float padding = 0.33f;

	public Vector3 zOffset = new Vector3(0f, 0f, -0.25f);

	private float scrollSpeed = 0.5f;

	[SerializeField]
	private Vector2 scrollOffset = Vector2.zero;

	private bool isAnimating;

	private void Start()
	{
		lineWidth = line.startWidth;
		rend = line.GetComponent<Renderer>();
	}

	public void SetLineCorners(Vector2[] corners)
	{
		if (corners.Length < 4)
		{
			Debug.Log("SetBoundaryDimensions did not receive enough corners!");
			return;
		}
		line.SetPosition(0, zOffset + new Vector3(padding, padding, 0f) + (Vector3)corners[0]);
		line.SetPosition(1, zOffset + new Vector3(padding, 0f - padding, 0f) + (Vector3)corners[1]);
		line.SetPosition(2, zOffset + new Vector3(0f - padding, 0f - padding, 0f) + (Vector3)corners[2]);
		line.SetPosition(3, zOffset + new Vector3(0f - padding, padding, 0f) + (Vector3)corners[3]);
	}

	public void SetGlowSize(Vector2 position, Vector2 size)
	{
		if (glow.gameObject.activeSelf)
		{
			glow.transform.localScale = new Vector3(size.x - padding, 10f, size.y - padding);
			glow.transform.position = position;
		}
	}

	private void Update()
	{
		scrollOffset = new Vector2((scrollOffset.x - Time.deltaTime * scrollSpeed * (Singleton<LevelManager>.Instance.isLevelBoundaryShrinking ? (-0.5f) : 1f)) % 1f, scrollOffset.y);
		rend.material.SetTextureOffset("_MainTex", scrollOffset);
		if (isAnimating || Singleton<GameManager>.Instance.players.Count <= 0)
		{
			return;
		}
		Color b = colorNormal;
		if (Singleton<LevelManager>.Instance.isLevelBoundaryShrinking)
		{
			b = colorOutside;
		}
		else
		{
			for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
			{
				if (Singleton<GameManager>.Instance.players[i].actorState == Actor.ActorState.Alive)
				{
					if (!Singleton<LevelManager>.Instance.IsInBounds(Singleton<GameManager>.Instance.players[i].transform.position))
					{
						b = colorOutside;
						break;
					}
					if (Singleton<LevelManager>.Instance.IsCloseToBoundaryEdge(Singleton<GameManager>.Instance.players[i].transform.position))
					{
						b = colorNear;
					}
				}
			}
		}
		rend.material.SetColor("_Color", Color.Lerp(rend.material.GetColor("_Color"), b, Time.deltaTime * 5f));
	}

	public void Flash()
	{
		rend.material.SetColor("_Color", colorNear);
		rend.material.DOColor(colorFlash, "_Color", 0.5f).From(isRelative: false).OnComplete(OnFlashComplete);
		isAnimating = true;
		glow.gameObject.SetActive(value: true);
	}

	public void HideGlow()
	{
		glow.gameObject.SetActive(value: false);
	}

	private void OnFlashComplete()
	{
		isAnimating = false;
	}
}
