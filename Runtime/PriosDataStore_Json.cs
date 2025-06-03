using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

namespace PriosTools
{
	public class PriosDataStore_Json : IPriosDataSourceHandler
	{
		public string SourceType => "JSON";

		public bool CanHandle(string url)
		{
			return url.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
		}

		public async Task<List<PriosDataStore.RawDataEntry>> FetchDataAsync(string url)
		{
			string json = await PriosWebTools.DownloadText(url);
			if (string.IsNullOrEmpty(json))
				throw new Exception("Failed to download JSON content.");

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
			var fileName = Path.GetFileNameWithoutExtension(url);
			fileName = Regex.Replace(fileName, @"[^a-zA-Z0-9_]", "_");
			if (!char.IsLetter(fileName[0]))
				fileName = "Json_" + fileName;

			return char.ToUpperInvariant(fileName[0]) + fileName.Substring(1);
		}

		private string ConvertJsonToCsv(string json)
		{
			var sb = new StringBuilder();
			var rows = ParseJson(json);

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

		private List<Dictionary<string, string>> ParseJson(string json)
		{
			var result = new List<Dictionary<string, string>>();
			var root = JSON.Parse(json);

			if (root is JSONArray array)
			{
				foreach (JSONNode node in array)
					result.Add(FlattenNode(node));
			}
			else if (root is JSONObject obj)
			{
				result.Add(FlattenNode(obj));
			}

			return result;
		}

		private Dictionary<string, string> FlattenNode(JSONNode node, string prefix = "")
		{
			var dict = new Dictionary<string, string>();

			foreach (var kvp in node)
			{
				string key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";
				var value = kvp.Value;

				if (value is JSONObject)
				{
					foreach (var sub in FlattenNode(value, key))
						dict[sub.Key] = sub.Value;
				}
				else if (value is JSONArray array)
				{
					var items = new List<string>();
					foreach (var item in array)
						items.Add(item.Value);
					dict[key] = string.Join(";", items);
				}
				else
				{
					dict[key] = value.Value;
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
	}
}
