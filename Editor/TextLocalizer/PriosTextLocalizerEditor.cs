using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace PriosTools
{
	[CustomEditor(typeof(PriosTextLocalizer))]
	public class PriosTextLocalizerEditor : Editor
	{
		private bool showSettings = false;
		private Vector2 keyScroll;

		SerializedProperty settingsAssetProp;
		SerializedProperty settingsInstanceProp;
		SerializedProperty keyProp;

		void OnEnable()
		{
			settingsAssetProp = serializedObject.FindProperty(nameof(PriosTextLocalizer.settings));
			settingsInstanceProp = serializedObject.FindProperty(nameof(PriosTextLocalizer.settingsInstance));
			keyProp = serializedObject.FindProperty(nameof(PriosTextLocalizer.key));
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			// Always show Settings Asset
			EditorGUILayout.PropertyField(settingsAssetProp, new GUIContent("Settings Asset"));
			EditorGUILayout.Space();

			// Key field on one line
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Key", GUILayout.Width(EditorGUIUtility.labelWidth));
			keyProp.stringValue = EditorGUILayout.TextField(keyProp.stringValue);
			EditorGUILayout.EndHorizontal();

			// Suggestions list
			var localizer = (PriosTextLocalizer)target;
			var cfg = localizer.ActiveSettings;
			if (cfg != null && cfg.dataStore != null && !string.IsNullOrEmpty(cfg.sheet))
			{
				var allKeys = cfg.dataStore.GetFieldValues(cfg.sheet, "Key");
				var filter = keyProp.stringValue ?? string.Empty;
				var suggestions = string.IsNullOrEmpty(filter)
					? allKeys.ToList()
					: allKeys.Where(k => k.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0).ToList();
				suggestions.RemoveAll(k => string.Equals(k, filter, System.StringComparison.Ordinal));

				const int MaxVisible = 6;
				float lineH = EditorGUIUtility.singleLineHeight;
				float height = Mathf.Min(suggestions.Count, MaxVisible) * (lineH + 2);
				keyScroll = EditorGUILayout.BeginScrollView(keyScroll, GUILayout.Height(height));
				foreach (var s in suggestions)
				{
					if (GUILayout.Button(s, EditorStyles.miniButton))
					{
						keyProp.stringValue = s;
						GUI.FocusControl(null);
					}
				}
				EditorGUILayout.EndScrollView();
				EditorGUILayout.Space();
			}

			// Dynamic foldout for instance vs shared settings
			if (settingsAssetProp.objectReferenceValue == null)
			{
				EditorGUILayout.Space();
				showSettings = EditorGUILayout.Foldout(showSettings, "Instance Settings", true);
			}
			else
			{
				var dataAsset = settingsAssetProp.objectReferenceValue as TextLocalizerData;
				showSettings = EditorGUILayout.Foldout(showSettings, dataAsset != null
					? $"Shared Settings ({dataAsset.name})"
					: "Shared Settings", true);
			}

			if (showSettings)
			{
				EditorGUI.indentLevel++;

				if (settingsAssetProp.objectReferenceValue == null)
				{
					// Draw inline override settings
					EditorGUILayout.PropertyField(settingsInstanceProp, GUIContent.none, true);
				}
				else
				{
					// Draw shared SO settings
					var dataAsset = settingsAssetProp.objectReferenceValue as TextLocalizerData;
					if (dataAsset != null)
					{
						var so = new SerializedObject(dataAsset);
						so.Update();
						EditorGUILayout.PropertyField(so.FindProperty(nameof(TextLocalizerData.Settings)), GUIContent.none, true);
						so.ApplyModifiedProperties();
					}
				}

				EditorGUI.indentLevel--;
				EditorGUILayout.Space();
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
