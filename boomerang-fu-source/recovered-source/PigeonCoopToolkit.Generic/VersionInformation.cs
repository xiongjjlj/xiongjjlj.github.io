using System;

namespace PigeonCoopToolkit.Generic
{
	[Serializable]
	public class VersionInformation
	{
		public string Name;

		public int Major = 1;

		public int Minor;

		public int Patch;

		public VersionInformation(string name, int major, int minor, int patch)
		{
			Name = name;
			Major = major;
			Minor = minor;
			Patch = patch;
		}

		public override string ToString()
		{
			return $"{Name} {Major}.{Minor}.{Patch}";
		}

		public bool Match(VersionInformation other, bool looseMatch)
		{
			if (looseMatch)
			{
				if (other.Name == Name && other.Major == Major)
				{
					return other.Minor == Minor;
				}
				return false;
			}
			if (other.Name == Name && other.Major == Major && other.Minor == Minor)
			{
				return other.Patch == Patch;
			}
			return false;
		}
	}
}
