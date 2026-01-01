using UnityEngine;
using Cysharp.Threading.Tasks;
using Gamemanager; 
using Game.SceneManagement; 

public class Bootstrap : MonoBehaviour
{
    [Tooltip("請將 GameManager 的整個 Prefab 拖曳到此欄位。")]
    [SerializeField]
    private GameObject _gameManagerPrefabObject; // 保持 GameObject 類型

    private void Awake()
    {
        // 1. 【正確的被動查找】：使用 FindObjectOfType 檢查，不觸發自動創建
        GameManager existingManager = FindObjectOfType<GameManager>();

        if (existingManager == null)
        {
            // 如果不存在，啟動引導流程
            StartBootstrap().Forget();
        }
        else
        {
            // 如果已經存在 (例如從其他場景直接進入)，銷毀 Bootstrap
            Destroy(gameObject);
        }
    }

    private async UniTaskVoid StartBootstrap()
    {
        Debug.Log("[Bootstrap] 開始...");

        // 宣告變數，但【不再呼叫 GameManager.Instance】
        GameManager manager = null; 

        // 1. 【強制實例化 Prefab】 (因為我們知道它目前不存在)
        if (_gameManagerPrefabObject != null)
        {
            // 實例化整個 GameObject Prefab (包含所有子物件和組件)
            GameObject managerObject = Instantiate(_gameManagerPrefabObject); 
            
            // 從新實例化的物件上獲取 GameManager 組件
            manager = managerObject.GetComponent<GameManager>();

            if (manager == null)
            {
                Debug.LogError("[Bootstrap] 實例化的 Prefab 上沒有找到 GameManager 組件！");
                return;
            }
            // 💡 提示：在 managerObject 被 Instantiate 的瞬間，
            // 它的所有組件（包括 GameManager 和所有子 Manager）的 Awake() 都已經執行完畢，
            // 並且 GameManager._instance 也已經被正確設定為這個完整的 Prefab 實例。
        }
        else
        {
            Debug.LogError("[Bootstrap] 缺少 Prefab！");
            return;
        }

        // 2. 等待一幀確保所有 Start() 流程開始
        await UniTask.Yield();

        // 3. 【關鍵】由 Bootstrap 主動呼叫初始化，並等待完成
        await manager.InitializeAsync();
        
        // 4. 切換場景 (職責回歸 Bootstrap)
        Debug.Log("[Bootstrap] 初始化完成，進入主菜單。");
        manager.SceneTransitionManager.LoadScene(SceneType.MainMenuScene ,true);
    }

    // private async UniTask InitializeManagers(GameManager manager)
    // {
    //     Debug.Log("[Bootstrap] 正在協調子系統的初始化和數據載入...");
    //
    //     // 這裡可以呼叫 GameManager 上的主要初始化函式
    //     await manager.Initialize(); 
    //     
    //     // 如果您的數據載入耗時，應該在這裡異步等待它完成
    //     // await manager.DataManager.LoadAllDataAsync(); 
    //     
    //     Debug.Log("[Bootstrap] 所有系統已準備就緒。");
    // }
}