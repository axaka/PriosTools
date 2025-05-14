using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using HtmlAgilityPack;
using System.IO;
using System.Text;
using NodaTime;
using NodaTime.Text;
using UnityEditor;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor.PackageManager;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace PriosTools
{
	[CreateAssetMenu(fileName = "PriosDataStore", menuName = "Data/PriosDataStore")]
	public class PriosDataStore : ScriptableObject
	{
		[SerializeField] private string _errorMessage = "";

		[SerializeField] public string Url = "https://docs.google.com/spreadsheets/d/1GsTBVi3-94PmEKTyDSvE8_gyUhQYV0d03LP72F1odYc/edit";
		[SerializeField] private string _lastUrl = "";

		public string SpreadsheetId
		{
			get
			{
				var match = Regex.Match(Url, @"\/d\/([^\/]+)");
				if (match.Success)
					return match.Groups[1].Value;

				throw new Exception("Invalid Google Sheets URL");
			}
		}


		[SerializeField] private long _lastHtmlDownloadTicks = 0;
		public DateTime? LastDownloadedTime => _lastHtmlDownloadTicks > 0 ? new DateTime(_lastHtmlDownloadTicks, DateTimeKind.Utc) : null;

		[SerializeField] private List<RawDataEntry> _rawDataEntries = new();

		[Serializable]
		public struct RawDataEntry
		{
			public string Name;
			[TextArea(3, 10)]
			public string CSV;
		}

		private static readonly string _classDir = "Assets/Scripts/DataStoreClass/";
		private static readonly string _classPrefix = "PDS_";

		[SerializeField] private List<object> _typedLists = new();
		public IEnumerable<object> TypedLists => _typedLists;

		private Dictionary<Type, object> _typedLookup = new();
		public List<string> SheetNames = new();


		public Dictionary<string, string> ExtractSpreadsheetInfo(string html)
		{
			var matches = Regex.Matches(html, @"items\.push\(\{name:\s*""(.*?)"",\s*pageUrl:.*?gid=(\d+)", RegexOptions.Singleline);
			var data = new Dictionary<string, string>();

			foreach (Match match in matches)
			{
				data[match.Groups[1].Value] = match.Groups[2].Value;
			}

			return data;
		}

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
					string cleanedCsv = RemoveCommentColumnsFromCsv(rawCsv);

					_rawDataEntries.Add(new RawDataEntry { Name = name, CSV = cleanedCsv });
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

		private static string RemoveCommentColumnsFromCsv(string csv)
		{
			var lines = csv.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
			if (lines.Count == 0)
				return csv;

			var header = lines[0].Split(',').Select(h => h.Trim()).ToList();

			// Identify indexes to keep (not starting with "#")
			var keepIndices = new List<int>();
			for (int i = 0; i < header.Count; i++)
			{
				if (!header[i].StartsWith("#"))
					keepIndices.Add(i);
			}

			// Build new cleaned CSV
			var sb = new StringBuilder();
			foreach (var line in lines)
			{
				var cells = line.Split(',');
				var filtered = keepIndices.Select(i => i < cells.Length ? cells[i] : "").ToArray();
				sb.AppendLine(string.Join(",", filtered));
			}

			return sb.ToString();
		}

		private void OnEnable()
		{
			if (_typedLists.Count == 0 && _rawDataEntries.Count > 0)
			{
				RehydrateFromCsvs();
			}
		}


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

				var rows = CsvToRows(entry.CSV, type);
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


#if UNITY_EDITOR
		public async Task Editor_GenerateDataModels()
		{
			string previewUrl = Url.Replace("/edit", "/preview");

			string html = await new HttpClient().GetStringAsync(previewUrl);
			var sheets = ExtractSpreadsheetInfo(html);

			await DownloadSheetDataAsync(SpreadsheetId, sheets);

			foreach (var entry in _rawDataEntries)
			{
				string className = _classPrefix + entry.Name.Replace(" ", "_");

				var lines = entry.CSV.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
				if (lines.Count == 0) continue;

				var header = lines[0].Split(',').ToList();
				var dataLines = lines.Skip(1).ToList();

				var types = new List<string>();
				var names = new List<string>();

				for (int col = 0; col < header.Count; col++)
				{
					var rawHeader = header[col];
					var tokens = rawHeader.Split(' ', StringSplitOptions.RemoveEmptyEntries);

					// Extract type, name, separator
					string typePart = tokens.ElementAtOrDefault(0) ?? "string";
					string namePart = tokens.ElementAtOrDefault(1) ?? $"Col{col}";
					string sepPart = tokens.ElementAtOrDefault(2);

					var (typeHint, _) = ParseTypeAndSeparator($"{typePart} {sepPart ?? ""}".Trim());
					var name = ValidateName(namePart, $"Col{col}");

					names.Add(name);
					types.Add(typeHint);
				}



				GenerateCsClass(className, types, names);
			}

			AssetDatabase.Refresh();
			Debug.Log("[PriosDataStore] ✅ Classes generated.");
		}
#endif


		public async Task UpdateData()
		{
			string previewUrl = Url.Replace("/edit", "/preview");

			string html = await new HttpClient().GetStringAsync(previewUrl);
			var sheets = ExtractSpreadsheetInfo(html);

			await DownloadSheetDataAsync(SpreadsheetId, sheets);
			RehydrateFromCsvs();

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		private static List<Dictionary<string, object>> CsvToRows(string csv, Type type)
		{
			var lines = csv.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
			if (lines.Count < 2) return new();

			var header = lines[0].Split(',').Select(h => h.Trim()).ToList();
			var typeAndSep = header.Select(h => ParseTypeAndSeparator(h.Split(' ')[0])).ToList();
			var names = header.Select((h, i) => ValidateName(h.Split(' ').Length > 1 ? h.Split(' ')[1] : "", $"Col{i}")).ToList();

			var rows = new List<Dictionary<string, object>>();

			for (int i = 1; i < lines.Count; i++)
			{
				var row = lines[i].Split(',');
				var dict = new Dictionary<string, object>();

				for (int j = 0; j < header.Count && j < row.Length; j++)
				{
					var (typeName, sep) = typeAndSep[j];
					string val = row[j].Trim();

					object parsed = sep != null
						? ParseArrayValue(typeName, val, sep)
						: ParseSingleValue(typeName, val);

					dict[names[j]] = parsed;
				}

				rows.Add(dict);
			}

			return rows;
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

		private static (string type, string? separator) ParseTypeAndSeparator(string rawTypeSegment)
		{
			if (string.IsNullOrWhiteSpace(rawTypeSegment))
				return ("string", null);

			rawTypeSegment = rawTypeSegment.Trim();

			if (rawTypeSegment.StartsWith("#"))
				return ("#", null);

			var tokens = rawTypeSegment.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			string baseTypeToken = tokens.ElementAtOrDefault(0)?.Trim() ?? "string";
			bool isArray = baseTypeToken.EndsWith("[]");
			bool isNullable = baseTypeToken.EndsWith("?");

			string typeCore = baseTypeToken.Replace("[]", "").Replace("?", "").ToLowerInvariant();

			typeCore = typeCore switch
			{
				"int" or "integer" => "int",
				"float" or "double" => "float",
				"bool" or "boolean" => "bool",
				"string" => "string",
				"date" or "datetime" => "DateTime",
				"color" => "Color",
				_ => "string"
			};


			if (isArray) typeCore += "[]";
			if (isNullable && !typeCore.EndsWith("?") && typeCore != "string") typeCore += "?";

			string? separator = isArray && tokens.Length >= 3 ? tokens[2] : (isArray ? ";" : null);

			return (typeCore, separator);
		}

		private static string ValidateName(string name, string backupGeneratedName)
		{
			if (string.IsNullOrWhiteSpace(name))
				return backupGeneratedName;

			name = name.Trim().Trim(';'); // Remove trailing semicolon(s)

			var sb = new StringBuilder();
			if (!char.IsLetter(name[0]) && name[0] != '_')
				sb.Append('_');

			foreach (char c in name)
			{
				sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
			}

			var result = sb.ToString();

			return string.IsNullOrWhiteSpace(result) ? backupGeneratedName : result;
		}

		private static object[] ParseArrayValue(string type, string value, string sep)
		{
			if (string.IsNullOrWhiteSpace(value)) return Array.Empty<object>();

			// Get base type (e.g. int[] => int)
			string baseType = type.Replace("[]", "").Replace("?", "");

			return value
				.Split(new[] { sep }, StringSplitOptions.RemoveEmptyEntries)
				.Select(val => ParseSingleValue(baseType, val.Trim()))
				.ToArray();
		}

		private static object ParseSingleValue(string type, string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;

			switch (type)
			{
				case "string": return value;

				case "int":
					var intVal = value.Replace(",", ".").Replace("−", "-");
					return int.TryParse(intVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : null;

				case "float":
					var floatVal = value.Replace(",", ".").Replace("−", "-");
					return float.TryParse(floatVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : null;

				case "bool":
					var boolVal = value.Trim()
						.Replace("−", "-")       // Replace Unicode minus
						.Replace("\u00A0", " ")  // Replace non-breaking space
						.ToLowerInvariant();

					switch (boolVal)
					{
						case "1":
						case "yes":
						case "y":
						case "true":
							return true;

						case "0":
						case "no":
						case "n":
						case "false":
							return false;

						default:
							return null;
					}

				case "date": return TryParseCustomDate(value);

				case "color": return ParseColor(value);

				default: return value;
			}
		}

		private static Color? ParseColor(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;

			value = value.Trim().ToLowerInvariant();

			// Handle named colors
			switch (value)
			{
				case "red": return Color.red;
				case "green": return Color.green;
				case "blue": return Color.blue;
				case "black": return Color.black;
				case "white": return Color.white;
				case "yellow": return Color.yellow;
				case "cyan": return Color.cyan;
				case "magenta": return Color.magenta;
				case "gray":
				case "grey": return Color.grey;
			}

			// Normalize hex string (with or without #)
			if (Regex.IsMatch(value, @"^#?[0-9a-f]{3,8}$"))
			{
				if (!value.StartsWith("#"))
					value = "#" + value;

				// Expand shorthand #rgb to #rrggbb
				if (value.Length == 4)
				{
					value = "#" + string.Concat(value.Skip(1).Select(c => $"{c}{c}"));
				}

				// Append full alpha if missing
				if (value.Length == 7)
					value += "FF";

				if (ColorUtility.TryParseHtmlString(value, out var htmlColor))
					return htmlColor;
			}

			// Handle comma-separated RGB(A)
			var parts = value.Split(',');
			if (parts.Length == 3 || parts.Length == 4)
			{
				if (parts.All(p => byte.TryParse(p.Trim(), out _)))
				{
					byte r = byte.Parse(parts[0].Trim());
					byte g = byte.Parse(parts[1].Trim());
					byte b = byte.Parse(parts[2].Trim());
					byte a = parts.Length == 4 ? byte.Parse(parts[3].Trim()) : (byte)255;
					return new Color32(r, g, b, a);
				}
			}

			Debug.LogWarning($"[ParseColor] ⚠️ Could not parse color value: '{value}'");
			return null;
		}


		private static DateTime? TryParseCustomDate(string input)
		{
			input = input.Replace("kl.", "", StringComparison.OrdinalIgnoreCase).Replace("  ", " ").Trim();

			try
			{
				var patterns = new[]
				{
					"dd.MM.yyyy HH.mm.ss",
					"dd.MM.yyyy HH:mm:ss",
					"yyyy-MM-dd HH:mm:ss",
					"M/d/yyyy h:mm:ss tt",
					"d.M.yyyy HH:mm"
				};

				// Extract potential timezone suffix
				var tokens = input.Split(' ');
				var last = tokens.Last();
				string timeZone = "UTC";

				// Very basic mapping, extend as needed
				var zoneMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					["CET"] = "Europe/Oslo",
					["CEST"] = "Europe/Oslo",
					["UTC"] = "UTC",
					["PST"] = "America/Los_Angeles",
					["EST"] = "America/New_York"
				};

				if (zoneMap.ContainsKey(last))
				{
					input = string.Join(" ", tokens.Take(tokens.Length - 1));
					timeZone = zoneMap[last];
				}

				var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZone) ?? NodaTime.DateTimeZoneProviders.Tzdb["UTC"];

				foreach (var fmt in patterns)
				{
					var pattern = LocalDateTimePattern.CreateWithInvariantCulture(fmt);
					var parseResult = pattern.Parse(input);
					if (parseResult.Success)
						return parseResult.Value.InZoneLeniently(zone).ToDateTimeUtc();
				}

			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[PriosDataStore] ⚠️ Error parsing date with NodaTime: '{input}'{ex.Message}");
			}

			Debug.LogWarning($"[PriosDataStore] ⚠️ Failed to parse date: '" +
				$"{input}'Supported formats include:" +
				$"- dd.MM.yyyy HH.mm.ss" +
				$"- dd.MM.yyyy HH:mm:ss" +
				$"- yyyy-MM-dd HH:mm:ss" +
				$"- M/d/yyyy h:mm:ss tt (e.g. 5/9/2025 1:24:02 AM)" +
				$"- d.M.yyyy HH:mm" +
				$"Each of the above may optionally end with a time zone suffix like 'CET', 'UTC', 'EST'.");
			return null;
		}

		private static string GetCSharpType(object value)
		{
			if (value is object[] arr && arr.Length > 0 && arr[0] != null)
			{
				string elementType = GetCSharpType(arr[0]);
				if (elementType == "string?") elementType = "string";
				if (!elementType.EndsWith("?") && elementType != "string") elementType += "?";
				return elementType + "[]";
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
			{
				sb.AppendLine($"    public {types[i]} {names[i]};");
			}

			sb.AppendLine();
			sb.AppendLine("    public override string Version => \"1.0\";");
			sb.AppendLine();
			sb.AppendLine("    public override bool IsValid() => true;");
			sb.AppendLine();
			sb.AppendLine("    public override string ToString()");
			sb.AppendLine("    {");
			sb.AppendLine("        return string.Join(\", \", new string[]");
			sb.AppendLine("        {");

			for (int i = 0; i < names.Count; i++)
			{
				sb.AppendLine($"            \"{names[i]}: {{{names[i]}}}\"{(i < names.Count - 1 ? "," : "")}");
			}

			sb.AppendLine("        });");
			sb.AppendLine("    }");
			sb.AppendLine("}");

			if (!Directory.Exists(_classDir))
				Directory.CreateDirectory(_classDir);

			string path = Path.Combine(_classDir, className + ".cs");
			File.WriteAllText(path, sb.ToString());
			Debug.Log($"[CodeGen] ✅ {path}");
		}

		private static Type GetGeneratedType(string className)
		{
			return AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a => a.GetTypes())
				.FirstOrDefault(t => t.Name == className && t.IsClass && !t.IsAbstract);
		}
	}
}
