using Cysharp.Threading.Tasks;

/// <summary>
/// 介面 1: 定義 Manager 的核心生命週期。
/// </summary>
public interface IInitializable
{
    /// <summary>
    /// 負責 Manager 的核心初始化，通常在 GameManager.Awake 階段被同步呼叫。
    /// 應包含：實例化、事件訂閱、清空狀態等不需要 DataManager 數據的操作。
    /// </summary>
    void Initialize(); 

    /// <summary>
    /// 負責 Manager 的清理工作，通常在 GameManager.OnDestroy 階段被呼叫。
    /// </summary>
    void Cleanup();
}

/// <summary>
/// 介面 2: 定義 Manager 的異步資產載入能力。
/// 只有在 DataManager 完成後，需要載入資源或執行耗時操作的 Manager 才需要實作。
/// </summary>
public interface IDataLoadable
{
    /// <summary>
    /// 負責 Manager 依賴資料的異步載入，通常在 DataManager.OnDataLoaded 事件後被呼叫。
    /// 例如：載入 HUD Prefab、載入初始遊戲配置。
    /// </summary>
    UniTask LoadDataDependentAssets();
}