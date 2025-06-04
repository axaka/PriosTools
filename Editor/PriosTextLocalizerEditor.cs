using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PriosTools
{
	[CustomEditor(typeof(PriosTextLocalizer))]
	public class PriosTextLocalizerEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var localizer = (PriosTextLocalizer)target;

			EditorGUILayout.PropertyField(serializedObject.FindProperty("dataStore"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("userData"));

			if (!localizer.dataStore || !localizer.userData)
			{
				serializedObject.ApplyModifiedProperties();
				return;
			}

			EditorGUILayout.Space(10f);

			var sheetProp = serializedObject.FindProperty("sheet");
			var sheetOptions = localizer.dataStore.SheetNames?
				.Select(name => PriosDataStore.classPrefix + name)
				.ToList();
			PriosEditor.DrawDropdownFromList("Sheet", sheetProp, sheetOptions);

			var keyProp = serializedObject.FindProperty("key");
			var keyOptions = localizer.dataStore.GetFieldValues(sheetProp.stringValue, "Key");
			PriosEditor.DrawDropdownFromList("Key", keyProp, keyOptions);

			EditorGUILayout.Space(10f);

			EditorGUILayout.PropertyField(serializedObject.FindProperty("useTypewriterEffect"));

			if (localizer.useTypewriterEffect)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("supportRichText"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("typewriterSpeed"));

				EditorGUILayout.PropertyField(serializedObject.FindProperty("characterSounds"), true);
				if (localizer.characterSounds != null && localizer.characterSounds.Length > 0)
				{
					EditorGUILayout.PropertyField(serializedObject.FindProperty("pitchRange"));
				}

			}

			EditorGUILayout.Space(10f);

			if (GUILayout.Button("Update Text"))
			{
				localizer.UpdateText();

				// In edit mode, mark the object dirty so Unity updates it visually
				if (!Application.isPlaying)
				{
					EditorUtility.SetDirty(localizer);
					SceneView.RepaintAll(); // Optional: refresh scene view if it's visible in there
				}
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
