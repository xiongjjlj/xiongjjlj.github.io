using UnityEngine.UI;

namespace BoomerangFu.Common.Helpers
{
	public static class NavigationHelper
	{
		public static void SetupVerticalNavigation(Selectable[] selectables, bool wrap)
		{
			if (selectables.Length > 1)
			{
				selectables[0].navigation = new Navigation
				{
					selectOnUp = (wrap ? selectables[^1] : null),
					selectOnDown = selectables[1],
					selectOnLeft = selectables[0].navigation.selectOnLeft,
					selectOnRight = selectables[0].navigation.selectOnRight,
					mode = Navigation.Mode.Explicit
				};
				for (int i = 1; i < selectables.Length - 1; i++)
				{
					selectables[i].navigation = new Navigation
					{
						selectOnUp = selectables[i - 1],
						selectOnDown = selectables[i + 1],
						selectOnLeft = selectables[i].navigation.selectOnLeft,
						selectOnRight = selectables[i].navigation.selectOnRight,
						mode = Navigation.Mode.Explicit
					};
				}
				selectables[^1].navigation = new Navigation
				{
					selectOnUp = selectables[^2],
					selectOnDown = (wrap ? selectables[0] : null),
					selectOnLeft = selectables[^1].navigation.selectOnLeft,
					selectOnRight = selectables[^1].navigation.selectOnRight,
					mode = Navigation.Mode.Explicit
				};
			}
		}
	}
}
