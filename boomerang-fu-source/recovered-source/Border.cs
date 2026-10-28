using UnityEngine;

public class Border : MonoBehaviour
{
	private Renderer borderRenderer;

	private float targetAlpha = 0.07f;

	private float currentAlpha = 0.07f;

	private int playersNearBorder;

	private void Start()
	{
		borderRenderer = GetComponent<Renderer>();
	}

	private void Update()
	{
		playersNearBorder = 0;
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			if (!Singleton<LevelManager>.Instance.IsInBounds(Singleton<GameManager>.Instance.players[i].transform.position * 1.33f))
			{
				playersNearBorder++;
			}
		}
		if (playersNearBorder > 0)
		{
			targetAlpha = 0.6f;
		}
		else
		{
			targetAlpha = 0.2f;
		}
		if (currentAlpha != targetAlpha)
		{
			currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, 5f * Time.deltaTime);
			ColorExt.SetAlpha(borderRenderer.material, currentAlpha);
		}
	}
}
