using PriosTools;
using System.Collections.Generic;
using System;

public class PriosEvent : PriosSingleton<PriosEvent>
{
	private readonly Dictionary<string, Action<object>> _events = new();
	private readonly Dictionary<string, List<Action<object>>> _listeners = new();

	public enum EventType
	{
		Object,
		Bool,
		Int,
		Float,
		String,
	}

	public static EventType GetEventType(object obj)
	{
		return obj switch
		{
			UnityEngine.Object => EventType.Object,
			bool => EventType.Bool,
			int => EventType.Int,
			float => EventType.Float,
			string => EventType.String,
			_ => EventType.Object
		};
	}

	public static void AddListener(string key, Action<object> callback)
	{
		if (!Instance._events.TryGetValue(key, out var _))
			Instance._events[key] = _ => { };

		if (!Instance._listeners.TryGetValue(key, out var list))
			Instance._listeners[key] = list = new();

		list.Add(callback);
		Instance._events[key] += callback;
	}

	public static void RemoveListener(string key, Action<object> callback)
	{
		if (!Instance._listeners.TryGetValue(key, out var list)) return;

		if (list.Contains(callback))
		{
			Instance._events[key] -= callback;
			list.Remove(callback);
		}
	}

	public static void TriggerEvent(string key, object value)
	{
		if (Instance._events.TryGetValue(key, out var action))
			action?.Invoke(value);
	}

	public static void ClearEvents() => Instance._events.Clear();
}
