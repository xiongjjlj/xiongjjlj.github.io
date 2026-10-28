using BoomerangFu.Common.Helpers;
using UnityEngine;
using UnityEngine.UI;

public class UINavigationRefresher : MonoBehaviour
{
	private void Start()
	{
		RefreshNavigation();
	}

	public void RefreshNavigation()
	{
		NavigationHelper.SetupVerticalNavigation(base.gameObject.GetComponentsInChildren<Selectable>(), wrap: false);
	}
}
