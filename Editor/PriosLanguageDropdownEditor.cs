using UnityEditor;
using UnityEngine;
using System.Linq;

namespace PriosTools
{
	[CustomEditor(typeof(PriosLanguageDropdown))]
	public class PriosLanguageDropdownEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var dropdown = (PriosLanguageDropdown)target;

			// Object references
			EditorGUILayout.PropertyField(serializedObject.FindProperty("userData"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("dataStore"));

			EditorGUILayout.Space(10);

			// Sheet selection dropdown
			var sheetProp = serializedObject.FindProperty("dataStoreSheet");
			if (dropdown.dataStore != null && dropdown.dataStore.TypedLookup?.Count > 0)
			{
				var sheetOptions = dropdown.dataStore.TypedLookup.Keys
					.Select(t => t.Name)
					.OrderBy(n => n)
					.ToList();

				PriosEditor.DrawDropdownFromList("Data Store Sheet", sheetProp, sheetOptions);
			}
			else
			{
				EditorGUILayout.PropertyField(sheetProp);
				EditorGUILayout.HelpBox("Assign a valid PriosDataStore with loaded data to choose a sheet.", MessageType.Info);
			}

			// UserData key dropdown
			var keyProp = serializedObject.FindProperty("userDataKey");
			if (dropdown.userData != null)
			{
				var userKeys = dropdown.userData.GetDefinedKeys().ToList();
				PriosEditor.DrawDropdownFromList("User Data Key", keyProp, userKeys);
			}
			else
			{
				EditorGUILayout.PropertyField(keyProp);
				EditorGUILayout.HelpBox("Assign a valid PriosUserData to choose a key.", MessageType.Info);
			}

			serializedObject.ApplyModifiedProperties();

			EditorGUILayout.Space(10);
			if (GUILayout.Button("Update Dropdown Now"))
			{
				dropdown.OnValidate();
				EditorUtility.SetDirty(dropdown);
			}
		}
	}
}
