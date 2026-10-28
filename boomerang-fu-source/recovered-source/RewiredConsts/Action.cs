using Rewired.Dev;

namespace RewiredConsts
{
	public static class Action
	{
		[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Move Horizontal")]
		public const int Move_Horizontal = 0;

		[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Move Vertical")]
		public const int Move_Vertical = 1;

		[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Dash")]
		public const int Dash = 2;

		[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Swipe")]
		public const int Swipe = 3;

		[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Throw")]
		public const int Throw = 4;

		[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Start")]
		public const int Start = 6;

		[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Back")]
		public const int Back = 5;

		[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UIHorizontal")]
		public const int UIHorizontal = 16;

		[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UIVertical")]
		public const int UIVertical = 17;

		[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UISubmit")]
		public const int UISubmit = 18;

		[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UICancel")]
		public const int UICancel = 19;

		[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UISelect")]
		public const int UISelect = 22;

		[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UIRestore")]
		public const int UIRestore = 23;

		[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UITabLeft")]
		public const int UITabLeft = 24;

		[ActionIdFieldInfo(categoryName = "Default", friendlyName = "UITabRight")]
		public const int UITabRight = 25;
	}
}
