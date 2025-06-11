using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriosTools
{
	public static class PriosEditor
	{
		public static Texture2D LoadIconPng(string name)
		{
			// 1. Check local dev path
			string localPath = $"Assets/PriosTools/Editor/Icons/{name}.png";
			var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(localPath);
			if (icon != null) return icon;

			// 2. Check package path
			string packagePath = $"Packages/com.axaka.priostools/Editor/Icons/{name}.png";
			icon = AssetDatabase.LoadAssetAtPath<Texture2D>(packagePath);
			if (icon != null) return icon;

			Debug.LogWarning($"[PriosEditor] Icon not found: {name}.png");
			return null;
		}

		public static Box CreateBox(Color? color = null)
		{
			var connectionSection = new Box();

			// Full width
			connectionSection.style.flexGrow = 1;

			// Margins and padding
			connectionSection.style.marginBottom = 10;
			connectionSection.style.paddingBottom = 5;
			connectionSection.style.paddingTop = 5;
			connectionSection.style.paddingLeft = 5;
			connectionSection.style.paddingRight = 5;

			// Border
			connectionSection.style.borderTopWidth = 1;
			connectionSection.style.borderBottomWidth = 1;
			connectionSection.style.borderLeftWidth = 1;
			connectionSection.style.borderRightWidth = 1;

			// Background color (optional)
			connectionSection.style.backgroundColor = color ?? new Color(0.4f, 0.4f, 0.4f);

			return connectionSection;
		}

		public static Button CreateButton(
			Func<Task> onClickAsync,
			Texture2D icon = null,
			string tooltip = null,
			Vector2? size = null)
		{
			var button = new Button(() => onClickAsync?.Invoke()) // wrapper avoids warnings
			{
				tooltip = tooltip ?? string.Empty
			};

			button.style.paddingLeft = 2;
			button.style.paddingRight = 2;
			button.style.paddingTop = 2;
			button.style.paddingBottom = 2;
			button.style.justifyContent = Justify.Center;
			button.style.alignItems = Align.Center;

			if (icon != null)
			{
				var image = new Image
				{
					image = icon,
					scaleMode = ScaleMode.ScaleToFit,
					style =
					{
						width = size?.x ?? 16,
						height = size?.y ?? 16,
						marginLeft = 2,
						marginRight = 2
					}
				};

				button.Add(image);
			}
			else
			{
				button.text = "Button";
			}

			return button;
		}

		/// <summary>
		/// Draws a dropdown for a string property using a provided list of options.
		/// </summary>
		/// <param name="label">Label to show next to the dropdown.</param>
		/// <param name="property">The SerializedProperty (string) to edit.</param>
		/// <param name="options">List of valid string options for the dropdown.</param>
		public static void DrawDropdownFromList(string label, SerializedProperty property, List<string> options)
		{
			if (property == null)
			{
				EditorGUILayout.HelpBox("SerializedProperty is null.", MessageType.Error);
				return;
			}

			if (options == null || options.Count == 0)
			{
				EditorGUILayout.PropertyField(property);
				EditorGUILayout.HelpBox("No options available for dropdown.", MessageType.Info);
				return;
			}

			// Use index fallback if current value is missing
			int currentIndex = options.IndexOf(property.stringValue);
			if (currentIndex < 0) currentIndex = 0;

			int selectedIndex = EditorGUILayout.Popup(label, currentIndex, options.ToArray());

			// Only update if index is valid
			if (selectedIndex >= 0 && selectedIndex < options.Count)
				property.stringValue = options[selectedIndex];
		}

		/// <summary>
		/// Draws a text field with auto-complete suggestions from a list.
		/// </summary>
		/// <param name="label">The label to show.</param>
		/// <param name="property">The string property to edit.</param>
		/// <param name="options">List of possible autocomplete options.</param>
		/// <param name="maxSuggestions">How many suggestions to show (default 10).</param>
		public static void DrawAutocompleteField(string label, SerializedProperty property, List<string> options, int maxSuggestions = 10)
		{
			if (property == null)
			{
				EditorGUILayout.HelpBox("SerializedProperty is null.", MessageType.Error);
				return;
			}

			string currentInput = property.stringValue ?? "";
			string newInput = EditorGUILayout.TextField(label, currentInput);

			// Update property if changed
			if (newInput != currentInput)
			{
				property.stringValue = newInput;
			}

			if (!string.IsNullOrEmpty(newInput) && options != null && options.Count > 0)
			{
				var matches = options
					.Where(k => k.IndexOf(newInput, StringComparison.OrdinalIgnoreCase) >= 0)
					.Take(maxSuggestions)
					.ToList();

				if (matches.Count > 0)
				{
					GUILayout.BeginVertical("box");
					foreach (string match in matches)
					{
						if (GUILayout.Button(match, EditorStyles.miniButton))
						{
							property.stringValue = match;
							GUI.FocusControl(null); // defocus to apply
						}
					}
					GUILayout.EndVertical();
				}
			}
		}

		public static void DrawAutocompleteFieldInline(SerializedProperty property, List<string> options, string hint = "Type to search...")
		{
			string currentValue = property.stringValue ?? "";

			EditorGUI.BeginChangeCheck();
			string newValue = EditorGUILayout.TextField(currentValue, GUI.skin.textField);

			if (EditorGUI.EndChangeCheck())
				property.stringValue = newValue;

			// Only show suggestions if text partially matches and is not exact
			if (!string.IsNullOrEmpty(newValue) && !options.Contains(newValue))
			{
				var filtered = options
					.Where(opt => opt.IndexOf(newValue, System.StringComparison.OrdinalIgnoreCase) >= 0)
					.Take(10) // limit for performance
					.ToList();

				if (filtered.Count > 0)
				{
					EditorGUILayout.BeginVertical("box");
					EditorGUILayout.LabelField(hint, EditorStyles.miniLabel);
					foreach (var match in filtered)
					{
						if (GUILayout.Button(match, EditorStyles.miniButton))
						{
							property.stringValue = match;
							GUI.FocusControl(null);
							break;
						}
					}
					EditorGUILayout.EndVertical();
				}
			}
		}

	}
}
