using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

namespace PriosTools
{
	[CreateAssetMenu(fileName = "PriosUserData", menuName = "Data/PriosUserData")]
	public class PriosUserData : ScriptableObject
	{
		[SerializeField] private string _playerPrefKey = "PriosPlayerData";

		private Dictionary<string, Action<string>> _changeListeners = new();


		[SerializeField, Tooltip("Define user settings keys and default values.")]
		private Entry[] _data;

		[Serializable]
		public class Entry
		{
			[Tooltip("Key used to access the setting at runtime")]
			public string Key;

			[Tooltip("Default value used if no value is stored")]
			public string Default;
		}


		private Dictionary<string, string> _runtimeData = null;

		/// <summary>
		/// Register a callback for when a specific key changes.
		/// </summary>
		public void RegisterOnChange(string key, Action<string> callback)
		{
			if (!_changeListeners.ContainsKey(key))
				_changeListeners[key] = null;

			_changeListeners[key] += callback;
		}

		/// <summary>
		/// Unregister a previously registered callback.
		/// </summary>
		public void UnregisterOnChange(string key, Action<string> callback)
		{
			if (_changeListeners.ContainsKey(key))
				_changeListeners[key] -= callback;
		}

		/// <summary>
		/// Gets the value for a key. Automatically loads if not already loaded.
		/// </summary>
		public string Get(string key)
		{
			EnsureLoaded();

			if (_runtimeData.TryGetValue(key, out string value))
			{
				return value;
			}

			Debug.LogWarning($"Key '{key}' not found in PriosUserData.");
			return null;
		}

		/// <summary>
		/// Sets a value and immediately saves to PlayerPrefs.
		/// </summary>
		public void Set(string key, string value)
		{
			EnsureLoaded();

			if (_runtimeData.ContainsKey(key))
			{
				bool changed = _runtimeData[key] != value;
				_runtimeData[key] = value;

				if (changed && _changeListeners.TryGetValue(key, out var listener) && listener != null)
				{
					listener.Invoke(value);
				}

				Save();
			}
			else
			{
				Debug.LogWarning($"Key '{key}' not defined in PriosUserData. Ignoring.");
			}
		}


		/// <summary>
		/// Loads values from PlayerPrefs (or defaults).
		/// </summary>
		public void Load()
		{
			_runtimeData = new Dictionary<string, string>();

			string json = PlayerPrefs.GetString(_playerPrefKey, "");
			if (!string.IsNullOrEmpty(json))
			{
				try
				{
					_runtimeData = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
				}
				catch (Exception ex)
				{
					Debug.LogError($"Failed to parse PriosUserData from PlayerPrefs: {ex.Message}");
				}
			}

			// Safeguard against null _data
			if (_data != null)
			{
				foreach (var entry in _data)
				{
					if (!_runtimeData.ContainsKey(entry.Key))
						_runtimeData[entry.Key] = entry.Default;
				}
			}
		}


		/// <summary>
		/// Saves current data to PlayerPrefs using JSON.
		/// </summary>
		public void Save()
		{
			if (_runtimeData == null) return;

			string json = JsonConvert.SerializeObject(_runtimeData);
			PlayerPrefs.SetString(_playerPrefKey, json);
			PlayerPrefs.Save();
		}

		/// <summary>
		/// Resets stored data and reloads defaults.
		/// </summary>
		public void ResetToDefaults()
		{
			PlayerPrefs.DeleteKey(_playerPrefKey);
			_runtimeData = null;
			Load();
			Save();
		}

		private void EnsureLoaded()
		{
			if (_runtimeData == null)
				Load();
		}

		public IEnumerable<string> GetDefinedKeys()
		{
			return _data != null ? _data.Select(e => e.Key) : Enumerable.Empty<string>();
		}

		public string GetDefaultForKey(string key)
		{
			return _data?.FirstOrDefault(d => d.Key == key)?.Default ?? "<none>";
		}

	}
}
