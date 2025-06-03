using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PriosTools
{
	public class PriosDataStore_GoogleSpreadsheet : IPriosDataSourceHandler
	{
		public string SourceType => "Google Spreadsheet";

		public bool CanHandle(string url)
		{
			return url.Contains("docs.google.com/spreadsheets");
		}

		public async Task<List<PriosDataStore.RawDataEntry>> FetchDataAsync(string url)
		{
			var entries = new List<PriosDataStore.RawDataEntry>();

			string spreadsheetId = ExtractSpreadsheetId(url);
			if (string.IsNullOrEmpty(spreadsheetId))
				throw new Exception("Invalid Google Sheets URL");

			string previewUrl = url.Replace("/edit", "/preview");
			string html = await PriosWebTools.DownloadText(previewUrl);
			if (string.IsNullOrEmpty(html))
				throw new Exception("Failed to load spreadsheet preview page");

			var sheets = ExtractSpreadsheetInfo(html);

			foreach (var (name, gid) in sheets)
			{
				try
				{
					string csvUrl = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={gid}";
					string csv = await PriosWebTools.DownloadText(csvUrl);

					entries.Add(new PriosDataStore.RawDataEntry
					{
						Name = name,
						Gid = gid,
						CSV = csv
					});

					Debug.Log($"✅ Downloaded: {name}");
				}
				catch (Exception ex)
				{
					Debug.LogError($"❌ Failed to download {name}: {ex.Message}");
				}
			}

			return entries;
		}

		private string ExtractSpreadsheetId(string url)
		{
			var match = Regex.Match(url, @"\/d\/([^\/]+)");
			return match.Success ? match.Groups[1].Value : null;
		}

		public static Dictionary<string, string> ExtractSpreadsheetInfo(string html)
		{
			var matches = Regex.Matches(html, @"items\.push\(\{name:\s*""(.*?)"",\s*pageUrl:.*?gid=(\d+)", RegexOptions.Singleline);
			var data = new Dictionary<string, string>();

			foreach (Match match in matches)
			{
				data[match.Groups[1].Value] = match.Groups[2].Value;
			}

			return data;
		}

		public void OpenInBrowser(string url)
		{
			Application.OpenURL(url);
		}
	}
}
