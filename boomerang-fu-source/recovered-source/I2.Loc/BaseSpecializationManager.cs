using System;
using System.Collections.Generic;
using UnityEngine;

namespace I2.Loc
{
	public class BaseSpecializationManager
	{
		public string[] mSpecializations;

		public Dictionary<string, string> mSpecializationsFallbacks;

		public virtual void InitializeSpecializations()
		{
			mSpecializations = new string[14]
			{
				"Any", "PC", "Touch", "Controller", "VR", "XBox", "PS4", "PS5", "OculusVR", "ViveVR",
				"GearVR", "Android", "IOS", "Switch"
			};
			mSpecializationsFallbacks = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				{ "XBox", "Controller" },
				{ "PS4", "Controller" },
				{ "OculusVR", "VR" },
				{ "ViveVR", "VR" },
				{ "GearVR", "VR" },
				{ "Android", "Touch" },
				{ "IOS", "Touch" }
			};
		}

		public virtual string GetCurrentSpecialization()
		{
			if (mSpecializations == null)
			{
				InitializeSpecializations();
			}
			return "PC";
		}

		private bool IsTouchInputSupported()
		{
			return Input.touchSupported;
		}

		public virtual string GetFallbackSpecialization(string specialization)
		{
			if (mSpecializationsFallbacks == null)
			{
				InitializeSpecializations();
			}
			if (mSpecializationsFallbacks.TryGetValue(specialization, out var value))
			{
				return value;
			}
			return "Any";
		}
	}
}
