using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PriosTools
{
	[CustomEditor(typeof(PriosSpreadsheet))]
	public class PriosSpreadsheetEditor : Editor
	{
		private PriosSpreadsheet spreadsheet;
		private Dictionary<string, List<object>> parsedData = new();
		private Dictionary<string, Type> dataTypes = new();
		private Vector2 scrollPos;
		private Dictionary<string, bool> foldoutStates = new();

		void OnEnable()
		{
			spreadsheet = (PriosSpreadsheet)target;
			RefreshJsonData();
		}

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			GUILayout.Space(10);
			if (GUILayout.Button("Run"))
			{
				spreadsheet.Run();
				AssetDatabase.Refresh();
				RefreshJsonData();
			}

			if (GUILayout.Button("Refresh Preview"))
			{
				AssetDatabase.Refresh();
				RefreshJsonData();
			}

			GUILayout.Space(10);
			EditorGUILayout.LabelField("📄 JSON Sheet Preview", EditorStyles.boldLabel);

			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

			if (spreadsheet.SheetNames == null || spreadsheet.SheetNames.Count == 0)
			{
				EditorGUILayout.HelpBox("No sheet names found. Press 'Run' to fetch data.", MessageType.Info);
			}
			else
			{
				foreach (var sheetName in spreadsheet.SheetNames)
				{
					string fileName = sheetName.Replace(" ", "_");

					foldoutStates.TryAdd(fileName, false);
					foldoutStates[fileName] = EditorGUILayout.Foldout(foldoutStates[fileName], $"▶ {fileName}", true);

					if (foldoutStates[fileName])
					{
						EditorGUI.indentLevel++;

						if (!parsedData.ContainsKey(fileName))
						{
							EditorGUILayout.HelpBox($"Missing JSON file for sheet: {fileName}", MessageType.Warning);
						}
						else
						{
							var entries = parsedData[fileName];
							var type = dataTypes.ContainsKey(fileName) ? dataTypes[fileName] : null;

							for (int i = 0; i < entries.Count; i++)
							{
								var entry = entries[i];
								EditorGUILayout.BeginVertical("box");
								EditorGUILayout.LabelField($"Entry {i + 1}", EditorStyles.boldLabel);

								if (type != null && entry != null)
								{
									FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
									foreach (var field in fields)
									{
										object value = null;
										try { value = field.GetValue(entry); }
										catch { }

										string valueStr = value is Array arr
											? string.Join(", ", arr.Cast<object>())
											: value?.ToString();

										//EditorGUILayout.LabelField(field.Name, valueStr ?? "<null>");
										EditorGUILayout.LabelField($"{field.Name} ({field.FieldType.Name})", valueStr ?? "<null>");

									}
								}
								else
								{
									EditorGUILayout.LabelField(entry?.ToString() ?? "<null>");
								}

								EditorGUILayout.EndVertical();
							}
						}

						EditorGUI.indentLevel--;
						GUILayout.Space(6);
					}
				}

			}

			EditorGUILayout.EndScrollView();
		}

		private void RefreshJsonData()
		{
			parsedData.Clear();
			dataTypes.Clear();

			if (spreadsheet.SheetNames == null || spreadsheet.SheetNames.Count == 0)
				return;

			foreach (string sheetName in spreadsheet.SheetNames)
			{
				Debug.Log($"[Preview] Looking for sheet: {sheetName}");

				string fileName = sheetName.Replace(" ", "_");
				string resourcePath = $"JsonData/{fileName}";

				TextAsset ta = Resources.Load<TextAsset>(resourcePath);
				if (ta == null)
				{
					Debug.LogWarning($"❌ Resources.Load failed: {resourcePath}");
				}
				else
				{
					Debug.Log($"✅ Found JSON file: {resourcePath}");
				}


				if (ta == null)
				{
					Debug.LogWarning($"JSON file not found in Resources: JsonData/{fileName}.json");
					continue;
				}

				Type type = GetGeneratedType(fileName);
				if (type != null)
				{
					Debug.Log($"✅ Found generated type: {type.Name}");
				}
				else
				{
					Debug.LogWarning($"❌ No matching generated class found for: {fileName}");
				}

				if (type != null)
				{
					var list = DeserializeToList(type, fileName);
					if (list != null)
					{
						parsedData[fileName] = list;
						dataTypes[fileName] = type;
					}
				}
				else
				{
					var fallbackList = ta.text.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).Cast<object>().ToList();
					parsedData[fileName] = fallbackList;
				}
			}
		}

		private Type GetGeneratedType(string className)
		{
			return AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a => a.GetTypes())
				.FirstOrDefault(t => t.Name == className && t.BaseType != null && t.BaseType.IsGenericType &&
									 t.BaseType.GetGenericTypeDefinition() == typeof(PriosJsonSheetBase<>));
		}


		private List<object> DeserializeToList(Type type, string fileName)
		{
			try
			{
				MethodInfo loadMethod = type.GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
				if (loadMethod == null) return null;

				var rawList = loadMethod.Invoke(null, new object[] { fileName });
				if (rawList is IEnumerable<object> casted)
					return casted.ToList();
				if (rawList is System.Collections.IEnumerable ie)
					return ie.Cast<object>().ToList();

				return null;
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"Failed to load type {type.Name}: {ex.Message}");
				return null;
			}
		}
	}
}
