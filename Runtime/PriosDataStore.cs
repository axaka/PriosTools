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

namespace PriosTools
{
	[CreateAssetMenu(fileName = "PriosDataStore", menuName = "Data/PriosDataStore")]
	public class PriosDataStore : ScriptableObject
	{
		public string url;

		[SerializeField]
		private string _lastHtml = "";

		[SerializeField]
		private long _lastHtmlDownloadTicks = 0;
		public DateTime? LastDownloadedTime => _lastHtmlDownloadTicks > 0 ? new DateTime(_lastHtmlDownloadTicks, DateTimeKind.Utc) : null;

		[SerializeField] private List<object> _typedLists = new();
		public IEnumerable<object> TypedLists => _typedLists;

		private Dictionary<Type, object> _typedLookup = new();
		public List<string> SheetNames = new();

		private static readonly string _classDir = "Assets/Scripts/DataStoreClass/";

		private void OnEnable()
		{
			if (_typedLists.Count == 0 && !string.IsNullOrWhiteSpace(_lastHtml))
			{
				RehydrateFromHtml(_lastHtml);
			}
		}

#if UNITY_EDITOR
		public async Task Editor_GenerateDataModels()
		{
			await DownloadHtml();

			foreach (var sheet in ParseHtmlData(_lastHtml))
			{
				var className = sheet.Key.Replace(" ", "_");
				var sample = sheet.Value.FirstOrDefault();
				if (sample?.Count == 0) continue;


				var types = sample.Select(kv => GetCSharpType(kv.Value)).ToList();
				var names = sample.Keys.ToList();

				GenerateCsClass(className, types, names);
			}

			AssetDatabase.Refresh();
			Debug.Log("[PriosDataStore] ✅ Classes generated. Wait for recompilation, then run Update Data().");
		}
#endif

		public async Task UpdateData()
		{
			await DownloadHtml();
			RehydrateFromHtml(_lastHtml);

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		public void RehydrateFromHtml(string html)
		{
			var sheetRawData = ParseHtmlData(html);
			_typedLists.Clear();
			_typedLookup.Clear();
			SheetNames.Clear();

			foreach (var sheet in sheetRawData)
			{
				string sheetName = sheet.Key;
				string className = sheetName.Replace(" ", "_");
				var rows = sheet.Value;

				Type type = GetGeneratedType(className);
				if (type == null) continue;

				var baseMethod = typeof(PriosDataBase<>).MakeGenericType(type)
					.GetMethod("FromRows", BindingFlags.Public | BindingFlags.Static);
				if (baseMethod == null) continue;

				var result = baseMethod.Invoke(null, new object[] { rows });

				if (result is System.Collections.IEnumerable)
				{
					var setMethod = typeof(PriosDataStore).GetMethod(nameof(SetData))
						.MakeGenericMethod(type);
					setMethod.Invoke(this, new object[] { result });
					SheetNames.Add(sheetName);
				}
			}
		}

		public void SetData<T>(List<T> list)
		{
			_typedLookup[typeof(T)] = list;
#if UNITY_EDITOR
			if (!_typedLists.Contains(list))
				_typedLists.Add(list);
#endif
		}

		public List<T> Get<T>()
		{
			return _typedLookup.TryGetValue(typeof(T), out var val) ? (List<T>)val : new List<T>();
		}

		public async Task DownloadHtml()
		{
			DateTime now = DateTime.UtcNow;
			DateTime? last = LastDownloadedTime;

			if (last.HasValue && (now - last.Value).TotalSeconds < 60)
			{
				Debug.LogWarning("[PriosDataStore] ⏳ Download skipped — last HTML download was less than 1 minute ago.");
				return;
			}

			Debug.Log("[PriosDataStore] Downloading data...");
			using var client = new HttpClient();
			string html = await client.GetStringAsync(url);

			if (string.IsNullOrWhiteSpace(html))
			{
				Debug.LogWarning("[PriosDataStore] ❌ Downloaded HTML is empty.");
				return;
			}

			_lastHtml = html;
			_lastHtmlDownloadTicks = DateTime.UtcNow.Ticks;

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		public static Dictionary<string, List<Dictionary<string, object>>> ParseHtmlData(string html)
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			var sheetMap = new Dictionary<string, string>();
			var menu = doc.DocumentNode.SelectNodes("//ul[@id='sheet-menu']/li");
			if (menu != null)
			{
				foreach (var li in menu)
				{
					var a = li.SelectSingleNode("./a");
					if (a == null) continue;
					var id = li.Id.Replace("sheet-button-", "");
					sheetMap[id] = a.InnerText.Trim();
				}
			}

			var result = new Dictionary<string, List<Dictionary<string, object>>>();

			foreach (var kv in sheetMap)
			{
				var id = kv.Key;
				var name = kv.Value;
				var rows = doc.DocumentNode.SelectNodes($"//div[@id='{id}']//table[contains(@class,'waffle')]//tr");
				if (rows == null) continue;

				var table = new List<List<string>>();
				foreach (var tr in rows)
				{
					var cells = tr.SelectNodes("./td");
					if (cells == null) continue;
					table.Add(cells.Select(td => td.InnerText.Trim()).ToList());
				}

				if (table.Count == 0) continue;

				var header = table[0];
				var types = new List<string>();
				var names = new List<string>();
				var seps = new List<string>();

				foreach (var cell in header)
				{
					var parts = cell.Split(' ', 2);
					string type = parts[0];
					string nameCol = parts.Length > 1 ? parts[1] : $"Col{types.Count}";
					string sep = null;
					if (type.Contains("[") && type.Contains("]"))
					{
						var start = type.IndexOf('[');
						var end = type.IndexOf(']');

						if (start >= 0 && end > start)
						{
							sep = type.Substring(start + 1, end - start - 1).Trim();
							type = type.Substring(0, start).Trim();

							if (string.IsNullOrEmpty(sep) || sep == ",")
							{
								type += "[]";
							}
						}
					}

					types.Add(type);
					names.Add(nameCol);
					seps.Add(sep);
				}

				var data = new List<Dictionary<string, object>>();
				for (int i = 1; i < table.Count; i++)
				{
					var row = table[i];
					var dict = new Dictionary<string, object>();
					bool anyNonEmpty = false;

					for (int c = 0; c < names.Count && c < row.Count; c++)
					{
						var val = row[c];
						var baseType = types[c];
						var sep = seps[c];
						var parsed = sep != null ? ParseArrayValue(baseType, val, sep) : ParseSingleValue(baseType, val);

						dict[names[c]] = parsed;
						anyNonEmpty |= parsed is Array arr ? arr.Length > 0 : parsed != null && parsed.ToString().Trim() != "";
					}

					if (anyNonEmpty)
						data.Add(dict);
				}

				result[name] = data;
			}

			return result;
		}

		private static object ParseSingleValue(string type, string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;

			switch (type)
			{
				case "string": return value;
				case "int": return int.TryParse(value, out var i) ? i : null;
				case "float": return float.TryParse(value.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : null;
				case "bool": return bool.TryParse(value, out var b) ? b : null;
				case "date": return TryParseCustomDate(value);
				default: return value;
			}
		}

		private static object[] ParseArrayValue(string type, string value, string sep)
		{
			if (string.IsNullOrWhiteSpace(value)) return Array.Empty<object>();
			return value.Split(new[] { sep }, StringSplitOptions.RemoveEmptyEntries).Select(val => ParseSingleValue(type, val.Trim())).ToArray();
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
				string rawType = types[i];
				string finalType = rawType;

				int start = rawType.IndexOf('[');
				int end = rawType.IndexOf(']');

				if (start >= 0 && end > start)
					finalType = rawType.Substring(0, start).Trim() + "[]";

				sb.AppendLine($"    public {finalType} {names[i]};");
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
