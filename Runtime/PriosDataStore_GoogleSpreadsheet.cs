using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace PriosTools
{
    public class PriosDataStore_GoogleSpreadsheet
    {

		public async Task<string> DownloadString(string url)
		{
			using var client = new HttpClient();
			return await client.GetStringAsync(url);
		}
		public async Task<List<string>> DownloadString(string[] urls)
		{
			using var client = new HttpClient();

			var outData = new List<string>();

			foreach (var url in urls)
			{
				var data = await client.GetStringAsync(url);
				outData.Add(data);
			}
			return outData;
		}

		// https://docs.google.com/spreadsheets/d/1GsTBVi3-94PmEKTyDSvE8_gyUhQYV0d03LP72F1odYc/preview
		// https://docs.google.com/spreadsheets/d/1GsTBVi3-94PmEKTyDSvE8_gyUhQYV0d03LP72F1odYc/edit
		public Dictionary<string, string> ExtractSpreadsheetInfo(string html)
		{
			var matches = Regex.Matches(html, @"items\.push\(\{name:\s*""(.*?)"",\s*pageUrl:.*?gid=(\d+)", RegexOptions.Singleline);
			var data = new Dictionary<string, string>();

			foreach (Match match in matches)
			{
				data.Add(match.Groups[1].Value, match.Groups[2].Value);
			}

			return data;
		}

		//public async Task DownloadSheetDataAsync(string spreadsheetId, Dictionary<string, string> sheets)
		//{
		//	using var client = new HttpClient();

		//	foreach (var sheet in sheets)
		//	{
		//		string name = sheet.Key;
		//		string gid = sheet.Value;

		//		string url = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={gid}";

		//		try
		//		{
		//			string csv = await client.GetStringAsync(url);
		//			File.WriteAllText($"{name}.csv", csv);
		//			Debug.Log($"Saved sheet '{name}' to {name}.csv");
		//		}
		//		catch (Exception ex)
		//		{
		//			Debug.LogError($"Failed to download sheet {name}: {ex.Message}");
		//		}
		//	}
		//}



	}
}
