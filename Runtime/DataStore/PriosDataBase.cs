using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Scripting;

namespace PriosTools
{
	[Preserve]
	public abstract class PriosDataBaseNonGeneric { }

	[Serializable]
	[Preserve]
	public abstract class PriosDataBase<T> : PriosDataBaseNonGeneric where T : PriosDataBase<T>, new()
	{
		public abstract string Version { get; }
		public abstract bool IsValid();

		[Preserve]
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
								continue;
							}

							if (fieldType.IsAssignableFrom(val.GetType()))
							{
								field.SetValue(inst, val);
								continue;
							}

							// Handle arrays
							if (fieldType.IsArray && val is object[] rawArray)
							{
								var elementType = fieldType.GetElementType();
								var typedArray = Array.CreateInstance(elementType, rawArray.Length);

								for (int i = 0; i < rawArray.Length; i++)
								{
									var elem = rawArray[i];

									if (elem == null)
									{
										typedArray.SetValue(null, i);
									}
									else if (elementType.IsAssignableFrom(elem.GetType()))
									{
										typedArray.SetValue(elem, i);
									}
									else
									{
										typedArray.SetValue(Convert.ChangeType(elem.ToString().Replace(",", "."), elementType, CultureInfo.InvariantCulture), i);
										Debug.Log($"Assigning field {field.Name} ({field.FieldType}) with value: {val} ({val?.GetType()})");
									}
								}

								field.SetValue(inst, typedArray);
								continue;
							}

							// Handle nullable types
							var targetType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;

							// Color special case
							if (targetType == typeof(Color))
							{
								if (val is Color colorVal)
								{
									field.SetValue(inst, colorVal);
								}
								else if (val is string s)
								{
									s = s.Trim();

									if (!s.StartsWith("#") && Regex.IsMatch(s, @"^[0-9a-fA-F]{6,8}$"))
										s = "#" + s;

									if (ColorUtility.TryParseHtmlString(s, out var parsedColor))
									{
										field.SetValue(inst, parsedColor);
									}
									else
									{
										var parts = s.Split(',');
										if (parts.Length == 3 || parts.Length == 4)
										{
											if (parts.All(p => byte.TryParse(p.Trim(), out _)))
											{
												byte r = byte.Parse(parts[0].Trim());
												byte g = byte.Parse(parts[1].Trim());
												byte b = byte.Parse(parts[2].Trim());
												byte a = parts.Length == 4 ? byte.Parse(parts[3].Trim()) : (byte)255;

												field.SetValue(inst, new Color32(r, g, b, a));
												continue;
											}
										}

										throw new FormatException($"Could not parse Color from '{s}'");
									}
								}
								continue;
							}

							// Enum
							if (targetType.IsEnum)
							{
								field.SetValue(inst, Enum.Parse(targetType, val.ToString()));
								continue;
							}

							// DateTime
							if (targetType == typeof(DateTime))
							{
								if (val is DateTime dt)
									field.SetValue(inst, dt);
								else if (DateTime.TryParse(val.ToString(), out var parsedDate))
									field.SetValue(inst, parsedDate);
								else
									throw new FormatException($"Cannot parse '{val}' as DateTime.");
								continue;
							}

							// Fallback conversion
							field.SetValue(inst, Convert.ChangeType(val, targetType));
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
