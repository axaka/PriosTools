using System;
using System.Collections.Generic;
using UnityEngine;

namespace PriosTools
{
	public abstract class PriosJsonSheetBase<T>
	{
		/// <summary>
		/// Optional version string extracted from sheet (you can extend to parse it from metadata).
		/// </summary>
		public virtual string Version => "1.0";

		/// <summary>
		/// Override in derived class to validate individual entries.
		/// </summary>
		public virtual bool IsValid() => true;

		/// <summary>
		/// Load and filter valid entries from Resources/JsonData.
		/// </summary>
		public static List<T> Load(string fileName)
		{
			TextAsset jsonText = Resources.Load<TextAsset>($"JsonData/{fileName}");
			if (jsonText == null)
			{
				Debug.LogError($"Failed to load JSON file: JsonData/{fileName}.json");
				return new List<T>();
			}

			return FilterValid(FromJsonArray(jsonText.text));
		}

		private static List<T> FromJsonArray(string json)
		{
			string wrappedJson = "{\"Items\":" + json + "}";
			Wrapper wrapper = JsonUtility.FromJson<Wrapper>(wrappedJson);
			return wrapper.Items ?? new List<T>();
		}

		private static List<T> FilterValid(List<T> input)
		{
			List<T> valid = new();
			foreach (var item in input)
			{
				if (item is PriosJsonSheetBase<T> casted)
				{
					if (casted.IsValid())
						valid.Add(item);
				}
				else
				{
					valid.Add(item); // fallback, assume valid
				}
			}

			return valid;
		}

		[Serializable]
		private class Wrapper
		{
			public List<T> Items;
		}
	}
}
