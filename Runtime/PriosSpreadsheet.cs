using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using HtmlAgilityPack;
using System.IO;
using System.Text;

namespace PriosTools
{
	[CreateAssetMenu(fileName = "PriosSpreadsheet", menuName = "Data/PriosSpreadsheet")]
	public class PriosSpreadsheet : ScriptableObject
	{
		[Header("Source")]
		public string fileUrl;

		[Header("Options")]
		public bool saveJson = true;
		public bool saveClass = true;

		private static string _jsonDir = "./Assets/Resources/JsonData/";
		private static string _classDir = "./Assets/Scripts/JsonClass/";
		public static List<string> baseTypes = new()
		{
			"string", "int", "int?", "float", "float?", "bool", "bool?"
		};


		//[HideInInspector]
		public List<string> SheetNames = new();

		/// <summary>
		/// In-playmode, after Run(), this holds your up-to-date sheet data:
		/// sheetName → list of rows → columnName→parsedValue
		/// </summary>
		public Dictionary<string, List<Dictionary<string, object>>> LoadedData { get; private set; }

		/// <summary>
		/// Kick off download, parsing, optional JSON export & class generation.
		/// Also populates LoadedData for immediate runtime use.
		/// </summary>
		public async void Run()
		{
			string html = await DownloadHTML(fileUrl);
			Debug.Log("HTML content downloaded.", this);

			// Parse for runtime access
			LoadedData = ParseHtmlData(html);

			// ✅ Extract sheet names from the HTML and store them
			SheetNames = LoadedData?.Keys.ToList() ?? new List<string>();

			// Save JSON + generate code if enabled
			if (saveJson || saveClass)
				ProcessFromLoadedData(LoadedData);

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this); // force SO to update in Inspector
#endif
		}

		public void ProcessFromLoadedData(Dictionary<string, List<Dictionary<string, object>>> data)
		{
			if (data == null || data.Count == 0) return;

			if (saveJson && !Directory.Exists(_jsonDir))
				Directory.CreateDirectory(_jsonDir);
			if (saveClass && !Directory.Exists(_classDir))
				Directory.CreateDirectory(_classDir);

			foreach (var sheet in data)
			{
				string sheetName = sheet.Key.Replace(" ", "_");
				var rows = sheet.Value;

				// Save JSON
				if (saveJson)
				{
					var json = JsonConvert.SerializeObject(rows, Formatting.Indented);
					var jsonPath = Path.Combine(_jsonDir, sheetName + ".json");
					File.WriteAllText(jsonPath, json);
					Debug.Log($"📄 Saved JSON: {jsonPath}");
				}

				// Generate class if valid
				if (saveClass && rows.Count > 0)
				{
					var sample = rows[0];
					var types = new List<string>();
					var names = new List<string>();

					foreach (var kv in sample)
					{
						string fieldName = kv.Key;
						string typeName = GetCSharpType(kv.Value);
						types.Add(typeName);
						names.Add(fieldName);
					}

					if (types.All(t => baseTypes.Any(baseT => t.StartsWith(baseT))))
					{
						GenerateCsClass(sheetName, types, names);
					}
				}
			}
		}

		private static string GetCSharpType(object val)
		{
			if (val == null)
				return "string"; // or "int?" if you prefer default numeric fallback

			if (val is string) return "string";
			if (val is int) return "int?";
			if (val is float || val is double) return "float?";
			if (val is bool) return "bool?";

			if (val is object[] array)
			{
				var first = array.FirstOrDefault();
				string elementType = first != null ? GetCSharpType(first) : "string";

				// Fix for CS8632 — avoid nullable reference types like "string?"
				if (elementType == "string?")
					elementType = "string";

				return elementType + "[]";
			}

			return "string";
		}




		public static async Task<string> DownloadHTML(string URL)
		{
			using var client = new HttpClient();
			return await client.GetStringAsync(URL);
		}

		private static readonly Dictionary<string, Func<string, object>> SingleParsers = new()
		{
			{ "string", v => v ?? "" },

			{ "int", v =>
			{
				if (string.IsNullOrWhiteSpace(v)) return null;

				var normalized = v.Replace("−", "-").Trim();

				if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
					int.TryParse(normalized.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out var hex))
					return hex;

				return int.TryParse(normalized, out var i) ? i : null;
			}},

			{ "float", v =>
			{
				if (string.IsNullOrWhiteSpace(v)) return null;

				var normalized = v
					.Replace("−", "-")
					.Replace(",", ".")
					.Trim();

				return float.TryParse(
					normalized,
					System.Globalization.NumberStyles.Any,
					System.Globalization.CultureInfo.InvariantCulture,
					out var f)
					? f
					: null;
			}},

			{ "bool", v =>
			{
				if (string.IsNullOrWhiteSpace(v)) return false;

				var val = v.Trim().ToLowerInvariant();

				return val switch
				{
					"true" or "yes" or "y" or "1" => true,
					"false" or "no" or "n" or "0" => false,
					_ => bool.TryParse(val, out var b) && b
				};
			}}
		};


		public static object ParseArrayValue(string type, string value, string sep)
		{
			if (!SingleParsers.ContainsKey(type))
			{
				Debug.LogWarning($"[PriosSpreadsheet] Unsupported array base type: {type}");
				return Array.Empty<object>();
			}

			if (string.IsNullOrWhiteSpace(value))
				return Array.Empty<object>();

			var parts = value.Split(new[] { sep }, StringSplitOptions.None);

			return parts.Select(p =>
			{
				var trimmed = p.Trim();
				if (string.IsNullOrWhiteSpace(trimmed))
					return null;

				try
				{
					return SingleParsers[type](trimmed);
				}
				catch (Exception ex)
				{
					Debug.LogWarning($"[PriosSpreadsheet] Failed to parse '{trimmed}' as {type} (array item): {ex.Message}");
					return null;
				}
			}).ToArray();
		}


		public static object ParseSingleValue(string type, string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;

			if (!SingleParsers.TryGetValue(type, out var parser))
			{
				Debug.LogWarning($"[PriosSpreadsheet] Unsupported type '{type}' for value: '{value}'");
				return null;
			}

			try
			{
				return parser(value);
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[PriosSpreadsheet] Failed to parse '{value}' as {type}: {ex.Message}");
				return null;
			}
		}



		public static Dictionary<string, List<List<string>>> ParseHtmlToRawLists(string html)
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			// find sheet buttons
			var linkNodes = doc.DocumentNode.SelectNodes("//ul[@id='sheet-menu']/li");
			if (linkNodes == null)
				return new Dictionary<string, List<List<string>>>();

			// map ID → name
			var idToName = new Dictionary<string, string>();
			foreach (var li in linkNodes)
			{
				var a = li.SelectSingleNode("./a");
				if (a == null) continue;
				var sid = li.Id.Replace("sheet-button-", "");
				var name = a.InnerText.Trim();
				idToName[sid] = name;
			}

			var result = new Dictionary<string, List<List<string>>>();
			foreach (var kv in idToName)
			{
				var sid = kv.Key;
				var name = kv.Value;

				// only <td> under the .waffle table in the div#sheet
				var rows = doc.DocumentNode
					.SelectNodes($"//div[@id='{sid}']//table[contains(@class,'waffle')]//tr");
				if (rows == null) continue;

				var table = new List<List<string>>();
				foreach (var tr in rows)
				{
					var cells = tr.SelectNodes("./td");
					if (cells == null) continue;
					table.Add(cells.Select(td => td.InnerText.Trim()).ToList());
				}
				result[name] = table;
			}

			return result;
		}

		/// <summary>
		/// Parses raw lists into typed data for runtime use.
		/// </summary>
		public static Dictionary<string, List<Dictionary<string, object>>> ParseHtmlData(string html)
		{
			var raw = ParseHtmlToRawLists(html);
			var outDict = new Dictionary<string, List<Dictionary<string, object>>>();

			foreach (var kv in raw)
			{
				var name = kv.Key;
				var table = kv.Value;
				if (table.Count < 1) continue;

				// header row → types, names, separators
				var header = table[0];
				var types = new List<string>();
				var cols = new List<string>();
				var seps = new List<string>();

				foreach (var cell in header)
				{
					// "type name" → [0]=type, [1]=name
					var parts = cell.Split(' ', 2);
					var t = parts[0].Trim();
					var n = parts.Length > 1 ? parts[1].Trim() : $"Col{types.Count}";
					string sep = null;
					int start = t.IndexOf('[');
					int end = t.IndexOf(']');
					if (start >= 0 && end > start)
					{
						sep = t.Substring(start + 1, end - start - 1);
						t = t.Substring(0, start); // strip off [sep]
					}

					types.Add(t);
					cols.Add(n);
					seps.Add(sep);
				}

				var dataList = new List<Dictionary<string, object>>();
				for (int i = 1; i < table.Count; i++)
				{
					var row = table[i];
					var obj = new Dictionary<string, object>();
					for (int c = 0; c < types.Count && c < row.Count; c++)
					{
						var rawVal = row[c];
						var t = types[c];
						var colName = cols[c];
						var sep = seps[c];
						try
						{
							obj[colName] = sep != null
								? ParseArrayValue(t, rawVal, sep)
								: ParseSingleValue(t, rawVal);
						}
						catch (Exception ex)
						{
							Debug.LogWarning($"Parse error sheet '{name}', row {i}, col {colName}: {ex.Message}");
							obj[colName] = null;
						}
					}
					dataList.Add(obj);
				}

				outDict[name] = dataList;
			}

			return outDict;
		}

		/// <summary>
		/// Saves JSON files & optionally generates C# classes.
		/// </summary>
		public void ProcessSheets(string html)
		{
			var raw = ParseHtmlToRawLists(html);

			if (saveJson && !Directory.Exists(_jsonDir))
				Directory.CreateDirectory(_jsonDir);
			if (saveClass && !Directory.Exists(_classDir))
				Directory.CreateDirectory(_classDir);

			foreach (var kv in raw)
			{
				var sheetName = kv.Key.Replace(" ", "_");
				var table = kv.Value;
				if (table.Count < 1) continue;

				// header parsing
				var header = table[0];
				var types = new List<string>();
				var cols = new List<string>();
				var seps = new List<string>();

				foreach (var cell in header)
				{
					var parts = cell.Split(' ', 2);
					string fullType = parts[0].Trim(); // e.g. "string[;]"
					string name = parts.Length > 1 ? parts[1].Trim() : $"Col{types.Count}";

					string baseType = fullType;
					string separator = null;

					// Generalized [delimiter] parsing
					int start = fullType.IndexOf('[');
					int end = fullType.IndexOf(']');
					if (start >= 0 && end > start)
					{
						separator = fullType.Substring(start + 1, end - start - 1);
						baseType = fullType.Substring(0, start); // removes [delimiter]
					}

					types.Add(baseType);  // e.g. "string"
					cols.Add(name);       // e.g. "MyField"
					seps.Add(separator);  // e.g. ";" or "|" or "::"
				}


				// parse rows
				var dataList = new List<Dictionary<string, object>>();
				for (int i = 1; i < table.Count; i++)
				{
					var row = table[i];
					var obj = new Dictionary<string, object>();
					for (int c = 0; c < types.Count && c < row.Count; c++)
					{
						var rawVal = row[c];
						var t = types[c];
						var colName = cols[c];
						var sep = seps[c];
						try
						{
							obj[colName] = sep != null
								? ParseArrayValue(t, rawVal, sep)
								: ParseSingleValue(t, rawVal);
						}
						catch
						{
							obj[colName] = null;
						}
					}
					dataList.Add(obj);
				}

				// write JSON
				if (saveJson)
				{
					var json = JsonConvert.SerializeObject(dataList, Formatting.Indented);
					var path = Path.Combine(_jsonDir, sheetName + ".json");
					File.WriteAllText(path, json);
					Debug.Log($"Saved JSON: {path}");
				}

				// generate C# class
				if (saveClass && types.All(t => baseTypes.Any(baseT => t.StartsWith(baseT))))
					GenerateCsClass(sheetName, types, cols);
			}
		}

		/// <summary>
		/// Emits a [Serializable] class with public fields and static LoadJson().
		/// </summary>
		public static void GenerateCsClass(string className, List<string> types, List<string> names)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine("using System;");
			sb.AppendLine("using UnityEngine;");
			sb.AppendLine("using PriosTools;");
			sb.AppendLine();
			sb.AppendLine("[Serializable]");
			sb.AppendLine($"public class {className} : PriosJsonSheetBase<{className}>");
			sb.AppendLine("{");

			var finalTypes = new List<string>();
			var arrayFields = new List<string>();

			for (int i = 0; i < types.Count; i++)
			{
				string rawType = types[i];
				string finalType = rawType;

				int start = rawType.IndexOf('[');
				int end = rawType.IndexOf(']');
				if (start >= 0 && end > start)
				{
					finalType = rawType.Substring(0, start) + "[]";
				}

				finalTypes.Add(finalType);
				if (finalType.EndsWith("[]")) arrayFields.Add(names[i]);

				sb.AppendLine($"    public {finalType} {names[i]};");
			}

			sb.AppendLine();
			sb.AppendLine("    public override string Version => \"1.0\";");
			sb.AppendLine();
			sb.AppendLine("    public override bool IsValid()");
			sb.AppendLine("    {");
			sb.AppendLine("        // Add validation logic if needed");
			sb.AppendLine("        return true;");
			sb.AppendLine("    }");
			sb.AppendLine();
			sb.AppendLine("    public override string ToString()");
			sb.AppendLine("    {");

			// Generate helper lines for array fields
			foreach (var arr in arrayFields)
			{
				sb.AppendLine($"        string {arr}Str = {arr} != null ? string.Join(\", \", {arr}) : \"null\";");
			}

			sb.AppendLine("        return string.Join(\", \", new string[]");
			sb.AppendLine("        {");

			for (int i = 0; i < names.Count; i++)
			{
				string name = names[i];
				string type = finalTypes[i];
				bool isArray = type.EndsWith("[]");

				string line = isArray
					? $"            $\"{name}: {{{name}Str}}\""
					: $"            $\"{name}: {{{name}}}\"";

				if (i < names.Count - 1) line += ",";
				sb.AppendLine(line);
			}

			sb.AppendLine("        });");
			sb.AppendLine("    }");
			sb.AppendLine("}");

			// Save file
			if (!Directory.Exists(_classDir))
				Directory.CreateDirectory(_classDir);

			string filePath = Path.Combine(_classDir, className + ".cs");
			File.WriteAllText(filePath, sb.ToString());

			Debug.Log($"✅ Generated class: {filePath}");
		}

	}
}
