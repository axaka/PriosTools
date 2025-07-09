using PriosTools;
using System;
using System.Collections.Generic;
using UnityEngine; // for Debug.LogWarning

public class PriosEvent : PriosSingleton<PriosEvent>
{
	private readonly Dictionary<string, Action<object>> _events = new();
	private readonly Dictionary<string, List<Action<object>>> _listeners = new();

	public enum EventType { Object, Bool, Int, Float, String }

	public static EventType GetEventType(object obj) => obj switch
	{
		UnityEngine.Object => EventType.Object,
		bool => EventType.Bool,
		int => EventType.Int,
		float => EventType.Float,
		string => EventType.String,
		_ => EventType.Object
	};

	public static void AddListener(string key, Action<object> callback)
	{
		if (!Instance._events.TryGetValue(key, out _))
			Instance._events[key] = _ => { };

		if (!Instance._listeners.TryGetValue(key, out var list))
			Instance._listeners[key] = list = new();

		list.Add(callback);
		Instance._events[key] += callback;
	}

	public static void RemoveListener(string key, Action<object> callback)
	{
		if (!Instance._listeners.TryGetValue(key, out var list)) return;
		if (!list.Contains(callback)) return;

		Instance._events[key] -= callback;
		list.Remove(callback);
	}

	public static void TriggerEvent(string key, object value)
	{
		if (Instance._events.TryGetValue(key, out var action))
			action?.Invoke(value);
	}

	public static void ClearEvents() => Instance._events.Clear();


	// ────────────────────────────────────────────────
	// new static dictionary to hold our generic→object wrappers
	// ────────────────────────────────────────────────

	private static readonly Dictionary<
		string,
		Dictionary<Delegate, Action<object>>
	> _wrappers = new(StringComparer.Ordinal);


	// ────────────────────────────────────────────────
	// new generic overloads (all still static!)
	// ────────────────────────────────────────────────

	/// <summary>
	/// Listen for an event whose payload is a T.
	/// </summary>
	public static void AddListener<T>(string key, Action<T> callback)
	{
		// build a one‐time wrapper that casts object→T
		Action<object> wrapper = obj =>
		{
			if (obj is T t)
				callback(t);
			else
				Debug.LogWarning(
					$"[PriosEvent] Payload for '{key}' is not a {typeof(T).Name}."
				);
		};

		// stash the wrapper so RemoveListener<T> can find it again
		if (!_wrappers.TryGetValue(key, out var map))
		{
			map = new Dictionary<Delegate, Action<object>>();
			_wrappers[key] = map;
		}
		map[callback] = wrapper!;

		// register it in the existing, static API
		AddListener(key, wrapper);
	}

	/// <summary>
	/// Stop listening to a T‐typed event.
	/// </summary>
	public static void RemoveListener<T>(string key, Action<T> callback)
	{
		if (_wrappers.TryGetValue(key, out var map)
		 && map.TryGetValue(callback, out var wrapper))
		{
			RemoveListener(key, wrapper);
			map.Remove(callback);
			if (map.Count == 0)
				_wrappers.Remove(key);
		}
	}

	/// <summary>
	/// Fire an event with a T payload.
	/// </summary>
	public static void TriggerEvent<T>(string key, T value)
	{
		TriggerEvent(key, (object)value!);
	}
}
