using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PriosTools
{
	[CreateAssetMenu(fileName = "PriosDataStore", menuName = "Data/PriosDataStore")]
	public class PriosDataStore : ScriptableObject
	{
		[SerializeField] public string Url = "https://docs.google.com/spreadsheets/d/...";
		[SerializeField] private long _lastHtmlDownloadTicks = 0;
		[SerializeField] private List<RawDataEntry> _rawDataEntries = new();
		[SerializeField] private List<object> _typedLists = new();

		public IEnumerable<object> TypedLists => _typedLists;
		public DateTime? LastDownloadedTime => _lastHtmlDownloadTicks > 0 ? new DateTime(_lastHtmlDownloadTicks, DateTimeKind.Utc) : null;
		public IEnumerable<(string Name, string Gid)> SheetGids => _rawDataEntries.Select(e => (e.Name, e.Gid));
		public List<string> SheetNames = new();

		private Dictionary<Type, object> _typedLookup = new();

		private static readonly string _classDir = "Assets/Scripts/DataStoreClass/";
		private static readonly string _classPrefix = "PDS_";

		[Serializable]
		public struct RawDataEntry
		{
			public string Name;
			public string Gid;
			[TextArea(3, 10)]
			public string CSV;
		}

		public string SpreadsheetId
		{
			get
			{
				var match = Regex.Match(Url, @"^https:\/\/docs\.google\.com\/spreadsheets\/d\/([a-zA-Z0-9-_]+)", RegexOptions.IgnoreCase);
				return match.Success ? match.Groups[1].Value : null;
			}
		}

		private void OnEnable()
		{
			if (_typedLists.Count == 0 && _rawDataEntries.Count > 0)
			{
				RehydrateFromCsvs();
			}
		}

		public Dictionary<string, string> ExtractSpreadsheetInfo(string html)
		{
			var matches = Regex.Matches(html, @"items\.push\(\{name:\s*""(.*?)"",\s*pageUrl:.*?gid=(\d+)", RegexOptions.Singleline);
			return matches.Cast<Match>().ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
		}

		public async Task DownloadSheetDataAsync(string spreadsheetId, Dictionary<string, string> sheets)
		{
			using var client = new HttpClient();
			_rawDataEntries.Clear();

			foreach (var (name, gid) in sheets)
			{
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
				foreach (var file in Directory.GetFiles(_classDir, $"{_classPrefix}*.cs"))
				{
					try { File.Delete(file); Debug.Log($"🧹 Deleted: {file}"); }
					catch (Exception ex) { Debug.LogWarning($"❌ Could not delete {file}: {ex.Message}"); }
				}
			}

			_rawDataEntries.Clear();
			_typedLists.Clear();
			_typedLookup.Clear();
			SheetNames.Clear();
			_lastHtmlDownloadTicks = 0;

			EditorUtility.SetDirty(this);
			AssetDatabase.Refresh();
			Debug.Log("🧼 Cleared generated data and metadata.");
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
				var type = GetGeneratedType(className);
				if (type == null) continue;

				var rows = CsvToRows(entry.CSV, type);
				var method = typeof(PriosDataBase<>).MakeGenericType(type).GetMethod("FromRows", BindingFlags.Public | BindingFlags.Static);
				var result = method.Invoke(null, new object[] { rows });

				if (result is System.Collections.IEnumerable)
				{
					var setMethod = typeof(PriosDataStore).GetMethod(nameof(SetData)).MakeGenericMethod(type);
					setMethod.Invoke(this, new object[] { result });
					SheetNames.Add(entry.Name);
				}
			}
		}

#if UNITY_EDITOR
		public async Task Editor_GenerateDataModels()
		{
			string html = await new HttpClient().GetStringAsync(Url.Replace("/edit", "/preview"));
			_lastHtmlDownloadTicks = DateTime.UtcNow.Ticks;
			EditorUtility.SetDirty(this);

			var sheets = ExtractSpreadsheetInfo(html);
			await DownloadSheetDataAsync(SpreadsheetId, sheets);

			foreach (var entry in _rawDataEntries)
			{
				string className = _classPrefix + entry.Name.Replace(" ", "_");
				var lines = entry.CSV.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
				if (lines.Count == 0) continue;

				var header = lines[0].Split(',').ToList();

				var types = new List<string>();
				var names = new List<string>();

				for (int col = 0; col < header.Count; col++)
				{
					var (typeHint, namePart, _) = ParseTypeAndSeparator(header[col]);
					if (typeHint == "#") continue;
					types.Add(typeHint);
					names.Add(ValidateName(namePart, $"Col{col}"));
				}

				GenerateCsClass(className, types, names);
			}

			AssetDatabase.Refresh();
			Debug.Log("✅ Classes generated.");
		}
#endif

		public async Task UpdateData()
		{
			string html = await new HttpClient().GetStringAsync(Url.Replace("/edit", "/preview"));
			_lastHtmlDownloadTicks = DateTime.UtcNow.Ticks;

			var sheets = ExtractSpreadsheetInfo(html);
			await DownloadSheetDataAsync(SpreadsheetId, sheets);
			RehydrateFromCsvs();

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		// --- Core Helpers ---

		private static List<Dictionary<string, object>> CsvToRows(string csv, Type type)
		{
			var parsed = PriosCsvParser.Parse(csv);
			if (parsed.Count < 2) return new();

			var header = parsed[0];
			var typeMap = header.Select(ParseTypeAndSeparator).ToList();

			return parsed.Skip(1).Select(row =>
			{
				var dict = new Dictionary<string, object>();
				for (int j = 0; j < header.Count && j < row.Count; j++)
				{
					var (typeName, fieldName, sep) = typeMap[j];
					string val = row[j].Trim();

					dict[fieldName] = typeName.EndsWith("[]")
						? ParseArrayValue(typeName, val, sep)
						: ParseSingleValue(typeName, val);
				}
				return dict;
			}).ToList();
		}

		public void SetData<T>(List<T> list)
		{
			_typedLookup[typeof(T)] = list;
#if UNITY_EDITOR
			if (!_typedLists.Contains(list)) _typedLists.Add(list);
#endif
		}

		public List<T> Get<T>() where T : PriosDataBaseNonGeneric
		{
			return _typedLookup.TryGetValue(typeof(T), out var val) ? (List<T>)val : new();
		}

		// --- Parsing Utilities ---

		private static (string type, string name, string separator) ParseTypeAndSeparator(string header)
		{
			var tokens = header?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
			string baseType = tokens.ElementAtOrDefault(0) ?? "string";
			string name = tokens.ElementAtOrDefault(1) ?? "Col";
			string separator = tokens.ElementAtOrDefault(2);

			bool isArray = baseType.EndsWith("[]");
			bool isNullable = baseType.EndsWith("?");

			string typeCore = baseType.Replace("[]", "").Replace("?", "").ToLowerInvariant() switch
			{
				"int" or "integer" => "int",
				"float" or "double" => "float",
				"bool" or "boolean" => "bool",
				"date" or "datetime" => "DateTime",
				"color" => "Color",
				_ => "string"
			};

			if (isArray) typeCore += "[]";
			if (isNullable && !typeCore.EndsWith("?") && typeCore != "string") typeCore += "?";
			if (isArray && string.IsNullOrEmpty(separator)) separator = ",";

			return (typeCore, name, isArray ? separator : null);
		}

		private static object[] ParseArrayValue(string type, string value, string sep)
		{
			if (string.IsNullOrWhiteSpace(value)) return Array.Empty<object>();
			string baseType = type.Replace("[]", "").Replace("?", "");
			return value.Split(new[] { sep }, StringSplitOptions.None)
				.Select(v => ParseSingleValue(baseType, v.Trim()))
				.ToArray();
		}

		private static object ParseSingleValue(string type, string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;
			type = type?.TrimEnd('?');

			return type switch
			{
				"string" => value,
				"int" => int.TryParse(value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : null,
				"float" => float.TryParse(value.Replace(",", ".").Replace("−", "-"), NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : null,
				"bool" => value.Trim().ToLowerInvariant() switch
				{
					"1" or "yes" or "y" or "true" => true,
					"0" or "no" or "n" or "false" => false,
					_ => null
				},
				"DateTime" => TryParseCustomDate(value),
				"Color" => ParseColor(value),
				_ => value
			};
		}

		private static Color? ParseColor(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;
			value = value.Trim().ToLowerInvariant();

			return value switch
			{
				"red" => Color.red,
				"green" => Color.green,
				"blue" => Color.blue,
				"black" => Color.black,
				"white" => Color.white,
				"yellow" => Color.yellow,
				"cyan" => Color.cyan,
				"magenta" => Color.magenta,
				"gray" or "grey" => Color.grey,
				_ => TryParseColorFallback(value)
			};
		}

		private static Color? TryParseColorFallback(string value)
		{
			if (!value.StartsWith("#") && Regex.IsMatch(value, @"^[0-9a-f]{6,8}$"))
				value = "#" + value;

			if (ColorUtility.TryParseHtmlString(value, out var colorHex)) return colorHex;

			var parts = value.Split(',');
			if (parts.Length is 3 or 4 && parts.All(p => byte.TryParse(p.Trim(), out _)))
			{
				byte r = byte.Parse(parts[0]);
				byte g = byte.Parse(parts[1]);
				byte b = byte.Parse(parts[2]);
				byte a = parts.Length == 4 ? byte.Parse(parts[3]) : (byte)255;
				return new Color32(r, g, b, a);
			}

			Debug.LogWarning($"⚠️ Could not parse color: '{value}'");
			return null;
		}

		private static DateTime? TryParseCustomDate(string input)
		{
			var formats = new[]
			{
				"HH:mm", "H:mm",
				"yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss",
				"dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy HH.mm.ss",
				"yyyy-MM-dd", "dd.MM.yyyy"
			};

			foreach (var fmt in formats)
			{
				if (DateTime.TryParseExact(input, fmt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
				{
					return fmt.StartsWith("H") ? DateTime.Today.Add(dt.TimeOfDay) : dt;
				}
			}

			Debug.LogWarning($"⚠️ Could not parse DateTime: '{input}'");
			return null;
		}

		private static string ValidateName(string name, string fallback)
		{
			if (string.IsNullOrWhiteSpace(name)) return fallback;

			var sb = new StringBuilder();
			if (!char.IsLetter(name[0]) && name[0] != '_') sb.Append('_');

			foreach (char c in name)
				sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');

			return string.IsNullOrWhiteSpace(sb.ToString()) ? fallback : sb.ToString();
		}

		private static string GetCSharpType(object value)
		{
			if (value is object[] arr && arr.Length > 0 && arr[0] != null)
			{
				string element = GetCSharpType(arr[0]);
				if (element == "string?") element = "string";
				if (!element.EndsWith("?") && element != "string") element += "?";
				return element + "[]";
			}

			return value switch
			{
				string => "string",
				int => "int?",
				float or double => "float?",
				bool => "bool?",
				_ => "string"
			};
		}

		public static void GenerateCsClass(string className, List<string> types, List<string> names)
		{
			var sb = new StringBuilder();
			sb.AppendLine("using System;");
			sb.AppendLine("using UnityEngine;");
			sb.AppendLine("using System.Collections.Generic;");
			sb.AppendLine("using PriosTools;");
			sb.AppendLine();
			sb.AppendLine("[Serializable]");
			sb.AppendLine($"public class {className} : PriosDataBase<{className}>");
			sb.AppendLine("{");

			for (int i = 0; i < types.Count; i++)
				sb.AppendLine($"    public {types[i]} {names[i]};");

			sb.AppendLine();
			sb.AppendLine("    public override string Version => \"1.0\";");
			sb.AppendLine("    public override bool IsValid() => true;");
			sb.AppendLine();
			sb.AppendLine("    public override string ToString()");
			sb.AppendLine("    {");
			sb.AppendLine("        return string.Join(\", \", new string[]");
			sb.AppendLine("        {");
			for (int i = 0; i < names.Count; i++)
				sb.AppendLine($"            \"{names[i]}: {{{names[i]}}}\"{(i < names.Count - 1 ? "," : "")}");
			sb.AppendLine("        });");
			sb.AppendLine("    }");
			sb.AppendLine("}");

			if (!Directory.Exists(_classDir))
				Directory.CreateDirectory(_classDir);

			File.WriteAllText(Path.Combine(_classDir, className + ".cs"), sb.ToString());
			Debug.Log($"✅ Generated: {className}");
		}

		private static Type GetGeneratedType(string className)
		{
			return AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a => a.GetTypes())
				.FirstOrDefault(t => t.Name == className && t.IsClass && !t.IsAbstract);
		}
	}
}
