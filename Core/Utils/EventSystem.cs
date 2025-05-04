using System;
using System.Collections.Generic;
using UnityEngine;

public class EventSystem
{
    private static EventSystem instance;
    public static EventSystem Instance
    {
        get
        {
            if (instance == null)
                instance = new EventSystem();
            return instance;
        }
    }
    // Dictionary to store event subscriptions
    private readonly Dictionary<Type, List<Delegate>> eventSubscriptions = new Dictionary<Type, List<Delegate>>();
    
    // Lock object for thread safety
    private readonly object subscriptionLock = new object();
    /// <summary>
    /// Subscribe to an event of type T
    /// </summary>
    public void Subscribe<T>(Action<T> handler)
    {
        if (handler == null)
        {
            Debug.LogError($"EventSystem: Cannot subscribe null handler for event type {typeof(T)}");
            return;
        }
        lock (subscriptionLock)
        {
            Type eventType = typeof(T);
            if (!eventSubscriptions.ContainsKey(eventType))
            {
                eventSubscriptions[eventType] = new List<Delegate>();
            }
            if (!eventSubscriptions[eventType].Contains(handler))
            {
                eventSubscriptions[eventType].Add(handler);
                Debug.Log($"EventSystem: Added subscription for {eventType}");
            }
        }
    }
    /// <summary>
    /// Unsubscribe from an event of type T
    /// </summary>
    public void Unsubscribe<T>(Action<T> handler)
    {
        if (handler == null) return;
        lock (subscriptionLock)
        {
            Type eventType = typeof(T);
            if (eventSubscriptions.TryGetValue(eventType, out var handlers))
            {
                if (handlers.Remove(handler))
                {
                    Debug.Log($"EventSystem: Removed subscription for {eventType}");
                    
                    // Clean up empty lists
                    if (handlers.Count == 0)
                    {
                        eventSubscriptions.Remove(eventType);
                    }
                }
            }
        }
    }
    /// <summary>
    /// Publish an event of type T
    /// </summary>
    public void Publish<T>(T eventData)
    {
        Type eventType = typeof(T);
        List<Delegate> handlers;
        // Get a snapshot of current handlers
        lock (subscriptionLock)
        {
            if (!eventSubscriptions.TryGetValue(eventType, out handlers))
                return;
            handlers = new List<Delegate>(handlers);
        }
        // Invoke handlers outside the lock
        foreach (var handler in handlers)
        {
            try
            {
                ((Action<T>)handler).Invoke(eventData);
            }
            catch (Exception e)
            {
                Debug.LogError($"EventSystem: Error invoking handler for {eventType}: {e}");
            }
        }
    }
    /// <summary>
    /// Clear all event subscriptions
    /// </summary>
    public void ClearAllSubscriptions()
    {
        lock (subscriptionLock)
        {
            eventSubscriptions.Clear();
            Debug.Log("EventSystem: Cleared all subscriptions");
        }
    }
    /// <summary>
    /// Get the number of subscribers for an event type
    /// </summary>
    public int GetSubscriberCount<T>()
    {
        lock (subscriptionLock)
        {
            Type eventType = typeof(T);
            return eventSubscriptions.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
        }
    }
    /// <summary>
    /// Check if there are any subscribers for an event type
    /// </summary>
    public bool HasSubscribers<T>()
    {
        return GetSubscriberCount<T>() > 0;
    }
}
