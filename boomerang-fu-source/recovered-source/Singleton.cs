using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	private static T instance;

	public static T Instance
	{
		get
		{
			if (instance == null)
			{
				instance = (T)Object.FindObjectOfType(typeof(T));
				if (instance == null)
				{
					Debug.LogError("An instance of " + typeof(T)?.ToString() + " is needed in the scene, but there is none.");
				}
			}
			return instance;
		}
	}

	public static bool InstanceExists
	{
		get
		{
			if ((bool)instance)
			{
				return true;
			}
			return false;
		}
	}
}
