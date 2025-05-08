using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PriosTools
{
	[Serializable]
	public abstract class PriosJsonSheetBase<T> where T : PriosJsonSheetBase<T>, new()
	{
		public abstract string Version { get; }
		public abstract bool IsValid();

		public static List<T> FromRows(List<Dictionary<string, object>> rows)
		{
			var list = new List<T>();
			var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);

			foreach (var row in rows)
			{
				var inst = new T();
				foreach (var field in fields)
				{
					if (row.TryGetValue(field.Name, out var val))
					{
						try
						{
							if (val == null || field.FieldType.IsAssignableFrom(val.GetType()))
							{
								field.SetValue(inst, val);
							}
							else
							{
								// Handle conversion if needed
								object converted = Convert.ChangeType(val, Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType);
								field.SetValue(inst, converted);
							}
						}
						catch (Exception ex)
						{
							Debug.LogWarning($"[PriosJsonSheetBase] Failed to set field '{field.Name}' on {typeof(T).Name}: {ex.Message}");
						}
					}
				}
				list.Add(inst);
			}

			return list;
		}
	}
}
