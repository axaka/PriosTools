using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Text.RegularExpressions;

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
		private static readonly Regex PlaceholderRegex = new(@"\{([^\{\}]+)\}", RegexOptions.Compiled);

		private readonly List<string> watchedKeys = new();

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

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (!IsValidInstance()) return;
			EnsureComponent();

			if (!Application.isPlaying)
			{
				EditorApplication.delayCall += () =>
				{
					if (this != null) UpdateText();
				};
			}
			else
			{
				UpdateText();
			}
		}
#else
		private void OnValidate()
		{
			if (IsValidInstance())
				UpdateText();
		}
#endif

		private void EnsureComponent()
		{
			if (textComponent == null)
				textComponent = GetComponent<TMP_Text>();
		}

		private bool IsValidInstance()
		{
			return this != null && gameObject != null;
		}

		private void RegisterKeyChangeCallback()
		{
			if (userData == null || dataStore == null || string.IsNullOrEmpty(sheet) || string.IsNullOrEmpty(key))
				return;

			UnregisterKeyChangeCallback();

			string lang = userData.Get("Language");
			string rawText = dataStore.GetFieldValueByKey(sheet, "Key", key, lang);

			if (!string.IsNullOrEmpty(rawText))
			{
				foreach (Match match in PlaceholderRegex.Matches(rawText))
				{
					string watchKey = match.Groups[1].Value;
					if (!string.IsNullOrEmpty(watchKey))
					{
						userData.RegisterOnChange(watchKey, OnWatchedKeyChanged);
						watchedKeys.Add(watchKey);
					}
				}
			}

			userData.RegisterOnChange("Language", OnWatchedKeyChanged);
			watchedKeys.Add("Language");
		}

		private void UnregisterKeyChangeCallback()
		{
			if (userData == null) return;

			foreach (var k in watchedKeys)
				userData.UnregisterOnChange(k, OnWatchedKeyChanged);

			watchedKeys.Clear();
		}

		private void OnWatchedKeyChanged(string _) => UpdateText();

		public void UpdateText()
		{
			if (!IsValidInstance()) return;
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
			textComponent.text = !string.IsNullOrEmpty(localizedText)
				? ReplacePlaceholders(localizedText)
				: $"[{key}]";
		}

		private string ReplacePlaceholders(string text)
		{
			if (string.IsNullOrEmpty(text) || userData == null)
				return text;

			return PlaceholderRegex.Replace(text, match =>
			{
				string key = match.Groups[1].Value;
				string replacement = userData.Get(key);
				return string.IsNullOrEmpty(replacement) ? match.Value : replacement;
			});
		}
	}
}
