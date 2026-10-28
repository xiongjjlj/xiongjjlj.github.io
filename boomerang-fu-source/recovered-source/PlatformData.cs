using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

public class PlatformData
{
	public event EventHandler<EventArgs> OnDataSaved = delegate
	{
	};

	public event EventHandler<SaveDataEventArgs> OnModifiersDataLoaded = delegate
	{
	};

	public event EventHandler<SaveDataEventArgs> OnSettingsDataLoaded = delegate
	{
	};

	public virtual string GetPath(string filename)
	{
		return Path.Combine(Application.persistentDataPath, filename);
	}

	public virtual bool FileExists(string filename)
	{
		if (File.Exists(GetPath(filename)))
		{
			return true;
		}
		return false;
	}

	public void DeleteFile(string filename)
	{
		string path = GetPath(filename);
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}

	public virtual void LoadJSON<T>(string filename, Action<T> OnDataLoaded) where T : class
	{
		string path = GetPath(filename);
		if (File.Exists(path))
		{
			byte[] array = File.ReadAllBytes(path);
			if (array == null || array.Length == 0)
			{
				Debug.Log("Load failed: bytes could not be read from disk");
				OnDataLoaded(null);
			}
			string text = Encoding.ASCII.GetString(array);
			if (string.IsNullOrEmpty(text))
			{
				Debug.Log("Load failed: json data could not be read from bytes");
				OnDataLoaded(null);
			}
			try
			{
				T obj = JsonUtility.FromJson<T>(text);
				Debug.Log("Loaded json file from: " + path);
				OnDataLoaded(obj);
				return;
			}
			catch (ArgumentException)
			{
				Debug.LogError(filename + " JSON was invalid! Loading default data instead");
				OnDataLoaded(null);
				return;
			}
		}
		Debug.Log("Load failed: " + path + " not found");
		OnDataLoaded(null);
	}

	public virtual void SaveJSON<T>(string filename, T data, Action<bool> OnDataSaved) where T : class
	{
		if (data == null)
		{
			Debug.Log("Save failed, data is missing");
			OnDataSaved?.Invoke(obj: false);
			return;
		}
		string path = GetPath(filename);
		string text = JsonUtility.ToJson(data);
		if (string.IsNullOrEmpty(text))
		{
			Debug.Log("Save failed: json data is missing");
			OnDataSaved?.Invoke(obj: false);
			return;
		}
		byte[] bytes = Encoding.ASCII.GetBytes(text);
		if (bytes == null || bytes.Length == 0)
		{
			Debug.Log("Save failed: bytes could not be read from json data");
			OnDataSaved?.Invoke(obj: false);
		}
		else
		{
			File.WriteAllBytes(path, bytes);
			Debug.Log("Saved JSON to: " + path);
			OnDataSaved?.Invoke(obj: true);
		}
	}

	public T LoadBinary<T>(string filename) where T : class
	{
		string path = GetPath(filename);
		if (File.Exists(path))
		{
			try
			{
				using Stream serializationStream = File.OpenRead(path);
				return new BinaryFormatter().Deserialize(serializationStream) as T;
			}
			catch (Exception ex)
			{
				Debug.LogWarning(ex.Message);
			}
		}
		return null;
	}

	public void SaveBinary<T>(string filename, T data) where T : class
	{
		string path = GetPath(filename);
		using (Stream serializationStream = File.OpenWrite(path))
		{
			new BinaryFormatter().Serialize(serializationStream, data);
		}
		Debug.Log(path + " saved");
	}
}
