using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
			if (!IsValidInstance()) return;
			EnsureComponent();
			RegisterKeyChangeCallback();
			UpdateText();
		}

		private void OnEnable()
		{
			if (!IsValidInstance()) return;
			EnsureComponent();
			RegisterKeyChangeCallback();
			UpdateText();
		}

		private void OnDisable()
		{
			if (!IsValidInstance()) return;
			UnregisterKeyChangeCallback();
		}

		private void OnValidate()
		{
			if (!IsValidInstance()) return;
			EnsureComponent();

			if (!Application.isPlaying)
				EditorApplication.delayCall += () =>
				{
					if (this) UpdateText();
				};
		}

		private void EnsureComponent()
		{
			if (textComponent == null)
				textComponent = GetComponent<TMP_Text>();
		}


		private bool IsValidInstance()
		{
			return this != null && gameObject != null;
		}

		private readonly List<string> watchedKeys = new();

		private void RegisterKeyChangeCallback()
		{
			if (userData == null || dataStore == null || string.IsNullOrEmpty(sheet) || string.IsNullOrEmpty(key))
				return;

			UnregisterKeyChangeCallback(); // Clear old keys

			// Get the current language
			string lang = userData.Get("Language");
			string rawText = dataStore.GetFieldValueByKey(sheet, "Key", key, lang);

			if (!string.IsNullOrEmpty(rawText))
			{
				var matches = System.Text.RegularExpressions.Regex.Matches(rawText, @"\{([^\{\}]+)\}");
				foreach (System.Text.RegularExpressions.Match match in matches)
				{
					string watchKey = match.Groups[1].Value;
					if (!string.IsNullOrEmpty(watchKey))
					{
						userData.RegisterOnChange(watchKey, OnWatchedKeyChanged);
						watchedKeys.Add(watchKey);
					}
				}
			}

			// Always also watch for language changes
			userData.RegisterOnChange("Language", OnWatchedKeyChanged);
			watchedKeys.Add("Language");
		}

		private void UnregisterKeyChangeCallback()
		{
			if (userData == null) return;

			foreach (var key in watchedKeys)
			{
				userData.UnregisterOnChange(key, OnWatchedKeyChanged);
			}
			watchedKeys.Clear();
		}

		private void OnWatchedKeyChanged(string _)
		{
			UpdateText();
		}

		public void UpdateText()
		{
			if (!IsValidInstance())
				return;

			EnsureComponent();

			if (textComponent == null)
				return;

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
				textComponent.text = ReplacePlaceholders(localizedText);
			}
			else
			{
				textComponent.text = $"[{key}]";
			}
		}

		private string ReplacePlaceholders(string text)
		{
			if (string.IsNullOrEmpty(text) || userData == null)
				return text;

			var matches = System.Text.RegularExpressions.Regex.Matches(text, @"\{([^\{\}]+)\}");

			foreach (System.Text.RegularExpressions.Match match in matches)
			{
				string placeholder = match.Groups[0].Value;
				string key = match.Groups[1].Value;
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
