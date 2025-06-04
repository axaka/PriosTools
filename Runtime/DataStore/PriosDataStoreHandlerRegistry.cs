using System;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PriosTools
{
#if UNITY_EDITOR
	[InitializeOnLoad]
#endif
	public static class PriosDataStoreHandlerRegistry
	{
		private static readonly Dictionary<string, IPriosDataSourceHandler> _handlers = new();
		public static IEnumerable<string> AvailableTypes => _handlers.Keys;

		static PriosDataStoreHandlerRegistry()
		{
			// Automatically register all types that implement the interface
			var handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a => a.GetTypes())
				.Where(t => typeof(IPriosDataSourceHandler).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

			foreach (var type in handlerTypes)
			{
				try
				{
					var instance = (IPriosDataSourceHandler)Activator.CreateInstance(type);
					RegisterHandler(instance);
				}
				catch (Exception ex)
				{
					UnityEngine.Debug.LogWarning($"[PriosRegistry] Failed to instantiate {type.Name}: {ex.Message}");
				}
			}
		}

		public static void RegisterHandler(IPriosDataSourceHandler handler)
		{
			if (handler == null || string.IsNullOrWhiteSpace(handler.SourceType)) return;

			if (_handlers.ContainsKey(handler.SourceType))
			{
				UnityEngine.Debug.LogWarning($"[PriosRegistry] Handler for type '{handler.SourceType}' already registered.");
				return;
			}

			_handlers[handler.SourceType] = handler;
			PriosDebugger.Log($"[PriosRegistry] Registered handler: {handler.SourceType}", handler.GetType());
		}

		public static IPriosDataSourceHandler GetHandlerForUrl(string url)
		{
			if (string.IsNullOrWhiteSpace(url)) return null;

			foreach (var handler in _handlers.Values)
			{
				if (handler.CanHandle(url))
					return handler;
			}

			return null;
		}


	}
}
