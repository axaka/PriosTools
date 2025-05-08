
// Auto-generated PriosSpreadsheet.cs with full data storage and code generation

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

namespace PriosTools
{
	[CreateAssetMenu(fileName = "PriosSpreadsheet", menuName = "Data/PriosSpreadsheet")]
	public class PriosSpreadsheet : ScriptableObject
	{
		[Header("Spreadsheet Source")]
		public string fileUrl;

		[SerializeField, HideInInspector] private List<object> _typedLists = new();
		private Dictionary<Type, object> _typedLookup = new();

		public List<string> SheetNames = new();

		private static readonly string _classDir = "Assets/Scripts/JsonClass/";


#if UNITY_EDITOR
		// Editor-only
		public async Task DownloadAndGenerateClasses()
		{
			string html = await DownloadHTML(fileUrl);
			var sheetRawData = ParseHtmlData(html);

			foreach (var sheet in sheetRawData)
			{
				var className = sheet.Key.Replace(" ", "_");
				var sample = sheet.Value.FirstOrDefault();
				if (sample == null) continue;

				var types = sample.Select(kv => GetCSharpType(kv.Value)).ToList();
				var names = sample.Keys.ToList();

				GenerateCsClass(className, types, names);
			}

			UnityEditor.AssetDatabase.Refresh();
			Debug.Log("[PriosSpreadsheet] ✅ Classes generated. Wait for recompilation, then run DownloadAndApplyData().");
		}
#else
		public Task DownloadAndGenerateClasses()
		{
			Debug.LogWarning("DownloadAndGenerateClasses() is editor-only.");
			return Task.CompletedTask;
		}
#endif


		public async Task DownloadAndApplyData()
		{
			Debug.Log("[PriosSpreadsheet] Downloading data...");
			string html = await DownloadHTML(fileUrl);
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
				if (type == null)
				{
					Debug.LogWarning($"[PriosSpreadsheet] ⚠️ No compiled class found for '{className}'. Skipping.");
					continue;
				}

				var baseMethod = typeof(PriosJsonSheetBase<>)
				.MakeGenericType(type)
				.GetMethod("FromRows", BindingFlags.Public | BindingFlags.Static);

				if (baseMethod == null)
				{
					Debug.LogWarning($"[PriosSpreadsheet] ❌ Could not find inherited FromRows method for {type.Name}");
					continue;
				}

				var result = baseMethod.Invoke(null, new object[] { rows });

				if (result is System.Collections.IEnumerable)
				{
					Type listType = typeof(List<>).MakeGenericType(type);
					MethodInfo setMethod = typeof(PriosSpreadsheet).GetMethod(nameof(SetData)).MakeGenericMethod(type);
					setMethod.Invoke(this, new object[] { result });
					SheetNames.Add(sheetName);
				}
			}

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
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

		public List<T> Get<T>()
		{
			return _typedLookup.TryGetValue(typeof(T), out var val) ? (List<T>)val : new List<T>();
		}

		public static async Task<string> DownloadHTML(string url)
		{
			using var client = new HttpClient();
			return await client.GetStringAsync(url);
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
						sep = type.Substring(start + 1, end - start - 1);
						type = type.Substring(0, start);
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
					for (int c = 0; c < names.Count && c < row.Count; c++)
					{
						var val = row[c];
						var baseType = types[c];
						var sep = seps[c];
						dict[names[c]] = sep != null
							? ParseArrayValue(baseType, val, sep)
							: ParseSingleValue(baseType, val);
					}
					data.Add(dict);
				}

				result[name] = data;
			}

			return result;
		}

		private static object ParseSingleValue(string type, string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;
			return type switch
			{
				"string" => value,
				"int" => int.TryParse(value, out var i) ? i : null,
				"float" => float.TryParse(value.Replace(",", "."), System.Globalization.NumberStyles.Any,
							 System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : null,
				"bool" => bool.TryParse(value, out var b) ? b : null,
				_ => value
			};
		}

		private static object[] ParseArrayValue(string type, string value, string sep)
		{
			if (string.IsNullOrWhiteSpace(value)) return Array.Empty<object>();
			return value.Split(new[] { sep }, StringSplitOptions.RemoveEmptyEntries)
				.Select(val => ParseSingleValue(type, val.Trim()))
				.ToArray();
		}

		private static string GetCSharpType(object value)
		{
			return value switch
			{
				string => "string",
				int => "int?",
				float or double => "float?",
				bool => "bool?",
				object[] arr when arr.Length > 0 => GetCSharpType(arr[0]) + "[]",
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
			sb.AppendLine($"public class {className} : PriosJsonSheetBase<{className}>");
			sb.AppendLine("{");

			for (int i = 0; i < types.Count; i++)
				sb.AppendLine($"    public {types[i]} {names[i]};");

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
				string fieldName = names[i];
				sb.AppendLine($"            $\"{fieldName}: {{{fieldName}}}\"{(i < names.Count - 1 ? "," : "")}");
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
