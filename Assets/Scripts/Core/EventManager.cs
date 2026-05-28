using System;
using System.Collections.Generic;

namespace TheAlchemistsCrypt.Core
{
    public static class EventManager
    {
        private static readonly Dictionary<Type, List<Delegate>> eventListeners = new Dictionary<Type, List<Delegate>>();

        public static void Subscribe<T>(Action<T> listener)
        {
            Type eventType = typeof(T);
            if (!eventListeners.ContainsKey(eventType))
            {
                eventListeners[eventType] = new List<Delegate>();
            }
            eventListeners[eventType].Add(listener);
        }

        public static void Unsubscribe<T>(Action<T> listener)
        {
            Type eventType = typeof(T);
            if (eventListeners.ContainsKey(eventType))
            {
                eventListeners[eventType].Remove(listener);
            }
        }

        public static void Trigger<T>(T eventData)
        {
            Type eventType = typeof(T);
            if (eventListeners.TryGetValue(eventType, out var listeners))
            {
                var listenersCopy = new List<Delegate>(listeners);
                foreach (var listener in listenersCopy)
                {
                    try
                    {
                        ((Action<T>)listener)?.Invoke(eventData);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"[EventManager] Error executing listener for event {eventType.Name}: {e}");
                    }
                }
            }
        }
    }

    public struct EnemyDeathEvent
    {
        public UnityEngine.GameObject EnemyObject;
        public UnityEngine.Vector3 Position;
    }

    public struct PlayerDamageEvent
    {
        public float DamageAmount;
        public float CurrentHealth;
        public float MaxHealth;
    }
}
