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

namespace PriosTools
{
	[CreateAssetMenu(fileName = "PriosDataStore", menuName = "Data/PriosDataStore")]
	public class PriosDataStore : ScriptableObject
	{
		[SerializeField] public string Url = "";

		public string SpreadsheetId
		{
			get
			{
				var match = Regex.Match(Url, @"^https:\/\/docs\.google\.com\/spreadsheets\/d\/([a-zA-Z0-9-_]+)", RegexOptions.IgnoreCase);
				return match.Success ? match.Groups[1].Value : null;
			}
		}

		[SerializeField] private long _lastHtmlDownloadTicks = 0;
		public DateTime? LastDownloadedTime => _lastHtmlDownloadTicks > 0 ? new DateTime(_lastHtmlDownloadTicks, DateTimeKind.Utc) : null;

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

		private Dictionary<Type, object> _typedLookup = new();
		public List<string> SheetNames = new();

		private static readonly string _classDir = "Assets/Scripts/DataStoreClass/";
		private static readonly string _classPrefix = "PDS_";

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
			string previewUrl = Url.Replace("/edit", "/preview");
			string html = await new HttpClient().GetStringAsync(previewUrl);

			_lastHtmlDownloadTicks = DateTime.UtcNow.Ticks; // Optional utility to update timestamp
			var sheets = PriosCsvTools.ExtractSpreadsheetInfo(html);

			await DownloadSheetDataAsync(SpreadsheetId, sheets);

			foreach (var entry in _rawDataEntries)
			{
				var className = "PDS_" + entry.Name.Replace(" ", "_");
				var parsed = PriosCsvTools.Parse(entry.CSV);
				if (parsed.Count < 1) continue;

				var header = parsed[0];
				var (types, names) = PriosCsvTools.ExtractTypesAndNames(header);

				PriosCodeGenerator.GenerateCsClass(className, types, names);
			}

			AssetDatabase.Refresh();
			Debug.Log("[PriosEditor] ✅ Classes generated.");
		}
#endif

		public async Task DownloadSheetDataAsync(string spreadsheetId, Dictionary<string, string> sheets)
		{
			using var client = new HttpClient();
			_rawDataEntries.Clear();

			foreach (var sheet in sheets)
			{
				string name = sheet.Key;
				string gid = sheet.Value;
				string url = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={gid}";

				try
				{
					string rawCsv = await client.GetStringAsync(url);
					_rawDataEntries.Add(new RawDataEntry { Name = name, Gid = gid, CSV = rawCsv });
					Debug.Log($"✅ Saved sheet '{name}' to RawDataEntries.");
				}
				catch (Exception ex)
				{
					Debug.LogError($"❌ Failed to download sheet {name}: {ex.Message}");
				}
			}

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

#if UNITY_EDITOR
		public void ClearGeneratedData()
		{
			if (Directory.Exists(_classDir))
			{
				var files = Directory.GetFiles(_classDir, $"{_classPrefix}*.cs");
				foreach (var file in files)
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

			_rawDataEntries.Clear();
			_typedLists.Clear();
			_typedLookup.Clear();
			SheetNames.Clear();
			_lastHtmlDownloadTicks = 0;

			EditorUtility.SetDirty(this);
			AssetDatabase.Refresh();
			Debug.Log("[PriosDataStore] 🧼 Cleared generated data and metadata.");
		}
#endif

		public void RehydrateFromCsvs()
		{
			_typedLists.Clear();
			_typedLookup.Clear();
			SheetNames.Clear();

			foreach (var entry in _rawDataEntries)
			{
				var className = _classPrefix + entry.Name.Replace(" ", "_");
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
			string previewUrl = Url.Replace("/edit", "/preview");
			string html = await new HttpClient().GetStringAsync(previewUrl);
			_lastHtmlDownloadTicks = DateTime.UtcNow.Ticks;

			var sheets = PriosCsvTools.ExtractSpreadsheetInfo(html);
			await DownloadSheetDataAsync(SpreadsheetId, sheets);
			RehydrateFromCsvs();

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		public void SetData<T>(List<T> list)
		{
			_typedLookup[typeof(T)] = list;
#if UNITY_EDITOR
			if (!_typedLists.Contains(list))
				_typedLists.Add(list);
#endif
		}

		public List<T> Get<T>() where T : PriosDataBaseNonGeneric
		{
			return _typedLookup.TryGetValue(typeof(T), out var val) ? (List<T>)val : new List<T>();
		}

		private static Type GetGeneratedType(string className)
		{
			return AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a => a.GetTypes())
				.FirstOrDefault(t => t.Name == className && t.IsClass && !t.IsAbstract);
		}
	}
}