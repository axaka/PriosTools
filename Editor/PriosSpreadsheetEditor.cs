
using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PriosTools
{
	[CustomEditor(typeof(PriosSpreadsheet))]
	public class PriosSpreadsheetEditor : Editor
	{
		private PriosSpreadsheet spreadsheet;
		private Vector2 scroll;
		private Dictionary<string, Vector2> scrolls = new();
		private Dictionary<string, bool> foldouts = new();

		void OnEnable()
		{
			spreadsheet = (PriosSpreadsheet)target;
		}

		private async void Editor_GenerateClassesClicked()
		{
			await spreadsheet.DownloadAndGenerateClasses();
			EditorUtility.DisplayDialog("Classes Generated",
				"Class files were generated successfully.\n\nPlease wait for Unity to recompile.\nAfter that, use 'Download and Apply Data' to populate the spreadsheet.",
				"OK");
		}
		private async void GenerateDataClicked()
		{
			await spreadsheet.DownloadAndApplyData();

			int sheetCount = spreadsheet.SheetNames?.Count ?? 0;

			string summary;
			if (sheetCount > 0)
			{
				summary = $"Successfully parsed and stored data for {sheetCount} sheet{(sheetCount == 1 ? "" : "s")}.\n\nYou can now use the data directly from the ScriptableObject.";
			}
			else
			{
				summary = "No sheet data was found or applied.";
			}

			EditorUtility.DisplayDialog("Data Download Complete", summary, "OK");
		}



		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			GUILayout.Space(10);

			EditorGUILayout.HelpBox("This tool downloads Google Sheet data, generates matching classes, and stores typed data in this ScriptableObject.", MessageType.Info);

			if (GUILayout.Button("Generate C# Classes"))
			{
				Editor_GenerateClassesClicked();
			}

			if (GUILayout.Button("Download and Apply Data"))
			{
				GenerateDataClicked();
			}

			GUILayout.Space(10);
			EditorGUILayout.LabelField("Live Parsed Data", EditorStyles.boldLabel);

			scroll = EditorGUILayout.BeginScrollView(scroll);

			FieldInfo typedListField = typeof(PriosSpreadsheet).GetField("_typedLists", BindingFlags.NonPublic | BindingFlags.Instance);
			if (typedListField != null)
			{
				var typedLists = typedListField.GetValue(spreadsheet) as IEnumerable<object>;
				if (typedLists != null)
				{
					foreach (var list in typedLists)
					{
						Type listType = list.GetType();
						if (!listType.IsGenericType) continue;

						Type elementType = listType.GetGenericArguments()[0];
						string label = elementType.Name;

						if (!foldouts.ContainsKey(label))
							foldouts[label] = false;

						foldouts[label] = EditorGUILayout.Foldout(foldouts[label], label, true);
						if (foldouts[label])
						{
							EditorGUI.indentLevel++;
							int count = 0;

							var fields = elementType.GetFields(BindingFlags.Public | BindingFlags.Instance);
							if (fields.Length == 0)
							{
								EditorGUILayout.LabelField("No public fields found.");
								continue;
							}

							// Combined Header: Type + Field Name (e.g., "int? MyField")
							EditorGUILayout.BeginHorizontal("box");
							foreach (var field in fields)
							{
								string typeName = GetPrettyTypeName(field.FieldType);
								GUILayout.Label($"{typeName} {field.Name}", EditorStyles.boldLabel, GUILayout.MinWidth(120));
							}
							EditorGUILayout.EndHorizontal();


							int rowCount = 0;
							foreach (var item in (IEnumerable)list)
							{
								EditorGUILayout.BeginHorizontal("box");
								foreach (var field in fields)
								{
									object value = field.GetValue(item);
									string str = value is Array a ? string.Join(", ", a.Cast<object>()) : value?.ToString() ?? "";
									GUIStyle wrapStyle = new GUIStyle(EditorStyles.label)
									{
										wordWrap = true
									};
									GUILayout.Label(str, wrapStyle, GUILayout.MinWidth(100), GUILayout.MaxWidth(300));
								}
								EditorGUILayout.EndHorizontal();

								rowCount++;
								if (rowCount > 100)
								{
									EditorGUILayout.LabelField("... truncated ...");
									break;
								}
							}



							EditorGUI.indentLevel--;
						}
					}
				}
			}

			EditorGUILayout.EndScrollView();
		}

		private static string GetPrettyTypeName(Type type)
		{
			if (type.IsArray)
				return GetPrettyTypeName(type.GetElementType()) + "[]";

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
				return GetPrettyTypeName(Nullable.GetUnderlyingType(type)) + "?";

			return type.Name switch
			{
				"String" => "string",
				"Int32" => "int",
				"Boolean" => "bool",
				"Single" => "float",
				_ => type.Name
			};
		}


	}
}
