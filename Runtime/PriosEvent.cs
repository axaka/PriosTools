using System;
using System.Collections.Generic;

namespace PriosTools
{
	public class PriosEvent : PriosSingleton<PriosEvent>
	{
		private readonly Dictionary<string, Action<object>> _events = new();
		private readonly Dictionary<string, List<Delegate>> _listeners = new();

		public enum EventType
		{
			Object, // Also includes null
			Bool,
			Int,
			Float,
			String,
		}

		public static EventType GetEventType(object obj)
		{
			return obj switch
			{
				UnityEngine.Object _ => EventType.Object, // None is also UnityEngine.Object
				bool _ => EventType.Bool,
				int _ => EventType.Int,
				float _ => EventType.Float,
				string _ => EventType.String,
				_ => EventType.Object
			};
		}

		public static void AddListener<T>(string key, Action<T> callback)
		{
			if (!Instance._events.TryGetValue(key, out var existing))
				Instance._events[key] = _ => { };

			Action<object> wrapper = obj =>
			{
				if (obj is T castedValue)
					callback?.Invoke(castedValue);
			};

			if (!Instance._listeners.TryGetValue(key, out var list))
				list = Instance._listeners[key] = new();

			list.Add(wrapper);
			Instance._events[key] += wrapper;
		}

		public static void RemoveListener<T>(string key, Action<T> callback)
		{
			if (!Instance._listeners.TryGetValue(key, out var list)) return;

			foreach (var del in list)
			{
				Instance._events[key] -= (Action<object>)del;
			}

			Instance._listeners[key].Clear(); // Optional: clean up
		}

		public static void TriggerEvent<T>(string key, T value)
		{
			if (Instance._events.TryGetValue(key, out var action))
			{
				action?.Invoke(value);
			}
		}

		public static void ClearEvents() => Instance._events.Clear();
	}
}