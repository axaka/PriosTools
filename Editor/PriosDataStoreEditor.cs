using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PriosTools
{
	[CustomEditor(typeof(PriosDataStore))]
	public class PriosDataStoreEditor : Editor
	{
		private PriosDataStore dataStore;
		private Dictionary<string, Vector2> scrolls = new();
		private Dictionary<string, bool> foldouts = new();

		private const int MaxRowsToDisplay = 100;
		private const string HelpText =
		"To use this tool:\n" +
		"1. Open your Google Sheet.\n" +
		"2. Go to File > Share > Publish to Web.\n" +
		"3. Copy the 'Published HTML' link and paste it into the File URL field.";

		void OnEnable()
		{
			dataStore = (PriosDataStore)target;
		}

		private async void Editor_GenerateDataModels()
		{
			await dataStore.Editor_GenerateDataModels();
			EditorUtility.DisplayDialog("Data Models Generated",
				"Class files were generated successfully.\n\nPlease wait for Unity to recompile.\nAfter that, use 'Update Data' to populate the spreadsheet.",
				"OK");
		}

		private async void UpdateData()
		{
			await dataStore.UpdateData();

			int count = dataStore.SheetNames?.Count ?? 0;
			string plural = count == 1 ? "" : "s";
			string message = count > 0
				? $"Successfully parsed and stored data for {count} sheet{plural}.\n\nYou can now use the data directly from the ScriptableObject."
				: "No sheet data was found or applied.";

			EditorUtility.DisplayDialog("Data Update Complete", message, "OK");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			if (string.IsNullOrEmpty(dataStore.url) || !dataStore.url.Contains("/pubhtml"))
			{
				EditorGUILayout.HelpBox(HelpText, MessageType.Warning);
			}
			else
			{
				DateTime? lastTime = dataStore.LastDownloadedTime;
				if (lastTime.HasValue)
				{
					DateTime localTime = lastTime.Value.ToLocalTime(); // Convert from UTC to local time
					TimeSpan elapsed = DateTime.Now - localTime;

					string agoText = elapsed.TotalMinutes < 1
						? $"{Mathf.FloorToInt((float)elapsed.TotalSeconds)} seconds ago"
						: $"{Mathf.FloorToInt((float)elapsed.TotalMinutes)} minutes ago";

					EditorGUILayout.HelpBox(
						$"Last Downloaded: {localTime:yyyy-MM-dd HH:mm:ss}\n{agoText}",
						MessageType.Info
					);
				}
				else
				{
					EditorGUILayout.HelpBox("No HTML has been downloaded yet.", MessageType.Info);
				}
			}

			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(dataStore.url)), new GUIContent("URL"), true);
			GUILayout.Space(10);

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Generate Data Models"))
			{
				Editor_GenerateDataModels();
			}

			if (GUILayout.Button("Update Data"))
			{
				UpdateData();
			}

			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);
			EditorGUILayout.LabelField("Data Preview", EditorStyles.boldLabel);

			foreach (var list in dataStore.TypedLists)
			{
				Type listType = list.GetType();
				if (!listType.IsGenericType) continue;

				Type elementType = listType.GetGenericArguments()[0];
				if (!elementType.Name.StartsWith("PDS_")) continue;
				string label = elementType.Name;

				if (!foldouts.ContainsKey(label))
					foldouts[label] = false;

				foldouts[label] = EditorGUILayout.Foldout(foldouts[label], label, true);
				if (foldouts[label])
				{
					if (!scrolls.ContainsKey(label))
						scrolls[label] = Vector2.zero;

					var items = ((IEnumerable)list).Cast<object>().ToList();
					int rowCount = items.Count;

					float rowHeight = 30f;
					float estimatedHeight = Mathf.Min(rowCount, MaxRowsToDisplay) * rowHeight + 30f;

					var fields = elementType.GetFields(BindingFlags.Public | BindingFlags.Instance);
					if (fields.Length == 0)
					{
						EditorGUILayout.LabelField("No public fields found.");
						continue;
					}

					// Sticky header (outside scroll view)
					EditorGUILayout.BeginHorizontal("box");
					foreach (var field in fields)
					{
						string typeLabel = GetPrettyTypeName(field.FieldType);
						GUILayout.Label($"{typeLabel} {field.Name}", EditorStyles.boldLabel, GUILayout.MinWidth(100));
					}
					EditorGUILayout.EndHorizontal();

					// Prepare a wrapped label style
					var wrapStyle = new GUIStyle(EditorStyles.label)
					{
						wordWrap = true
					};

					scrolls[label] = EditorGUILayout.BeginScrollView(
						scrolls[label],
						GUILayout.Height(Mathf.Min(estimatedHeight, 200))
					);

					foreach (var item in items)
					{
						EditorGUILayout.BeginHorizontal("box");
						foreach (var field in fields)
						{
							object value = field.GetValue(item);
							string str = FormatFieldValue(value);
							GUILayout.Label(str, wrapStyle, GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));
						}
						EditorGUILayout.EndHorizontal();
					}

					EditorGUILayout.EndScrollView();
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		private static string FormatFieldValue(object value)
		{
			return value is Array a ? string.Join(", ", a.Cast<object>()) : value?.ToString();
		}

		private static string GetPrettyTypeName(Type type)
		{
			if (type.IsArray)
				return GetPrettyTypeName(type.GetElementType()) + "[]";

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				var underlying = Nullable.GetUnderlyingType(type);
				return underlying != null ? GetPrettyTypeName(underlying) + "?" : "null?";
			}

			// Normalize common CLR names
			return type.Name switch
			{
				"String" => "string",
				"Int32" => "int",
				"Boolean" => "bool",
				"Single" => "float",
				"Double" => "double",
				_ => type.IsGenericType
					? $"{type.Name.Split('`')[0]}<{string.Join(", ", type.GetGenericArguments().Select(GetPrettyTypeName))}>"
					: type.Name
			};
		}
	}
}
