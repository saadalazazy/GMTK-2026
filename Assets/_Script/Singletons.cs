using UnityEngine;

/// <summary>
/// A Singleton that persists across scene loads.
/// Attach this to a GameObject in your starting scene.
/// </summary>
public abstract class PersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            // Destroy duplicate if one enters from another scene or duplicate spawn
            Destroy(gameObject);
        }
    }
}
/// <summary>
/// A Singleton that is destroyed when its scene is unloaded.
/// Ideal for level-specific or UI managers.
/// </summary>
public abstract class StaticInstance<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;
        }
        else if (Instance != this)
        {
            // Destroy duplicates within the same scene
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        // Clear reference when the scene unloads
        if (Instance == this)
        {
            Instance = null;
        }
    }
}