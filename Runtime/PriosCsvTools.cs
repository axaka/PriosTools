
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PriosTools
{
	public static class PriosCsvTools
	{
		public static List<Dictionary<string, object>> CsvToRows(string csv)
		{
			var parsed = Parse(csv);
			if (parsed.Count < 2) return new();

			var header = parsed[0];
			var dataRows = parsed.Skip(1).ToList();
			var metadata = header.Select(ParseTypeAndSeparator).ToList();

			var rows = new List<Dictionary<string, object>>();

			foreach (var dataRow in dataRows)
			{
				var dict = new Dictionary<string, object>();
				for (int i = 0; i < header.Count && i < dataRow.Count; i++)
				{
					var (type, name, separator) = metadata[i];
					string cell = dataRow[i].Trim();

					object value = type.EndsWith("[]")
						? ParseArrayValue(type, cell, separator)
						: ParseSingleValue(type, cell);

					dict[name] = value;
				}
				rows.Add(dict);
			}

			return rows;
		}

		public static List<List<string>> Parse(string csv)
		{
			var result = new List<List<string>>();
			var currentRow = new List<string>();
			var field = new StringBuilder();
			bool inQuotes = false;

			for (int i = 0; i < csv.Length; i++)
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
			}

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
				.Replace("\n", "\n")
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

			return rows.Select(row =>
			{
				var filtered = new List<string>();
				foreach (var index in keepIndices)
					filtered.Add(index < row.Count ? row[index] : "");
				return filtered;
			}).ToList();
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

		public static (List<string> types, List<string> names) ExtractTypesAndNames(List<string> headerRow)
		{
			var types = new List<string>();
			var names = new List<string>();

			for (int col = 0; col < headerRow.Count; col++)
			{
				string rawHeader = headerRow[col];
				var (typeHint, namePart, _) = ParseTypeAndSeparator(rawHeader);

				// Skip comment columns
				if (typeHint == "#")
					continue;

				string validatedName = ValidateName(namePart, $"Col{col}");
				types.Add(typeHint);
				names.Add(validatedName);
			}

			return (types, names);
		}

		public static string ValidateName(string name, string fallback)
		{
			if (string.IsNullOrWhiteSpace(name))
				return fallback;

			name = name.Trim().Trim(';');

			var sb = new System.Text.StringBuilder();

			// Ensure valid first character
			if (!char.IsLetter(name[0]) && name[0] != '_')
				sb.Append('_');

			foreach (char c in name)
			{
				if (char.IsLetterOrDigit(c) || c == '_')
					sb.Append(c);
				else
					sb.Append('_');
			}

			string result = sb.ToString();

			return string.IsNullOrWhiteSpace(result) ? fallback : result;
		}


		private static (string type, string name, string separator) ParseTypeAndSeparator(string header)
		{
			if (string.IsNullOrWhiteSpace(header))
				return ("string", $"Col", null);

			var tokens = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			string baseTypeToken = tokens.ElementAtOrDefault(0)?.Trim() ?? "string";
			string name = tokens.ElementAtOrDefault(1)?.Trim() ?? "Col";
			string separator = tokens.ElementAtOrDefault(2)?.Trim();

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

			if (isArray && string.IsNullOrEmpty(separator))
				separator = ",";

			return (typeCore, name, isArray ? separator : null);
		}

		private static object[] ParseArrayValue(string type, string value, string sep)
		{
			if (string.IsNullOrWhiteSpace(value)) return Array.Empty<object>();
			string baseType = type.Replace("[]", "").Replace("?", "");

			return value
				.Split(new[] { sep }, StringSplitOptions.None)
				.Select(v =>
				{
					var parsed = ParseSingleValue(baseType, v.Trim());
					return parsed;
				}).ToArray();
		}

		private static object ParseSingleValue(string type, string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;
			type = type?.TrimEnd('?');

			switch (type)
			{
				case "string": return value;
				case "int":
					return int.TryParse(value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : null;
				case "float":
					return float.TryParse(value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : null;
				case "bool":
					var b = value.Trim().ToLowerInvariant();
					return b switch
					{
						"1" or "yes" or "y" or "true" => true,
						"0" or "no" or "n" or "false" => false,
						_ => null
					};
				case "DateTime":
					return DateTime.TryParse(value, out var dt) ? dt : null;
				case "Color":
					return ColorUtility.TryParseHtmlString(value.StartsWith("#") ? value : "#" + value, out var color) ? color : null;
				default: return value;
			}
		}
	}
}
