
using UnityEngine;

/// <summary>
/// 通用 MonoBehaviour 單例基類。
/// 負責實例查找、唯一性檢查與必要時的動態建立。
/// 不負責跨場景生命週期管理。
/// </summary>
public abstract class Singleton<T> : MonoBehaviour
    where T : MonoBehaviour
{
    protected static T _instance;
    private static bool _isQuitting;

    [Header("Singleton Debug")]
    [SerializeField]
    private bool enableSingletonDebug = true;

    public static T Instance
    {
        get
        {
            // Unity 退出期間，不再建立或取得新的實例。
            if (_isQuitting)
                return null;

            if (_instance != null)
                return _instance;

            // 先尋找場景中已存在的實例，包含未啟用物件。
            _instance = Object.FindAnyObjectByType<T>(
                FindObjectsInactive.Include
            );

            if (_instance != null)
            {
                LogFoundInstance(_instance);
                return _instance;
            }

            // 非 Play Mode 不自動建立物件。
            if (!Application.isPlaying)
                return null;

            // 找不到時才動態建立。
            GameObject singletonObject =
                new GameObject($"[Singleton] {typeof(T).Name}");

            _instance = singletonObject.AddComponent<T>();

            LogCreatedInstance(_instance);

            // 注意：不在這裡呼叫 DontDestroyOnLoad。
            // 跨場景生命週期交由 Bootstrap 或子類別管理。

            return _instance;
        }
    }

    /// <summary>
    /// 檢查唯一性並註冊目前實例。
    /// 子類別若覆寫 Awake，必須呼叫 base.Awake()。
    /// </summary>
    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning(
                $"[Singleton] 偵測到重複實例！\n" +
                $"類型：{typeof(T).Name}\n" +
                $"保留：{GetHierarchyPath(_instance.transform)}\n" +
                $"銷毀：{GetHierarchyPath(transform)}\n" +
                $"場景：{gameObject.scene.name}",
                gameObject
            );

            Destroy(gameObject);
            return;
        }

        _instance = this as T;

        Log(
            $"實例已註冊。\n" +
            $"類型：{typeof(T).Name}\n" +
            $"物件：{GetHierarchyPath(transform)}\n" +
            $"場景：{gameObject.scene.name}"
        );
    }

    /// <summary>
    /// 只有目前註冊的實例被銷毀時，才清除靜態引用。
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            Log(
                $"實例已銷毀。\n" +
                $"類型：{typeof(T).Name}\n" +
                $"物件：{GetHierarchyPath(transform)}"
            );

            _instance = null;
        }
    }

    /// <summary>
    /// 防止退出流程中再次建立 Singleton。
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private static void LogFoundInstance(T instance)
    {
        var singleton = instance as Singleton<T>;

        if (singleton == null ||
            !singleton.enableSingletonDebug)
            return;

        Debug.Log(
            $"[Singleton] 找到既有實例：{typeof(T).Name}\n" +
            $"物件：{GetHierarchyPath(instance.transform)}\n" +
            $"場景：{instance.gameObject.scene.name}",
            instance.gameObject
        );
    }

    private static void LogCreatedInstance(T instance)
    {
        var singleton = instance as Singleton<T>;

        if (singleton == null ||
            !singleton.enableSingletonDebug)
            return;

        Debug.Log(
            $"[Singleton] 已動態建立實例：{typeof(T).Name}\n" +
            $"物件：{GetHierarchyPath(instance.transform)}\n" +
            $"場景：{instance.gameObject.scene.name}",
            instance.gameObject
        );
    }

    private void Log(string message)
    {
        if (!enableSingletonDebug)
            return;

        Debug.Log($"[Singleton] {message}", gameObject);
    }

    private static string GetHierarchyPath(Transform target)
    {
        if (target == null)
            return "<null>";

        string path = target.name;
        Transform parent = target.parent;

        while (parent != null)
        {
            path = $"{parent.name}/{path}";
            parent = parent.parent;
        }

        return path;
    }
}
