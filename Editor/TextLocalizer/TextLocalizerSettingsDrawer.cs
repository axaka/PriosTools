using System.Linq;
using UnityEngine;
using UnityEditor;

namespace PriosTools
{
	[CustomPropertyDrawer(typeof(TextLocalizerSettings), true)]
	public class TextLocalizerSettingsDrawer : PropertyDrawer
	{
		private bool _showAdvanced;

		public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
		{
			EditorGUI.BeginProperty(position, GUIContent.none, prop);

			// Cache child props:
			var userDataProp = prop.FindPropertyRelative("userData");
			var dataStoreProp = prop.FindPropertyRelative("dataStore");
			var sheetProp = prop.FindPropertyRelative("sheet");
			var useTypeProp = prop.FindPropertyRelative("useTypewriterEffect");
			var speedProp = prop.FindPropertyRelative("typewriterSpeed");
			var multProp = prop.FindPropertyRelative("speedUpMultiplier");
			var pagProp = prop.FindPropertyRelative("enablePagination");
			var autoProp = prop.FindPropertyRelative("autoDetectBounds");
			var maxHProp = prop.FindPropertyRelative("maxHeight");
			var scrollProp = prop.FindPropertyRelative("scrollOneLineAtATime");
			var soundsProp = prop.FindPropertyRelative("characterSounds");
			var pitchProp = prop.FindPropertyRelative("pitchVariation");
			var replProp = prop.FindPropertyRelative("textReplacements");
			var keyFieldProp = prop.FindPropertyRelative("keyField");
			var langDispProp = prop.FindPropertyRelative("languageDisplayKey");

			// 1) Data Configuration
			EditorGUILayout.LabelField("Data Configuration", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(userDataProp);
			EditorGUILayout.PropertyField(dataStoreProp);

			// Sheet as dropdown
			if (dataStoreProp.objectReferenceValue is PriosDataStore ds && ds.SheetNames != null)
			{
				var sheets = ds.SheetNames.ToArray();
				int current = Mathf.Max(0, System.Array.IndexOf(sheets, sheetProp.stringValue));
				int picked = EditorGUILayout.Popup("Sheet", current, sheets);
				sheetProp.stringValue = (picked >= 0 && picked < sheets.Length)
					? sheets[picked]
					: sheetProp.stringValue;
			}
			else
			{
				using (new EditorGUI.DisabledScope(dataStoreProp.objectReferenceValue == null))
					EditorGUILayout.PropertyField(sheetProp);
			}

			// Text Replacement
			EditorGUILayout.PropertyField(replProp, true);

			EditorGUILayout.Space();

			// 2) Typewriter Effect
			EditorGUILayout.LabelField("Typewriter Effect", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(useTypeProp);
			if (useTypeProp.boolValue)
			{
				EditorGUILayout.PropertyField(speedProp);
				EditorGUILayout.PropertyField(multProp);
			}
			EditorGUILayout.Space();

			// 3) Pagination
			EditorGUILayout.LabelField("Pagination", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(pagProp);
			if (pagProp.boolValue)
			{
				EditorGUILayout.PropertyField(autoProp);
				if (!autoProp.boolValue)
					EditorGUILayout.PropertyField(maxHProp);
				EditorGUILayout.PropertyField(scrollProp);
			}
			EditorGUILayout.Space();

			// Audio
			EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
			pitchProp.floatValue = EditorGUILayout.Slider(
				new GUIContent("Pitch Variation"),
				pitchProp.floatValue,
				0f,
				1f
			);
			EditorGUILayout.PropertyField(soundsProp, true);
			EditorGUILayout.Space();

			// 6) Advanced Settings
			if (keyFieldProp != null && langDispProp != null)
			{
				_showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced Settings", true);
				if (_showAdvanced)
				{
					EditorGUI.indentLevel++;
					EditorGUILayout.PropertyField(keyFieldProp, new GUIContent("Key Field"));
					EditorGUILayout.PropertyField(langDispProp, new GUIContent("Language Display Key"));
					EditorGUI.indentLevel--;
					EditorGUILayout.Space();
				}
			}

			EditorGUI.EndProperty();
		}

		// No GetPropertyHeight override needed when using EditorGUILayout.
	}
}
