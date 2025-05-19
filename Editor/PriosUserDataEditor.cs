using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

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
			return string.IsNullOrEmpty(json)
				? new Dictionary<string, string>()
				: JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
		}

		private void SaveToPrefs(PriosUserData data, Dictionary<string, string> updated)
		{
			string json = JsonConvert.SerializeObject(updated);
			PlayerPrefs.SetString(GetPrefsKey(data), json);
			PlayerPrefs.Save();
		}

		private string GetPrefsKey(PriosUserData data)
		{
			var field = typeof(PriosUserData).GetField("_playerPrefKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			return field?.GetValue(data) as string ?? data.name; // fallback
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
