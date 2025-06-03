using System;
using System.Collections.Generic;
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

			// 2. Check package path (adjust if your package name differs)
			string packagePath = $"Packages/com.prios.priostools/PriosTools/Editor/Icons/{name}.png";
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

	}
}
