using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace PriosTools
{
	[CustomEditor(typeof(PriosUserData))]
	public class PriosUserDataEditor : Editor
	{
		private PriosUserData userData;
		private Dictionary<string, string> currentData;

		private void OnEnable()
		{
			userData = (PriosUserData)target;

			if (!Application.isPlaying)
			{
				currentData = LoadStoredPrefs(userData);
			}
		}

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("🧩 PlayerPrefs Data", EditorStyles.boldLabel);

			if (Application.isPlaying)
			{
				userData.Load(); // force refresh
				currentData = GetRuntimeData(userData);
			}

			if (currentData == null || currentData.Count == 0)
			{
				EditorGUILayout.HelpBox("No data found in PlayerPrefs.", MessageType.Info);
				return;
			}

			var definedKeys = userData.GetDefinedKeys();
			foreach (var key in currentData.Keys.ToList())
			{
				bool isDefined = definedKeys.Contains(key);
				string defaultValue = userData.GetDefaultForKey(key);

				using (new EditorGUILayout.VerticalScope("box"))
				{
					GUI.color = isDefined ? Color.white : new Color(1f, 0.9f, 0.6f);
					string newVal = EditorGUILayout.TextField(new GUIContent(key, $"Default: {defaultValue}"), currentData[key]);
					GUI.color = Color.white;

					if (newVal != currentData[key])
					{
						currentData[key] = newVal;
						if (Application.isPlaying)
						{
							userData.Set(key, newVal);
							ForceUpdateTextLocalizers();
						}
					}

					if (!isDefined)
					{
						EditorGUILayout.HelpBox("⚠ Key not defined in _data (schema).", MessageType.Warning);
					}
				}
			}

			if (!Application.isPlaying && GUILayout.Button("💾 Save to PlayerPrefs"))
			{
				SaveToPrefs(userData, currentData);
			}

			if (GUILayout.Button("♻ Reset to Defaults"))
			{
				userData.ResetToDefaults();
				currentData = GetRuntimeData(userData);
				Debug.Log("PriosUserData reset.");
			}
		}

		private Dictionary<string, string> LoadStoredPrefs(PriosUserData data)
		{
			string json = PlayerPrefs.GetString(GetPrefsKey(data), "");
			var result = new Dictionary<string, string>();

			if (!string.IsNullOrEmpty(json))
			{
				try
				{
					var node = JSON.Parse(json);
					if (node != null)
					{
						foreach (var kvp in node)
							result[kvp.Key] = kvp.Value.Value;
					}
				}
				catch
				{
					Debug.LogWarning("Failed to parse PlayerPrefs JSON with SimpleJSON.");
				}
			}

			return result;
		}

		private void SaveToPrefs(PriosUserData data, Dictionary<string, string> updated)
		{
			var jsonObj = new JSONObject();
			foreach (var kvp in updated)
			{
				jsonObj[kvp.Key] = kvp.Value;
			}
			PlayerPrefs.SetString(GetPrefsKey(data), jsonObj.ToString());
			PlayerPrefs.Save();
		}

		private string GetPrefsKey(PriosUserData data)
		{
			var field = typeof(PriosUserData).GetField("_playerPrefKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			return field?.GetValue(data) as string ?? data.name;
		}

		private Dictionary<string, string> GetRuntimeData(PriosUserData data)
		{
			var field = typeof(PriosUserData).GetField("_runtimeData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			return field?.GetValue(data) as Dictionary<string, string>;
		}

		private void ForceUpdateTextLocalizers()
		{
			var localizers = GameObject.FindObjectsOfType<PriosTextLocalizer>();
			foreach (var l in localizers)
			{
				l.UpdateText();
			}
		}
	}
}
