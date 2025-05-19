using TMPro;
using UnityEngine;

namespace PriosTools
{
	[ExecuteAlways]
	[RequireComponent(typeof(TMP_Text))]
	public class PriosTextLocalizer : MonoBehaviour
	{
		public string sheet;
		public string key;
		public PriosDataStore dataStore;
		public PriosUserData userData;

		private TMP_Text textComponent;

		private void Awake()
		{
			EnsureComponent();
			RegisterKeyChangeCallback();
			UpdateText();
		}

		private void OnEnable()
		{
			EnsureComponent();
			RegisterKeyChangeCallback();
			UpdateText();
		}

		private void OnDisable()
		{
			UnregisterKeyChangeCallback();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			EnsureComponent();

			// Make sure we update when modified in Editor
			if (!Application.isPlaying)
				UpdateText();
		}
#endif

		private void EnsureComponent()
		{
			if (textComponent == null)
				textComponent = GetComponent<TMP_Text>();
		}

		private void RegisterKeyChangeCallback()
		{
			if (Application.isPlaying && userData != null && !string.IsNullOrEmpty("Language"))
			{
				userData.RegisterOnChange("Language", OnLanguageChanged);
			}
		}

		private void UnregisterKeyChangeCallback()
		{
			if (userData != null)
			{
				userData.UnregisterOnChange("Language", OnLanguageChanged);
			}
		}

		private void OnLanguageChanged(string newValue)
		{
			UpdateText();
		}


		public void UpdateText()
		{
			EnsureComponent();

			if (dataStore == null || userData == null)
			{
				textComponent.text = "[Missing DataStore/UserData]";
				return;
			}

			if (string.IsNullOrEmpty(sheet) || string.IsNullOrEmpty(key))
			{
				textComponent.text = "[Missing Key]";
				return;
			}

			string lang = userData.Get("Language");
			if (string.IsNullOrEmpty(lang))
			{
				textComponent.text = "[Missing Language]";
				return;
			}

			string localizedText = dataStore.GetFieldValueByKey(sheet, "Key", key, lang);

			if (!string.IsNullOrEmpty(localizedText))
			{
				localizedText = ReplacePlaceholders(localizedText);
				textComponent.text = localizedText;
			}
			else
			{
				textComponent.text = $"[{key}]";
			}


			//textComponent.text = !string.IsNullOrEmpty(localizedText)
			//	? localizedText
			//	: $"[{key}]";
		}

		private string ReplacePlaceholders(string text)
		{
			if (string.IsNullOrEmpty(text) || userData == null)
				return text;

			// Matches {SomeKey} anywhere in the string
			var matches = System.Text.RegularExpressions.Regex.Matches(text, @"\{([^\{\}]+)\}");

			foreach (System.Text.RegularExpressions.Match match in matches)
			{
				string placeholder = match.Groups[0].Value; // e.g., "{User}"
				string key = match.Groups[1].Value;         // e.g., "User"
				string replacement = userData.Get(key);

				if (!string.IsNullOrEmpty(replacement))
				{
					text = text.Replace(placeholder, replacement);
				}
			}

			return text;
		}

	}
}
