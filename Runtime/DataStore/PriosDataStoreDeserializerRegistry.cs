using System;
using System.Collections.Generic;

namespace PriosTools
{
    public static class PriosDataStoreDeserializerRegistry
    {
		public static readonly Dictionary<string, Func<List<Dictionary<string, object>>, object>> Deserializers
					= new();

		public static void Register(string name, Func<List<Dictionary<string, object>>, object> func)
		{
			Deserializers[name] = func;
		}
	}
}
