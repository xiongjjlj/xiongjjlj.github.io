using UnityEngine;

public class UICharacterGridPlayerIcon : UIPlayerIcon
{
	private Vector2 cellSize = new Vector2(100f, 100f);

	public void SetPositionByIndex(UICharacterGridCell targetCell, int index)
	{
		Vector2 vector = new Vector2(0f - cellSize.x, cellSize.y) / 2f;
		SetAnchoredPosition(vector + Vector2.Scale(b: new Vector2((float)index % 3f, (0f - Mathf.Floor(index / 3)) * 2f), a: cellSize) * 0.5f);
	}
}
