using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PriosTools
{
	public class PriosDataStore_Json : IPriosDataSourceHandler
	{
		public string SourceType => ".json";

		public bool CanHandle(string url)
		{
			return url.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
		}

		public async Task<List<PriosDataStore.RawDataEntry>> FetchDataAsync(string url)
		{
			using var client = new HttpClient();
			string json = await client.GetStringAsync(url);

			string csv = ConvertJsonToCsv(json);

			return new List<PriosDataStore.RawDataEntry>
			{
				new PriosDataStore.RawDataEntry
				{
					Name = GenerateNameFromUrl(url),
					Gid = "0",
					CSV = csv
				}
			};
		}

		private string GenerateNameFromUrl(string url)
		{
			var fileName = System.IO.Path.GetFileNameWithoutExtension(url);
			fileName = Regex.Replace(fileName, @"[^a-zA-Z0-9_]", "_"); // Replace illegal chars
			if (!char.IsLetter(fileName, 0))
				fileName = "Json_" + fileName;

			// Optionally convert to PascalCase
			fileName = char.ToUpperInvariant(fileName[0]) + fileName.Substring(1);
			return fileName;
		}


		private string ConvertJsonToCsv(string json)
		{
			var sb = new System.Text.StringBuilder();

			var token = JToken.Parse(json);
			List<Dictionary<string, string>> rows = new();

			if (token.Type == JTokenType.Array)
			{
				foreach (var obj in token)
				{
					if (obj is JObject jObj)
						rows.Add(Flatten(jObj));
				}
			}
			else if (token.Type == JTokenType.Object)
			{
				rows.Add(Flatten((JObject)token));
			}

			if (rows.Count == 0) return "";

			var headers = rows[0].Keys.ToList();
			var usedNames = new HashSet<string>();
			var typeHeader = new List<string>();
			var nameHeader = new List<string>();

			for (int i = 0; i < headers.Count; i++)
			{
				string raw = headers[i];
				string name = PriosCsvTools.ValidateName(raw, $"Col{i}");
				string baseName = name;
				int suffix = 1;
				while (usedNames.Contains(name))
					name = $"{baseName}_{suffix++}";
				usedNames.Add(name);

				var columnValues = rows.Select(row => row.ContainsKey(raw) ? row[raw] : "").ToList();
				string type = PriosCsvTools.InferTypeWithNullCheck(columnValues);

				typeHeader.Add($"{type} {name}");
				nameHeader.Add(raw);
			}

			sb.AppendLine(string.Join(",", typeHeader));

			foreach (var row in rows)
			{
				var values = nameHeader.ConvertAll(h => EscapeCsv(row.ContainsKey(h) ? row[h] : ""));
				sb.AppendLine(string.Join(",", values));
			}

			return sb.ToString();
		}


		private Dictionary<string, string> Flatten(JObject obj, string prefix = "")
		{
			var dict = new Dictionary<string, string>();

			foreach (var prop in obj.Properties())
			{
				string key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
				switch (prop.Value.Type)
				{
					case JTokenType.Object:
						var nested = Flatten((JObject)prop.Value, key);
						foreach (var kvp in nested)
							dict[kvp.Key] = kvp.Value;
						break;

					case JTokenType.Array:
						var items = prop.Value.Select(x => x.Type == JTokenType.String ? x.ToString() : JsonConvert.SerializeObject(x)).ToArray();
						dict[key] = string.Join(";", items);
						break;

					default:
						dict[key] = prop.Value.ToString();
						break;
				}
			}

			return dict;
		}


		private string EscapeCsv(string value)
		{
			if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
				return $"\"{value.Replace("\"", "\"\"")}\"";
			return value;
		}

		public void OpenInBrowser(string url)
		{
			Application.OpenURL(url);
		}

		public Task<(List<string> types, List<string> names)> ExtractTypesAndNamesAsync(string csv)
		{
			var parsed = PriosCsvTools.Parse(csv);
			if (parsed.Count < 2)
				return Task.FromResult((new List<string>(), new List<string>()));

			var header = parsed[0];
			var sampleRow = parsed[1];

			var types = new List<string>();
			var names = new List<string>();
			var usedNames = new HashSet<string>();

			for (int i = 0; i < header.Count; i++)
			{
				string raw = header[i].Trim();
				string value = i < sampleRow.Count ? sampleRow[i].Trim() : "";
				var columnValues = parsed.Skip(1).Select(row => i < row.Count ? row[i] : "").ToList();
				string type = PriosCsvTools.InferTypeWithNullCheck(columnValues);
				string name = PriosCsvTools.ValidateName(raw, $"Col{i}");

				// Ensure uniqueness
				string baseName = name;
				int suffix = 1;
				while (usedNames.Contains(name))
					name = $"{baseName}_{suffix++}";

				usedNames.Add(name);
				types.Add(type);
				names.Add(name);

				Debug.Log($"[TypeInfer] {raw} → {name} : {value} => {type}");

			}

			return Task.FromResult((types, names));
		}

	}
}
