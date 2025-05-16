using System;
using UnityEditor;
using UnityEngine;

public static class PriosDebugger
{
	public static void LogWithScript(string message, Type type)
	{
		var script = GetScriptObject(type);
		if (script != null)
			Debug.Log(message, script);
		else
			Debug.LogWarning($"Script file not found for type {type.Name}. Message: {message}");
	}

	public static MonoScript GetScriptObject(Type type)
	{
		string fileName = type.Name + ".cs";
		string[] guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");

		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (path.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
			{
				return AssetDatabase.LoadAssetAtPath<MonoScript>(path);
			}
		}

		return null;
	}
}
