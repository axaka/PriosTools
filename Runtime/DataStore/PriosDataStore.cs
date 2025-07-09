using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;
using System.Collections;

namespace PriosTools
{
	[CreateAssetMenu(fileName = "PriosDataStore", menuName = "Data/PriosDataStore")]
	public class PriosDataStore : ScriptableObject
	{
		[SerializeField] public string Url = "";

		[SerializeField] private long _lastDownloadTicks = 0;
		public DateTime? LastDownloadedTime => _lastDownloadTicks > 0 ? new DateTime(_lastDownloadTicks, DateTimeKind.Utc) : null;

		[Serializable]
		public struct RawDataEntry
		{
			public string Name;
			public string Gid;
			[TextArea(3, 10)]
			public string CSV;
		}

		[SerializeField] private List<RawDataEntry> _rawDataEntries = new();
		public IEnumerable<(string Name, string Gid)> SheetGids => _rawDataEntries.Select(e => (e.Name, e.Gid));

		[SerializeField] private List<object> _typedLists = new();
		public IEnumerable<object> TypedLists => _typedLists;

		public Dictionary<Type, object> TypedLookup { get; private set; } = new();

		public List<string> SheetNames = new();

		public static readonly string classDir = "Assets/Scripts/DataStoreClass/";
		public static readonly string classPrefix = "PDS_";

		private static List<IPriosDataSourceHandler> _handlers;
		public IPriosDataSourceHandler CurrentHandler => 
			PriosDataStoreHandlerRegistry.GetHandlerForUrl(Url);

		private static List<IPriosDataSourceHandler> GetAvailableHandlers()
		{
			if (_handlers != null) return _handlers;

			_handlers = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a => a.GetTypes())
				.Where(t => typeof(IPriosDataSourceHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
				.Select(t => (IPriosDataSourceHandler)Activator.CreateInstance(t))
				.ToList();

			return _handlers;
		}



		private void OnEnable()
		{
			if (_typedLists.Count == 0 && _rawDataEntries.Count > 0)
			{
				RehydrateFromCsvs();
			}
		}

#if UNITY_EDITOR
		public async Task GenerateDataModels()
		{
			var handler = GetAvailableHandlers().FirstOrDefault(h => h.CanHandle(Url));
			if (handler == null)
			{
				Debug.LogError($"❌ No handler found for URL: {Url}");
				return;
			}

			try
			{
				_rawDataEntries.Clear();
				var entries = await handler.FetchDataAsync(Url);
				_rawDataEntries.AddRange(entries);
				_lastDownloadTicks = DateTime.UtcNow.Ticks;

				foreach (var entry in _rawDataEntries)
				{
					var className = "PDS_" + entry.Name.Replace(" ", "_");
					var parsed = PriosCsvTools.Parse(entry.CSV);
					if (parsed.Count < 2) continue;

					var header = parsed[0];
					var dataRows = parsed.Skip(1).ToList();

					var (types, names) = PriosCsvTools.ExtractTypesAndNames(header, dataRows);

					PriosCodeGenerator.GenerateCsClass(className, types, names);
				}

				AssetDatabase.Refresh();
				Debug.Log("[PriosEditor] ✅ Classes generated.");
				EditorUtility.SetDirty(this);
			}
			catch (Exception ex)
			{
				Debug.LogError($"❌ Failed to generate models: {ex.Message}");
			}
		}
#endif



#if UNITY_EDITOR
		public void ClearGeneratedData()
		{
			if (Directory.Exists(classDir))
			{
				var targetFileNames = _rawDataEntries
					.Select(e => $"{classPrefix}{e.Name.Replace(" ", "_")}.cs")
					.ToHashSet(StringComparer.OrdinalIgnoreCase);

				var files = Directory.GetFiles(classDir, $"{classPrefix}*.cs");

				foreach (var file in files)
				{
					string fileName = Path.GetFileName(file);
					if (targetFileNames.Contains(fileName))
					{
						try
						{
							File.Delete(file);
							Debug.Log($"[PriosDataStore] 🧹 Deleted: {file}");
						}
						catch (Exception ex)
						{
							Debug.LogWarning($"[PriosDataStore] ❌ Failed to delete {file}: {ex.Message}");
						}
					}
				}
			}

			_rawDataEntries.Clear();
			_typedLists.Clear();
			TypedLookup.Clear();
			SheetNames.Clear();
			_lastDownloadTicks = 0;

			EditorUtility.SetDirty(this);
			AssetDatabase.Refresh();
			Debug.Log("[PriosDataStore] 🧼 Cleared generated data and metadata.");
		}
#endif


		public void RehydrateFromCsvs()
		{
			_typedLists.Clear();
			TypedLookup.Clear();
			SheetNames.Clear();

			foreach (var entry in _rawDataEntries)
			{
				var className = classPrefix + entry.Name.Replace(" ", "_");
				Type type = GetGeneratedType(className);
				if (type == null) continue;

				var rows = PriosCsvTools.CsvToRows(entry.CSV);
				var baseMethod = typeof(PriosDataBase<>).MakeGenericType(type).GetMethod("FromRows", BindingFlags.Public | BindingFlags.Static);
				var result = baseMethod.Invoke(null, new object[] { rows });

				if (result is System.Collections.IEnumerable)
				{
					var setMethod = typeof(PriosDataStore).GetMethod(nameof(SetData)).MakeGenericMethod(type);
					setMethod.Invoke(this, new object[] { result });
					SheetNames.Add(entry.Name);
				}
			}
		}

		public async Task UpdateData()
		{
			var handler = GetAvailableHandlers().FirstOrDefault(h => h.CanHandle(Url));
			if (handler == null)
			{
				Debug.LogError($"❌ No handler found for URL: {Url}");
				return;
			}

			try
			{
				_rawDataEntries.Clear();
				var entries = await handler.FetchDataAsync(Url);
				_rawDataEntries.AddRange(entries);
				_lastDownloadTicks = DateTime.UtcNow.Ticks;

				RehydrateFromCsvs();
#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
#endif
			}
			catch (Exception ex)
			{
				Debug.LogError($"❌ Failed to fetch data: {ex.Message}");
			}
		}



		public void SetData<T>(List<T> list)
		{
			TypedLookup[typeof(T)] = list;
#if UNITY_EDITOR
			if (!_typedLists.Contains(list))
				_typedLists.Add(list);
#endif
		}

		private static Type GetGeneratedType(string className)
		{
			return AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a => a.GetTypes())
				.FirstOrDefault(t => t.Name == className && t.IsClass && !t.IsAbstract);
		}

		public List<T> Get<T>() where T : PriosDataBaseNonGeneric
		{
			return TypedLookup.TryGetValue(typeof(T), out var val) ? (List<T>)val : new List<T>();
		}

		// New method using string type name
		public IList GetByTypeName(string typeName)
		{
			foreach (var pair in TypedLookup)
			{
				if (pair.Key.Name == "PDS_" + typeName)
				{
					return (IList)pair.Value;
				}
			}

			Debug.LogWarning($"Type '{typeName}' not found in PriosDataStore.");
			return null;
		}

		// Get values for a specific field inside a specific type
		public List<string> GetFieldValues(string typeName, string fieldName)
		{
			var result = new List<string>();

			var list = GetByTypeName(typeName);
			if (list == null)
				return result;

			foreach (var item in list)
			{
				var field = item.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
				if (field != null)
				{
					var value = field.GetValue(item)?.ToString();
					result.Add(value);
				}
				else
				{
					Debug.LogWarning($"Field '{fieldName}' not found in type '{typeName}'.");
					break;
				}
			}

			return result;
		}

		public string GetFirstFieldValue(string typeName, string fieldName)
		{
			var list = GetByTypeName(typeName);
			if (list == null || list.Count == 0)
				return null;

			var firstItem = list[0];
			var field = firstItem.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
			if (field == null)
			{
				Debug.LogWarning($"Field '{fieldName}' not found in type '{typeName}'.");
				return null;
			}

			return field.GetValue(firstItem)?.ToString();
		}

		/// <summary>
		/// Gets a field's value from a specific entry by matching another field (e.g., find by Translation_Key).
		/// </summary>
		public string GetFieldValueByKey(string typeName, string matchFieldName, string matchValue, string targetFieldName)
		{
			var list = GetByTypeName(typeName);
			if (list == null)
				return null;

			foreach (var item in list)
			{
				var type = item.GetType();

				var matchField = type.GetField(matchFieldName, BindingFlags.Public | BindingFlags.Instance);
				if (matchField == null) continue;

				var fieldValue = matchField.GetValue(item)?.ToString();
				if (fieldValue != matchValue) continue;

				var targetField = type.GetField(targetFieldName, BindingFlags.Public | BindingFlags.Instance);
				if (targetField == null)
				{
					Debug.LogWarning($"Target field '{targetFieldName}' not found in type '{typeName}'.");
					return null;
				}

				return targetField.GetValue(item)?.ToString();
			}

			if (Application.isPlaying)
			{
				Debug.LogWarning(
					$"No matching entry with {matchFieldName} = '{matchValue}' in type '{typeName}'."
				);
			}

			return null;
		}


	}
}