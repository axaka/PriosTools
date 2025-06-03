using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PriosTools
{
	[RequireComponent(typeof(TMP_Dropdown))]
	public class PriosLanguageDropdown : MonoBehaviour
	{
		public PriosUserData userData;
		public PriosDataStore dataStore;
		public string userDataKey = "Language"; // default key in userData
		public string dataStoreSheet = "Translation"; // default key in dataStore

		public TMP_Dropdown dropdown;

		private void Awake()
		{
			EnsureDropdown();
			InitializeOptions();
			ApplySavedSelection();

			dropdown.onValueChanged.AddListener(OnLanguageChanged);
		}

		private void OnDestroy()
		{
			if (dropdown != null)
				dropdown.onValueChanged.RemoveListener(OnLanguageChanged);
		}

#if UNITY_EDITOR
		public void OnValidate()
		{
			if (!Application.isPlaying)
			{
				EnsureDropdown();
				InitializeOptions();
			}
		}
#endif

		private void EnsureDropdown()
		{
			if (dropdown == null)
				dropdown = GetComponent<TMP_Dropdown>();
		}

		private void InitializeOptions()
		{
			if (!this || dropdown == null || userData == null || dataStore == null) return;

			var langs = GetLanguageColumnsFromTypeName(dataStoreSheet);

			dropdown.ClearOptions();
			dropdown.AddOptions(langs);
		}

		private void ApplySavedSelection()
		{
			if (!this || dropdown == null || userData == null || dataStore == null) return;

			string savedValue = userData.Get(userDataKey);
			if (string.IsNullOrEmpty(savedValue)) return;

			// Try exact match first
			int index = dropdown.options.FindIndex(x => x.text == savedValue);

			// If not found, try case-insensitive match
			if (index < 0)
				index = dropdown.options.FindIndex(x => x.text.Equals(savedValue, StringComparison.OrdinalIgnoreCase));

			// Fallback to first option if still not found
			if (index < 0 && dropdown.options.Count > 0)
				index = 0;

			if (index >= 0)
				dropdown.SetValueWithoutNotify(index);
		}

		private void OnLanguageChanged(int index)
		{
			if (!this || dropdown == null || userData == null || dataStore == null) return;

			string selectedLang = dropdown.options[index].text;
			userData.Set(userDataKey, selectedLang);
		}

		public List<string> GetLanguageColumnsFromTypeName(string typeName)
		{
			if (!dataStore.TypedLookup.Any())
			{
				Debug.LogWarning("[PriosDataStore] TypedLookup is empty. Ensure data has been loaded.");
				return new List<string>();
			}

			// Try to find the type from the registered types
			var match = dataStore.TypedLookup.Keys.FirstOrDefault(t => t.Name == typeName);
			if (match == null)
			{
				Debug.LogWarning($"[PriosDataStore] Type '{typeName}' not found in TypedLookup.");
				return new List<string>();
			}

			try
			{
				var method = match.GetMethod("GetFieldNames", BindingFlags.Public | BindingFlags.Static);
				if (method != null)
				{
					var result = method.Invoke(null, null) as List<string>;
					if (result != null)
					{
						// Skip the first column if it's assumed to be a key
						return result.Skip(1).ToList();
					}
				}

				Debug.LogWarning($"[PriosDataStore] Could not invoke GetFieldNames() on type '{typeName}'.");
				return new List<string>();
			}
			catch (Exception ex)
			{
				Debug.LogError($"[PriosDataStore] Reflection error on '{typeName}': {ex.Message}");
				return new List<string>();
			}
		}
	}
}
