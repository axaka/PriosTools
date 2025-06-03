using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PriosTools
{
	public static class PriosCodeGenerator
	{
		private static readonly string _classDir = "Assets/Scripts/DataStoreClass/";
		private static readonly string _classPrefix = "PDS_";

		public static void GenerateCsClass(string className, List<string> types, List<string> names)
		{
			var sb = new StringBuilder();
			sb.AppendLine("using System;");
			sb.AppendLine("using UnityEngine;");
			sb.AppendLine("using System.Collections.Generic;");
			sb.AppendLine("using PriosTools;");
			sb.AppendLine("using UnityEngine.Scripting;");
			sb.AppendLine();

			sb.AppendLine("[Serializable]");
			sb.AppendLine("[Preserve]");
			sb.AppendLine($"public class {className} : PriosDataBase<{className}>");
			sb.AppendLine("{");

			foreach (var (type, name) in types.Zip(names, (t, n) => (t, n)))
				sb.AppendLine($"    public {type} {name};");

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
				sb.AppendLine($"            $\"{names[i]}: {{{names[i]}}}\"{(i < names.Count - 1 ? "," : "")}");
			sb.AppendLine("        });");
			sb.AppendLine("    }");
			sb.AppendLine();

			// ✅ WebGL-safe wrapper method
			sb.AppendLine("    [Preserve]");
			sb.AppendLine("    public static object FromRowsWrapper(List<Dictionary<string, object>> rows)");
			sb.AppendLine("    {");
			sb.AppendLine("        return FromRows(rows);");
			sb.AppendLine("    }");
			sb.AppendLine();

			// ✅ Static field name metadata for reflection-free access
			sb.AppendLine("    [Preserve]");
			sb.AppendLine("    public static List<string> GetFieldNames()");
			sb.AppendLine("    {");
			sb.AppendLine("        return new List<string>");
			sb.AppendLine("        {");
			for (int i = 0; i < names.Count; i++)
				sb.AppendLine($"            \"{names[i]}\"{(i < names.Count - 1 ? "," : "")}");
			sb.AppendLine("        };");
			sb.AppendLine("    }");
			sb.AppendLine();

			// ✅ Static constructor to auto-register deserializer
			sb.AppendLine($"    static {className}()");
			sb.AppendLine("    {");
			sb.AppendLine($"        PriosDataStoreDeserializerRegistry.Register(\"{className}\", FromRowsWrapper);");
			sb.AppendLine("    }");

			sb.AppendLine("}");

			if (!Directory.Exists(_classDir))
				Directory.CreateDirectory(_classDir);

			string path = Path.Combine(_classDir, className + ".cs");
			File.WriteAllText(path, sb.ToString());
			Debug.Log($"[CodeGen] ✅ {path}");
		}

		public static string GetClassPrefix() => _classPrefix;
		public static string GetClassDir() => _classDir;
	}
}
