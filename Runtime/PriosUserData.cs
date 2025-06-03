using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SimpleJSON;

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

		public void RegisterOnChange(string key, Action<string> callback)
		{
			if (!_changeListeners.ContainsKey(key))
				_changeListeners[key] = null;

			_changeListeners[key] += callback;
		}

		public void UnregisterOnChange(string key, Action<string> callback)
		{
			if (_changeListeners.ContainsKey(key))
				_changeListeners[key] -= callback;
		}

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

		public void Load()
		{
			_runtimeData = new Dictionary<string, string>();

			string json = PlayerPrefs.GetString(_playerPrefKey, "");
			if (!string.IsNullOrEmpty(json))
			{
				try
				{
					var jsonObj = JSON.Parse(json);
					if (jsonObj != null)
					{
						foreach (var kvp in jsonObj)
						{
							_runtimeData[kvp.Key] = kvp.Value.Value;
						}
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"Failed to parse PriosUserData from PlayerPrefs: {ex.Message}");
				}
			}

			if (_data != null)
			{
				foreach (var entry in _data)
				{
					if (!_runtimeData.ContainsKey(entry.Key))
						_runtimeData[entry.Key] = entry.Default;
				}
			}
		}

		public void Save()
		{
			if (_runtimeData == null) return;

			var jsonObj = new JSONObject();
			foreach (var kvp in _runtimeData)
			{
				jsonObj[kvp.Key] = kvp.Value;
			}

			PlayerPrefs.SetString(_playerPrefKey, jsonObj.ToString());
			PlayerPrefs.Save();
		}

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
