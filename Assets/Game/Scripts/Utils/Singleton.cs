using UnityEngine;

/// <summary>
/// Production-ready generic Singleton base class for MonoBehaviours.
/// Supports optional DontDestroyOnLoad, application quit protection, and clean lifecycle cleanup.
/// </summary>
/// <typeparam name="T">Component Type</typeparam>
public abstract class Singleton<T> : MonoBehaviour where T : Component
{
    private static T instance;
    private static bool isQuitting = false;
    private static readonly object lockObj = new object();

    [Header("Singleton Settings")]
    [SerializeField] private bool dontDestroyOnLoad = false;

    public static bool HasInstance => instance != null;

    public static T Instance
    {
        get
        {
            if (isQuitting)
            {
                return null;
            }

            lock (lockObj)
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<T>();
                }
                return instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this as T;

        if (dontDestroyOnLoad)
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        isQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}