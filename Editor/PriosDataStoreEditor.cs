using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace PriosTools
{
	[CustomEditor(typeof(PriosDataStore))]
	public class PriosDataStoreEditor : Editor
	{
		public override VisualElement CreateInspectorGUI()
		{
			var dataStore = (PriosDataStore)target;
			var root = new VisualElement();

			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/PriosEditorStyles.uss");
			if (styleSheet != null) root.styleSheets.Add(styleSheet);
			else Debug.LogWarning("PriosEditorStyles.uss not found. Please ensure the file exists at the specified path.");

			root.Add(CreateHeader(dataStore));
			root.Add(CreateDataPreview(dataStore));

			return root;
		}

		private static VisualElement CreateDataPreview(PriosDataStore dataStore)
		{
			const float ColumnMinWidth = 125f;
			var container = new VisualElement();

			container.Add(new Label("Data Preview")
			{
				style = {
					unityFontStyleAndWeight = FontStyle.Bold,
					marginTop = 10,
					marginBottom = 4,
					fontSize = 13
				}
			});

			foreach (var list in dataStore.TypedLists)
			{
				Type listType = list.GetType();
				if (!listType.IsGenericType) continue;

				Type elementType = listType.GetGenericArguments()[0];
				if (!elementType.Name.StartsWith("PDS_")) continue;

				var foldout = new Foldout
				{
					text = elementType.Name,
					value = false
				};
				foldout.style.marginBottom = 6;
				foldout.style.unityFontStyleAndWeight = FontStyle.Bold;
				foldout.style.fontSize = 12;
				container.Add(foldout);

				var items = ((IEnumerable)list).Cast<object>().ToList();
				var fields = elementType.GetFields(BindingFlags.Public | BindingFlags.Instance);
				float totalWidth = fields.Length * ColumnMinWidth;

				var scroll = new ScrollView(ScrollViewMode.Horizontal)
				{
					style = {
						maxHeight = 220,
						flexGrow = 1,
						borderTopLeftRadius = 4,
						borderTopRightRadius = 4,
						borderBottomLeftRadius = 4,
						borderBottomRightRadius = 4,
						borderBottomWidth = 1,
						borderTopWidth = 1,
						borderLeftWidth = 1,
						borderRightWidth = 1,
						backgroundColor = new Color(0.11f, 0.11f, 0.11f)
					}
				};
				foldout.Add(scroll);

				var verticalStack = new VisualElement
				{
					style = {
						flexDirection = FlexDirection.Column,
						minWidth = totalWidth
					}
				};
				scroll.Add(verticalStack);

				// Header row
				var header = new VisualElement
				{
					style = {
						flexDirection = FlexDirection.Row,
						backgroundColor = new Color(0.22f, 0.22f, 0.22f),
						borderBottomWidth = 1,
						borderBottomColor = new Color(0.3f, 0.3f, 0.3f)
					}
				};

				foreach (var field in fields)
				{
					header.Add(new Label($"{GetPrettyTypeName(field.FieldType)} {field.Name.TrimEnd('_')}")
					{
						style = {
							minWidth = ColumnMinWidth,
							marginRight = 5,
							paddingLeft = 4,
							unityFontStyleAndWeight = FontStyle.Bold,
							fontSize = 11,
							color = Color.white
						}
					});
				}
				verticalStack.Add(header);

				// Data rows
				for (int i = 0; i < items.Count; i++)
				{
					var row = new VisualElement
					{
						style = {
							flexDirection = FlexDirection.Row,
							backgroundColor = i % 2 == 0
								? new Color(0.16f, 0.16f, 0.16f)
								: new Color(0.13f, 0.13f, 0.13f)                }
					};

					// Optional: Hover highlight effect
					row.RegisterCallback<MouseEnterEvent>(_ =>
					{
						row.style.backgroundColor = new StyleColor(new Color(0.25f, 0.25f, 0.3f));
					});
					row.RegisterCallback<MouseLeaveEvent>(_ =>
					{
						row.style.backgroundColor = new StyleColor(i % 2 == 0
							? new Color(0.16f, 0.16f, 0.16f)
							: new Color(0.13f, 0.13f, 0.13f));
					});

					foreach (var field in fields)
					{
						object val = field.GetValue(items[i]);
						row.Add(new Label(FormatFieldValue(val))
						{
							style = {
								minWidth = ColumnMinWidth,
								marginRight = 5,
								paddingLeft = 4,
								unityTextAlign = TextAnchor.MiddleLeft,
								whiteSpace = WhiteSpace.Normal,
								fontSize = 11,
								color = new Color(0.9f, 0.9f, 0.9f)
							}
						});
					}

					verticalStack.Add(row);
				}
			}

			return container;
		}


		private VisualElement CreateHeader(PriosDataStore dataStore)
		{
			var container = PriosEditor.CreateBox();

			// ▶️ First row: Data URL label + Source Type label
			var labelRow = new VisualElement
			{
				style = {
					flexDirection = FlexDirection.Row,
					justifyContent = Justify.SpaceBetween,
					marginBottom = 2,
					marginLeft = 5,
					marginRight = 5
				}
			};

			var urlLabel = new Label("Data URL")
			{
				style = {
					unityFontStyleAndWeight = FontStyle.Bold,
					fontSize = 12
				}
			};

			var handlerLabel = new Label()
			{
				style = {
					fontSize = 11,
					color = new Color(0.7f, 0.7f, 0.7f),
					unityTextAlign = TextAnchor.MiddleRight,
					flexGrow = 1,
					overflow = Overflow.Hidden,
					textOverflow = TextOverflow.Ellipsis,
					whiteSpace = WhiteSpace.NoWrap
				}
			};

			// Initial handler type
			var handler = dataStore.CurrentHandler;
			handlerLabel.text = $"Source Type: {(handler?.SourceType ?? "Unknown")}";

			labelRow.Add(urlLabel);
			labelRow.Add(handlerLabel);
			container.Add(labelRow);

			// ▶️ URL Field
			var urlField = new PropertyField(serializedObject.FindProperty("Url"))
			{
				label = ""
			};
			urlField.style.marginRight = 5;
			container.Add(urlField);

			// ▶️ Update handler label when URL changes
			urlField.RegisterValueChangeCallback(_ =>
			{
				var updatedHandler = dataStore.CurrentHandler;
				handlerLabel.text = $"Source Type: {(updatedHandler?.SourceType ?? "Unknown")}";
			});

			// ▶️ Info Box
			var infoBox = new HelpBox("", HelpBoxMessageType.Info)
			{
				style = { marginTop = 5, marginBottom = 5 }
			};
			container.Add(infoBox);

			infoBox.schedule.Execute(() =>
			{
				DateTime? lastTime = dataStore.LastDownloadedTime;
				if (lastTime.HasValue && dataStore.SpreadsheetId != null)
				{
					DateTime localTime = lastTime.Value.ToLocalTime();
					TimeSpan elapsed = DateTime.Now - localTime;
					string agoText = elapsed.TotalMinutes < 1
						? $"{Mathf.FloorToInt((float)elapsed.TotalSeconds)} seconds ago"
						: $"{Mathf.FloorToInt((float)elapsed.TotalMinutes)} minutes ago";
					infoBox.text = $"Last Downloaded: {localTime:yyyy-MM-dd HH:mm:ss}\n{agoText}";
				}
				else infoBox.text = "Insert a valid URL to your data source";
			}).Every(1000).StartingIn(0);

			// Generate Button
			var generateBtn = PriosEditor.CreateButton(async () =>
			{
				await dataStore.GenerateDataModels();
				EditorUtility.DisplayDialog("Data Models Generated",
					"Class files were generated successfully.\n\nPlease wait for Unity to recompile.\nAfter that, use 'Update Data' to populate the spreadsheet.",
					"OK");
			}, icon: LoadIcon("Tools"), size: new Vector2(16, 16), tooltip: "Generate Data Models");

			// Clear Button
			var clearBtn = PriosEditor.CreateButton(() =>
			{
				dataStore.ClearGeneratedData();
				return Task.CompletedTask;
			}, icon: LoadIcon("Garbage-Closed"),
			tooltip: "Removes generated classes and cached spreadsheet data", size: new Vector2(16, 16));

			// Edit Button
			var editBtn = PriosEditor.CreateButton(() =>
			{
				if (dataStore.CurrentHandler != null)
					dataStore.CurrentHandler.OpenInBrowser(dataStore.Url);
				else
					Debug.LogWarning("No handler found for current URL.");

				return Task.CompletedTask;
			}, icon: LoadIcon("Pencil"), size: new Vector2(16, 16), tooltip: "Open in external source");

			// Refresh Button
			var updateBtn = PriosEditor.CreateButton(async () =>
			{
				await dataStore.UpdateData();
				int count = dataStore.SheetNames?.Count ?? 0;
				string plural = count == 1 ? "" : "s";
				string message = count > 0
					? $"Successfully parsed and stored data for {count} sheet{plural}.\n\nYou can now use the data directly from the ScriptableObject."
					: "No sheet data was found or applied.";
				EditorUtility.DisplayDialog("Data Update Complete", message, "OK");
			}, icon: LoadIcon("Refresh"),
				size: new Vector2(16, 16), tooltip: "Refresh Content Only");

			var buttonRow = new VisualElement
			{
				style = {
					flexDirection = FlexDirection.Row,
					justifyContent = Justify.FlexStart
				}
			};
			buttonRow.Add(generateBtn);
			buttonRow.Add(clearBtn);
			buttonRow.Add(editBtn);
			buttonRow.Add(updateBtn);
			container.Add(buttonRow);

			return container;
		}

		private Texture2D LoadIcon(string name)
		{
			return AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Editor/Icons/{name}.png");
		}

		private static string FormatFieldValue(object value, int maxItems = 10)
		{
			if (value == null) return "<null>";

			if (value is Array array)
			{
				var items = array.Cast<object>().Select(item =>
				{
					if (item == null) return "<null>";
					if (item is Color color) return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
					if (item is DateTime dt)
					{
						if (dt.Date == DateTime.Today && dt.TimeOfDay.TotalSeconds > 0) return dt.ToString("HH:mm");
						if (dt.TimeOfDay.TotalSeconds == 0) return dt.ToString("yyyy-MM-dd");
						return dt.ToString("yyyy-MM-dd HH:mm");
					}
					return item.ToString();
				}).ToList();

				int total = items.Count;
				if (total == 0) return "[]";
				var previewItems = items.Take(maxItems).ToList();
				string suffix = total > maxItems ? $" (+{total - maxItems} more)" : "";
				return $"[{string.Join(", ", previewItems)}{suffix}]";
			}

			if (value is Color c) return $"#{ColorUtility.ToHtmlStringRGBA(c)}";
			if (value is DateTime dtSingle)
			{
				if (dtSingle.Date == DateTime.Today && dtSingle.TimeOfDay.TotalSeconds > 0)
					return dtSingle.ToString("HH:mm");
				return dtSingle.TimeOfDay.TotalSeconds == 0
					? dtSingle.ToString("yyyy-MM-dd")
					: dtSingle.ToString("yyyy-MM-dd HH:mm");
			}

			return value.ToString();
		}

		private static string GetPrettyTypeName(Type type)
		{
			if (type.IsArray) return GetPrettyTypeName(type.GetElementType()) + "[]";
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
				return GetPrettyTypeName(Nullable.GetUnderlyingType(type)) + "?";
			return type.Name switch
			{
				"String" => "string",
				"Int32" => "int",
				"Boolean" => "bool",
				"Single" => "float",
				"Double" => "double",
				"DateTime" => "DateTime",
				_ => type.Name
			};
		}
	}
}
