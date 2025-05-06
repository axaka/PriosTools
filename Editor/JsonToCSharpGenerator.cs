using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PriosTools
{
	class JsonToCSharpGenerator
	{
		public static void GenerateCSharpClass(string jsonPath, string className, string outputPath)
		{
			if (!File.Exists(jsonPath))
			{
				Console.WriteLine("JSON file not found!");
				return;
			}

			string jsonContent = File.ReadAllText(jsonPath);

			JArray jsonArray = JArray.Parse(jsonContent);
			if (jsonArray.Count == 0 || jsonArray[0].Type != JTokenType.Object)
			{
				Console.WriteLine("Invalid JSON format or empty array.");
				return;
			}

			JObject sampleObject = (JObject)jsonArray[0];

			StringBuilder classBuilder = new StringBuilder();
			classBuilder.AppendLine("using System;");
			classBuilder.AppendLine("using System.Collections.Generic;");
			classBuilder.AppendLine("using UnityEngine;");
			classBuilder.AppendLine();
			classBuilder.AppendLine("[Serializable]");
			classBuilder.AppendLine($"public class {className}");
			classBuilder.AppendLine("{");

			// Fields based on JSON structure
			foreach (var property in sampleObject.Properties())
			{
				string pascalCaseName = Char.ToUpper(property.Name[0]) + property.Name.Substring(1);
				string propertyType = GetCSharpType(property.Value);
				classBuilder.AppendLine($"	public {propertyType} {pascalCaseName};");
			}

			classBuilder.AppendLine();
			classBuilder.AppendLine($"	public static List<{className}> LoadJson()");
			classBuilder.AppendLine("	{");
			classBuilder.AppendLine($"		TextAsset jsonText = Resources.Load<TextAsset>(\"JsonData/{className}\");");
			classBuilder.AppendLine("		if (jsonText == null)");
			classBuilder.AppendLine("		{");
			classBuilder.AppendLine($"			Debug.LogError(\"Failed to load JSON file: JsonData/{className}.json\");");
			classBuilder.AppendLine($"			return new List<{className}>();");
			classBuilder.AppendLine("		}");
			classBuilder.AppendLine("		return FromJsonArray(jsonText.text);");
			classBuilder.AppendLine("	}");

			classBuilder.AppendLine();
			classBuilder.AppendLine($"	private static List<{className}> FromJsonArray(string json)");
			classBuilder.AppendLine("	{");
			classBuilder.AppendLine("		string wrappedJson = \"{\\\"Items\\\":\" + json + \"}\";");
			classBuilder.AppendLine($"		Wrapper wrapper = JsonUtility.FromJson<Wrapper>(wrappedJson);");
			classBuilder.AppendLine("		return wrapper.Items;");
			classBuilder.AppendLine("	}");

			classBuilder.AppendLine();
			classBuilder.AppendLine("	[Serializable]");
			classBuilder.AppendLine("	private class Wrapper");
			classBuilder.AppendLine("	{");
			classBuilder.AppendLine($"		public List<{className}> Items;");
			classBuilder.AppendLine("	}");

			classBuilder.AppendLine("}");


			File.WriteAllText(outputPath, classBuilder.ToString());
			Console.WriteLine($"C# class generated successfully at: {outputPath}");
		}


		private static string GetCSharpType(JToken token)
		{
			switch (token.Type)
			{
				case JTokenType.String:
					return "string";
				case JTokenType.Integer:
					return "int";
				case JTokenType.Float:
					return "double";
				case JTokenType.Boolean:
					return "bool";
				case JTokenType.Array:
					var array = token as JArray;
					if (array != null && array.Count > 0)
					{
						var firstItem = array.First;
						return $"List<{GetCSharpType(firstItem)}>";
					}
					return "List<object>";
				case JTokenType.Object:
					return "object"; // You can expand this to handle nested classes if needed
				default:
					return "string";
			}
		}
	}
}
