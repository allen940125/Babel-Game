using System.Collections;
using Cysharp.Threading.Tasks;
using Datamanager;
using Game.Input;
using Game.SceneManagement;
using UnityEngine;
using Game.UI;
using Gamemanager;
using UnityEngine.InputSystem;

public class GameManager : Singleton<GameManager>
{
    public MainGameEventPack MainGameEvent { get; private set; } = new MainGameEventPack();
    
    [SerializeField] private MainGameMediator mainGameMediator;
    public MainGameMediator MainGameMediator 
    { 
        get => mainGameMediator; 
        private set => mainGameMediator = value; 
    }
    
    [SerializeField] private GameObject player;
    
    public GameObject Player 
    { 
        get => player; 
        private set => player = value; 
    }
    
    public SceneTransitionManager SceneTransitionManager { get; private set; } = new SceneTransitionManager();
    public TransitionUIManager TransitionUIManager { get; private set; } = new TransitionUIManager();
    public UIManager UIManager { get; private set; } = new UIManager();
    public InputManagers InputManagers { get; private set; } = new InputManagers();

    [Header("遊戲資料")]
    [SerializeField] private GameSo gameSo;
    [SerializeField] private TextAsset csvFile;
    [SerializeField] private Sprite sprite;
    public GameSo GameSo 
    { 
        get => gameSo; 
        set => gameSo = value; 
    }
    
    // 新增一個標記，防止重複初始化
    public bool IsInitialized { get; private set; } = false;

    protected override void Awake()
    {
        base.Awake();
        if (this != Instance) return;
        DontDestroyOnLoad(gameObject);

        // 基礎系統的同步初始化 (建構子或輕量設定)
        // 如果 MainGameMediator 不需要從 Inspector 設定，可直接 new
        if (MainGameMediator == null) MainGameMediator = new MainGameMediator();
        MainGameMediator.MainGameMediatorInit();
        
        SceneTransitionManager.Initialize();
        TransitionUIManager.Initialize();
        UIManager.Initialize();
        InputManagers.Initialize();
    }
    
    private void OnDestroy()
    {
        SceneTransitionManager.Cleanup();
        TransitionUIManager.Cleanup();
        UIManager.Cleanup();
        InputManagers.Cleanup();
    }
    
    private void Start()
    {
        // 【開發模式支援】
        // 如果 GameManager 被創建了，但沒有人呼叫 InitializeAsync (表示沒有經過 Bootstrap)
        // 我們可以在這裡自動執行，以支援直接從 GameScene 測試
        // 但為了避免與 Bootstrap 衝突，我們通常等待一幀或檢查標記
        CheckAndAutoInitialize().Forget();
    }

    private void Update()
    {
        #region 測試用

        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            Debug.Log("鍵盤上的 H 鍵被按下了 (Was Pressed This Frame)！");

            UIManager.OpenPanel<FadeInOutWindow>(UIType.FadeInOutWindow);
            //UIManager.GetPanel<FadeInOutWindow>(UIType.FadeInOutWindow).SetFadeImage(sprite);
            UIManager.GetPanel<FadeInOutWindow>(UIType.FadeInOutWindow).FadeIn(1,4);
        }
        
        if (Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame)
        {
            Debug.Log("鍵盤上的 U 鍵被按下了 (Was Pressed This Frame)！");
            Debug.Log(UIManager.GetOpenedPanelsDetailedInfo());
            Debug.Log(UIManager.GetPanelStackDetailedInfo());
        }

        #endregion
        
        InputManagers.DebugCheckInputSwitch();
    }

    private async UniTaskVoid CheckAndAutoInitialize()
    {
        await UniTask.Yield(); // 等待 Bootstrap 有機會執行
        if (!IsInitialized)
        {
            Debug.LogWarning("[GameManager] 偵測到未經過 Bootstrap 啟動，正在進行開發模式自我初始化...");
            await InitializeAsync();
            // 開發模式下，通常我們停留在當前場景，不跳轉 MainMenu
            InputManagers.SetInputActive(InputType.Player);
        }
    }
    
    /// <summary>
    /// 【核心修改】公開的異步初始化方法
    /// 讓 Bootstrap 可以 await 這個過程
    /// </summary>
    public async UniTask InitializeAsync()
    {
        if (IsInitialized) return;
        
        IsInitialized = true;
        
        Debug.Log("[GameManager] 開始載入核心數據...");

        // 1. 等待 DataManager 載入 (假設 InitDataMananger 回傳 UniTask 或 Task)
        // 既然使用了 async/await，就不需要訂閱 OnDataLoaded 事件了，直接等待即可
        await GameContainer.Get<DataManager>().InitDataMananger();
        
        // 2. 等待 UI 必要資源載入
        await UIManager.LoadDataDependentAssets();

        Debug.Log("[GameManager] 核心數據與資源載入完成。");
    }
    
    public void SetPlayer(GameObject newPlayer)
    {
        if (newPlayer != null)
        {
            Player = newPlayer;
        }
    }

    public void NewGame()
    {
        // TODO: 實作新遊戲初始化邏輯
    }

    public void GameStart()
    {
        // TODO: 實作遊戲開始流程
        Debug.Log("Game Start Logic Executed");
    }
    
    public void GameOver()
    {
        //UIManager.OpenPanel(UIType.GameOverMenu);
    }
    
    public void StartCoroutineFromManager(IEnumerator coroutine)
    {
        StartCoroutine(coroutine);
    }

    public GameObject InstantiateFromManager(GameObject prefab, Transform parent = null, bool instantiateInWorldSpace = false)
    {
        return Instantiate(prefab, parent, instantiateInWorldSpace);
    }
}
