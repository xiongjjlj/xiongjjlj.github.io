using System.Collections;
using DG.Tweening;
using UnityEngine;

public class InteractiveObstacle : MonoBehaviour
{
	public enum ObstacleMaterial
	{
		Stone,
		Wood,
		Bamboo,
		Tree,
		Shrub
	}

	public Player.DisguiseType disguiseType;

	public ObstacleMaterial obstacleMaterial;

	private Renderer[] renderers;

	private IEnumerator flashRedRoutine;

	private const float flashRedDur = 0.4f;

	protected virtual void Start()
	{
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
		{
			renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
		}
	}

	public virtual void Interact(Vector2 force)
	{
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek && disguiseType != Player.DisguiseType.None && renderers.Length != 0)
		{
			Renderer[] array = renderers;
			foreach (Renderer obj in array)
			{
				obj.material.SetColor("_AddColor", Color.red);
				obj.material.DOColor(Color.black, "_AddColor", 0.4f).SetEase(Ease.InOutQuad);
			}
		}
	}
}
