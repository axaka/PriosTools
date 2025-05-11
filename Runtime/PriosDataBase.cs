using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PriosTools
{
	[Serializable]
	public abstract class PriosDataBase<T> where T : PriosDataBase<T>, new()
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
							var fieldType = field.FieldType;

							if (val == null)
							{
								field.SetValue(inst, null);
							}
							else if (fieldType.IsAssignableFrom(val.GetType()))
							{
								field.SetValue(inst, val);
							}
							else if (fieldType.IsArray && val is object[] rawArray)
							{
								var elementType = fieldType.GetElementType();
								var typedArray = Array.CreateInstance(elementType, rawArray.Length);
								for (int i = 0; i < rawArray.Length; i++)
								{
									if (rawArray[i] == null)
									{
										typedArray.SetValue(null, i);
									}
									else if (elementType.IsAssignableFrom(rawArray[i].GetType()))
									{
										typedArray.SetValue(rawArray[i], i);
									}
									else
									{
										typedArray.SetValue(Convert.ChangeType(rawArray[i], elementType), i);
									}
								}
								field.SetValue(inst, typedArray);
							}
							else
							{
								var targetType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
								field.SetValue(inst, Convert.ChangeType(val, targetType));
							}
						}
						catch (Exception ex)
						{
							Debug.LogWarning($"[PriosDataBase] Failed to set field '{field.Name}' on {typeof(T).Name}: {ex.Message}");
						}
					}
				}
				list.Add(inst);
			}

			return list;
		}
	}
}
