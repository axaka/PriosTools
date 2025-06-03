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

			var dataPreviewContainer = new VisualElement();
			root.Add(CreateHeader(dataStore, () =>
			{
				dataPreviewContainer.Clear();
				dataPreviewContainer.Add(CreateDataPreview(dataStore));
			}));

			dataPreviewContainer.Add(CreateDataPreview(dataStore));
			root.Add(dataPreviewContainer);

			return root;
		}

		private VisualElement CreateHeader(PriosDataStore dataStore, Action refreshCallback)
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

				if (lastTime.HasValue)
				{
					DateTime localTime = lastTime.Value.ToLocalTime();
					TimeSpan elapsed = DateTime.Now - localTime;
					string agoText = elapsed.TotalMinutes < 1
						? $"{Mathf.FloorToInt((float)elapsed.TotalSeconds)} seconds ago"
						: $"{Mathf.FloorToInt((float)elapsed.TotalMinutes)} minutes ago";

					infoBox.text = $"Last Downloaded: {localTime:yyyy-MM-dd HH:mm:ss}\n{agoText}";
				}
				else
				{
					infoBox.text = $"No data downloaded yet.";
				}
			}).Every(1000).StartingIn(0);

			// ▶️ Buttons
			var generateBtn = PriosEditor.CreateButton(async () =>
			{
				await dataStore.GenerateDataModels();
				EditorUtility.DisplayDialog("Data Models Generated",
					"Class files were generated successfully.\n\nPlease wait for Unity to recompile.\nAfter that, use 'Update Data' to populate the spreadsheet.",
					"OK");
			}, icon: PriosEditor.LoadIconPng("Tools"), size: new Vector2(16, 16), tooltip: "Generate Data Models");

			var clearBtn = PriosEditor.CreateButton(() =>
			{
				dataStore.ClearGeneratedData();
				refreshCallback?.Invoke();
				return Task.CompletedTask;
			}, icon: PriosEditor.LoadIconPng("Garbage-Closed"),
			tooltip: "Removes generated classes and cached spreadsheet data", size: new Vector2(16, 16));

			var editBtn = PriosEditor.CreateButton(() =>
			{
				dataStore.CurrentHandler?.OpenInBrowser(dataStore.Url);
				return Task.CompletedTask;
			}, icon: PriosEditor.LoadIconPng("Pencil"), size: new Vector2(16, 16), tooltip: "Open in external source");

			var updateBtn = PriosEditor.CreateButton(async () =>
			{
				await dataStore.UpdateData();
				int count = dataStore.SheetNames?.Count ?? 0;
				string plural = count == 1 ? "" : "s";
				string message = count > 0
					? $"Successfully parsed and stored data for {count} sheet{plural}.\n\nYou can now use the data directly from the ScriptableObject."
					: "No sheet data was found or applied.";
				EditorUtility.DisplayDialog("Data Update Complete", message, "OK");

				refreshCallback?.Invoke(); // 🔁 Redraw Data Preview
			}, icon: PriosEditor.LoadIconPng("Refresh"), size: new Vector2(16, 16), tooltip: "Refresh Content");

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

		private static VisualElement CreateDataPreview(PriosDataStore dataStore)
		{
			const float ColumnMinWidth = 150f;
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
				container.Add(foldout);

				var items = ((IEnumerable)list).Cast<object>().ToList();
				var fields = elementType.GetFields(BindingFlags.Public | BindingFlags.Instance);
				float totalWidth = fields.Length * ColumnMinWidth;

				var scroll = new ScrollView(ScrollViewMode.Horizontal)
				{
					style = {
						//maxHeight = 220,
						flexGrow = 1,
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

				var header = new VisualElement
				{
					style = {
						flexDirection = FlexDirection.Row,
						backgroundColor = new Color(0.22f, 0.22f, 0.22f)
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

				for (int i = 0; i < items.Count; i++)
				{
					var row = new VisualElement
					{
						style = {
							flexDirection = FlexDirection.Row,
							flexWrap = Wrap.Wrap,
							flexGrow = 1,
							overflow = Overflow.Visible,
							backgroundColor = i % 2 == 0
								? new Color(0.16f, 0.16f, 0.16f)
								: new Color(0.13f, 0.13f, 0.13f)
						}
					};

					foreach (var field in fields)
					{
						object val = field.GetValue(items[i]);
						var label = new Label(FormatFieldValue(val));
						label.style.minWidth = ColumnMinWidth;
						label.style.marginRight = 5;
						label.style.paddingLeft = 4;
						label.style.unityTextAlign = TextAnchor.UpperLeft;
						label.style.fontSize = 11;
						label.style.color = new Color(0.9f, 0.9f, 0.9f);
						label.style.whiteSpace = WhiteSpace.Normal;
						label.style.overflow = Overflow.Visible;
						label.style.flexGrow = 1;
						label.style.flexShrink = 1;
						label.style.flexBasis = Length.Auto();
						label.style.maxWidth = ColumnMinWidth; // ✅ prevents infinite horizontal growth
						label.style.height = StyleKeyword.Auto; // ✅ allow vertical grow
						label.style.unityOverflowClipBox = OverflowClipBox.PaddingBox; // ✅ avoids clipping
						row.Add(label);


					}

					verticalStack.Add(row);
				}
			}

			return container;
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

				if (previewItems.Count == 1)
					return $"[{previewItems[0]}]{suffix}";

				var formattedItems = string.Join(",\n", previewItems);
				return $"[\n{formattedItems},\n]{suffix}";
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
