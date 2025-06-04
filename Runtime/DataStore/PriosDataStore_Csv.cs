using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PriosTools
{
	public class PriosDataStore_Csv : IPriosDataSourceHandler
	{
		public string SourceType => "CSV";

		public bool CanHandle(string url)
		{
			return url.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
		}

		public async Task<List<PriosDataStore.RawDataEntry>> FetchDataAsync(string url)
		{
			string rawCsv = await PriosWebTools.DownloadText(url);
			if (string.IsNullOrEmpty(rawCsv))
			{
				Debug.LogWarning("[CSV Handler] Failed to download or empty CSV.");
				return new List<PriosDataStore.RawDataEntry>();
			}

			var parsed = PriosCsvTools.Parse(rawCsv);
			if (parsed.Count < 2)
			{
				Debug.LogWarning("[CSV Handler] Not enough rows to infer headers.");
				return new List<PriosDataStore.RawDataEntry>();
			}

			var header = parsed[0];
			bool headerHasTypes = PriosCsvTools.HeaderLooksTyped(header);

			if (!headerHasTypes)
			{
				// Infer types from second row and replace header
				var dataRows = parsed.Skip(1).ToList();
				var (types, names) = PriosCsvTools.ExtractTypesAndNames(header, dataRows);
				var newHeader = types.Zip(names, (t, n) => $"{t} {n}").ToList();

				// Replace header line
				parsed[0] = newHeader;
				rawCsv = PriosCsvTools.ToCsv(parsed);
			}

			return new List<PriosDataStore.RawDataEntry>
			{
				new()
				{
					Name = GenerateNameFromUrl(url),
					Gid = "0",
					CSV = rawCsv
				}
			};
		}

		private string GenerateNameFromUrl(string url)
		{
			var fileName = Path.GetFileNameWithoutExtension(url);
			fileName = Regex.Replace(fileName, @"[^a-zA-Z0-9_]", "_");
			if (!char.IsLetter(fileName[0]))
				fileName = "Csv_" + fileName;

			return char.ToUpperInvariant(fileName[0]) + fileName.Substring(1);
		}

		public void OpenInBrowser(string url)
		{
			Application.OpenURL(url);
		}
	}
}
