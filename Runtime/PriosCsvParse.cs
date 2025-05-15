using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PriosTools
{
	public static class PriosCsvParser
	{
		public static List<List<string>> Parse(string csv)
		{
			var result = new List<List<string>>();
			var currentRow = new List<string>();
			var field = new StringBuilder();
			bool inQuotes = false;
			int i = 0;

			while (i < csv.Length)
			{
				char c = csv[i];

				if (c == '"')
				{
					if (inQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
					{
						field.Append('"');
						i++;
					}
					else
					{
						inQuotes = !inQuotes;
					}
				}
				else if (c == ',' && !inQuotes)
				{
					currentRow.Add(FinalizeField(field.ToString()));
					field.Clear();
				}
				else if ((c == '\r' || c == '\n') && !inQuotes)
				{
					// Handle CRLF
					if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;
					currentRow.Add(FinalizeField(field.ToString()));
					result.Add(currentRow);
					currentRow = new List<string>();
					field.Clear();
				}
				else
				{
					field.Append(c);
				}

				i++;
			}

			// Final value
			if (field.Length > 0 || currentRow.Count > 0)
			{
				currentRow.Add(FinalizeField(field.ToString()));
				result.Add(currentRow);
			}

			return RemoveCommentColumns(result);
		}

		private static string FinalizeField(string field)
		{
			return field
				.Trim()
				.Replace("\\n", "\n")
				.Replace("<br>", "\n")
				.Replace("<br/>", "\n")
				.Replace("<br />", "\n");
		}

		private static List<List<string>> RemoveCommentColumns(List<List<string>> rows)
		{
			if (rows.Count == 0) return rows;

			var header = rows[0];
			var keepIndices = new List<int>();

			for (int i = 0; i < header.Count; i++)
			{
				if (!header[i].TrimStart().StartsWith("#"))
					keepIndices.Add(i);
			}

			var cleaned = new List<List<string>>();
			foreach (var row in rows)
			{
				var filtered = new List<string>();
				foreach (var index in keepIndices)
				{
					filtered.Add(index < row.Count ? row[index] : "");
				}
				cleaned.Add(filtered);
			}

			return cleaned;
		}
	}

}