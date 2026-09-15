using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Production-grade Service Locator / Dependency Container.
/// Manages both MonoBehaviours and pure C# services with clean registration,
/// graceful fallbacks, and inspector wiring.
/// </summary>
public class DependencyManager : Singleton<DependencyManager>
{
    [Header("Pre-configured Scene / Prefab Dependencies")]
    [SerializeField]
    private List<MonoBehaviour> initialDependencies = new List<MonoBehaviour>();

    // Stores all registered services by their Type (classes or interfaces)
    private readonly Dictionary<Type, object> servicesDict = new Dictionary<Type, object>();

    protected override void Awake()
    {
        base.Awake();
        InitializeDependencies();
    }

    private void InitializeDependencies()
    {
        for (int i = 0; i < initialDependencies.Count; i++)
        {
            var item = initialDependencies[i];
            if (item != null)
            {
                Type type = item.GetType();
                if (!servicesDict.ContainsKey(type))
                {
                    servicesDict.Add(type, item);
                }
            }
        }
    }

    /// <summary>
    /// Registers a service instance (class or interface).
    /// </summary>
    public void Register<T>(T service) where T : class
    {
        Type type = typeof(T);
        if (servicesDict.ContainsKey(type))
        {
            Debug.LogWarning($"[DependencyManager] Service of type {type.Name} is already registered. Overwriting.");
            servicesDict[type] = service;
        }
        else
        {
            servicesDict.Add(type, service);
        }
    }

    /// <summary>
    /// Backward-compatible alias for Register.
    /// </summary>
    public void AddDependency<T>(T obj) where T : class
    {
        Register(obj);
    }

    /// <summary>
    /// Unregisters a service of type T.
    /// </summary>
    public void Unregister<T>() where T : class
    {
        Type type = typeof(T);
        if (servicesDict.ContainsKey(type))
        {
            servicesDict.Remove(type);
        }
    }

    /// <summary>
    /// Backward-compatible alias for Unregister.
    /// </summary>
    public void RemoveDependency<T>() where T : class
    {
        Unregister<T>();
    }

    /// <summary>
    /// Resolves a registered service. Throws or logs if not found.
    /// </summary>
    public T Resolve<T>() where T : class
    {
        Type type = typeof(T);
        if (servicesDict.TryGetValue(type, out object service))
        {
            return service as T;
        }

        Debug.LogError($"[DependencyManager] Dependency of type {type.Name} not found! Ensure it was registered or added in Awake.");
        return null;
    }

    /// <summary>
    /// Attempts to resolve a service. Optionally falls back to searching in scene if MonoBehaviour.
    /// </summary>
    public T TryResolve<T>(bool searchInScene = true) where T : class
    {
        Type type = typeof(T);
        if (servicesDict.TryGetValue(type, out object service))
        {
            return service as T;
        }

        if (searchInScene && typeof(MonoBehaviour).IsAssignableFrom(type))
        {
            MonoBehaviour found = FindAnyObjectByType(type) as MonoBehaviour;
            if (found != null)
            {
                Register(found as T);
                return found as T;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a service of type T is registered.
    /// </summary>
    public bool Contains<T>() where T : class
    {
        return servicesDict.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Clears all runtime registered dependencies (useful when switching scenes).
    /// </summary>
    public void ClearRuntimeDependencies()
    {
        servicesDict.Clear();
        InitializeDependencies();
    }
}