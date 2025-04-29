using System;
using System.Collections.Generic;
using UnityEngine;

namespace PriosTools
{
    public class PriosEvent : PriosSingleton<PriosEvent>
    {
        private readonly Dictionary<string, Action<object>> _events = new();

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
            if (!Instance._events.TryGetValue(key, out var existingEvent))
            {
                Instance._events[key] = _ => { };
            }

            Instance._events[key] += obj =>
            {
                if (obj is T castedValue)
                {
                    callback?.Invoke(castedValue);
                }
            };
        }

        public static void RemoveListener<T>(string key, Action<T> callback)
        {
            if (!Instance._events.ContainsKey(key)) return;

            Instance._events[key] -= obj =>
            {
                if (obj is T castedValue)
                {
                    callback?.Invoke(castedValue);
                }
            };
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