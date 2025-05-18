using System;
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
			return AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Editor/Icons/{name}.png");
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
	}
}
