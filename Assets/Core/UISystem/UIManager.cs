using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Datamanager;
using Game.SceneManagement;
using Gamemanager;
using UnityEngine.SceneManagement; // 用於獲取場景資訊

namespace Game.UI
{
    /// <summary>
    /// 負責管理所有 UI 面板（HUD、選單、彈窗）的生命週期、顯示狀態和堆疊邏輯。
    /// </summary>
    public class UIManager : IInitializable, IDataLoadable
    {
        private Transform _uiRoot;
        public Transform UIRoot => _uiRoot;

        // --- 核心管理結構 ---

        /// <summary>
        /// 1. 常駐 UI (HUD) 的專門引用。
        /// 它不參與堆疊，由 UIManager 直接控制其生命週期和顯示。
        /// </summary>
        private BasePanel _hudPanel;

        /// <summary>
        /// 2. 面板堆疊 (Stack)。
        /// 僅用於 Menu 和 Popup 類型。
        /// 決定 ESC 鍵應關閉哪個面板，並實現「單一焦點」。
        /// </summary>
        private Stack<BasePanel> _panelStack = new Stack<BasePanel>();

        /// <summary>
        /// 3. 面板字典 (Dictionary)。
        /// 儲存所有已實例化的「可堆疊」面板，用於 O(1) 快速查找。
        /// </summary>
        public Dictionary<UIType, BasePanel> PanelDict { get; private set; } = new();

        /// <summary>
        /// UI 模板快取
        /// </summary>
        private Dictionary<UIType, UIDataBaseTemplete> _templateCache = new();

        #region 初始化與清理 (Bootstrap)

        public void Initialize()
        {
            Debug.Log("[UIManager] 初始化...");
            EnsureUIRootExists(); // 確保 Canvas 存在且為 DDOL

            PanelDict.Clear();
            _panelStack.Clear();
            _templateCache.Clear();

            // 訂閱核心事件
            // 注意：OnOpenBackpackKeyPressedEvent 這種特定按鍵事件，
            // 更好的做法是在 InputManager 中監聽，然後呼叫 UIManager.OpenPanel(UIType.BagMenu)
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnSceneLoadedEvent, OnSceneLoadedEvent);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnEscapeKeyPressedEvent, OnEscapeKeyPressedEvent);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnOpenBackpackKeyPressedEvent, OnOpenBackpackKeyPressedEvent);
        }

        // 新增方法：處理依賴 DataManager 的資產載入
        public async UniTask LoadDataDependentAssets()
        {
            Debug.Log("[UIManager] 資產載入初始化 (依賴 DataManager)...");
            
            await LoadInitialPersistentUI();
        }
        
        public void Cleanup()
        {
            Debug.Log("[UIManager] 清理...");
            CloseAllPanels(); // 清理所有可堆疊面板

            // 確保 HUD 也被銷毀
            if (_hudPanel != null && _hudPanel.gameObject != null)
            {
                UnityEngine.Object.Destroy(_hudPanel.gameObject);
            }
            _hudPanel = null;

            _templateCache.Clear();

            // 取消訂閱事件
            GameManager.Instance.MainGameEvent.Unsubscribe<SceneLoadedEvent>(OnSceneLoadedEvent);
            GameManager.Instance.MainGameEvent.Unsubscribe<EscapeKeyPressedEvent>(OnEscapeKeyPressedEvent);
            GameManager.Instance.MainGameEvent.Unsubscribe<OpenBackpackKeyPressedEvent>(OnOpenBackpackKeyPressedEvent);
        }

        private void EnsureUIRootExists()
        {
            if (_uiRoot == null)
            {
                var canvas = GameObject.Find("Canvas");
                if (canvas == null)
                {
                    Debug.LogWarning("[UIManager] 未找到 'Canvas'，動態創建一個。");
                    canvas = new GameObject("Canvas");
                    // 這裡應添加 Canvas, CanvasScaler, GraphicRaycaster 組件
                    // ...
                }
                _uiRoot = canvas.transform;
            }
            
            // 確保 UI 根物件（Canvas）不隨場景切換而銷毀
            GameObject.DontDestroyOnLoad(_uiRoot.gameObject);
        }
        
        /// <summary>
        /// 遊戲啟動時，預加載所有「常駐 UI」並將其設為隱藏
        /// </summary>
        private async UniTask LoadInitialPersistentUI()
        {
            // 1. 獲取專門定義「常駐 UI」的場景類型
            SceneType persistentType = SceneType.Persistent; 
            
            // 2. 從配置中獲取所有常駐 UI 的列表
            List<UIType> persistentUiTypes = GameManager.Instance.GameSo.uiConfig.GetUIPanelForScene(persistentType.ToString());

            if (persistentUiTypes == null || persistentUiTypes.Count == 0)
            {
                Debug.LogWarning("[UIManager] UIConfig 中沒有定義常駐 UI (Persistent)！");
                Debug.LogWarning("[UIManager] UIConfig 中沒有定義常駐 UI (Persistent)！");
                return;
            }
            
            List<UniTask> loadTasks = new List<UniTask>();

            // 3. 遍歷並加載所有常T駐 UI
            foreach (var uiType in persistentUiTypes)
            {
                // 這裡我們需要一個「只加載、不開啟」的通用方法
                loadTasks.Add(LoadAndInstantiatePanel(uiType, UIGroup.Persistent));
                Debug.Log($"[UIManager] 預加載並初始化常駐 UI: {uiType}");
            }
            
            // 4. 等待所有加載完成
            await UniTask.WhenAll(loadTasks);
            
            Debug.Log($"[UIManager] 成功預加載 {loadTasks.Count} 個常駐 UI。");
            
            // 檢查 _hudPanel 是否在加載過程中被正確賦值
            if (_hudPanel == null)
            {
                Debug.LogError("[UIManager] 常駐 UI 加載完畢，但 _hudPanel 引用為空！請檢查配置！");
            }
        }


        /// <summary>
        /// (新增輔助方法) 僅加載和實例化 Panel，不進行 Open (不顯示、不入堆疊)
        /// </summary>
        private async UniTask<BasePanel> LoadAndInstantiatePanel(UIType uiType, UIGroup uiGroup)
        {
            var prefab = LoadPanelPrefab(uiType); 
            if (prefab == null)
            {
                Debug.LogError($"[UIManager] 無法加載 Prefab for {uiType}！");
                return null;
            }

            var go = GameManager.Instance.InstantiateFromManager(prefab, _uiRoot, false);
            var panel = go.GetComponent<BasePanel>();
            if (panel == null)
            {
                Debug.LogError($"[UIManager] Prefab {uiType} 上缺少 BasePanel 組件！");
                UnityEngine.Object.Destroy(go);
                return null;
            }

            panel.Group = uiGroup;

            // 核心區別：
            if (uiGroup == UIGroup.Persistent)
            {
                // 如果是常駐 UI，我們需要儲存它的引用 (例如 HUD)
                // 這裡我們假設 GameHUD 是常駐 UI 的一種
                if (uiType == UIType.GameHUD) 
                {
                    _hudPanel = panel;
                }
                // 你可能還需要一個 _persistentPanelDict 來儲存其他的常駐 UI
            }
            else
            {
                // 如果是場景 UI，儲存在主字典中
                PanelDict[uiType] = panel;
            }
            
            // 預設隱藏
            panel.gameObject.SetActive(false); 
            return panel;
        }

        #endregion

        #region 事件訂閱與處理

        // 修正後的版本
        private async void OnSceneLoadedEvent(SceneLoadedEvent cmd)
        {
            Debug.Log("場景載入完成準備處理UI");
            // 1. 換場景時，關閉所有「非持久化」的 UI
            CloseAllPanels(); 

            // 2. 從事件參數中獲取強型別的 SceneType (這才是解耦的關鍵！)
            //    (您必須確保您的 SceneLoader 在發送事件時，把 SceneType 放入 cmd 中)
            SceneType currentSceneType = cmd.SceneType; 

            // 3. 根據 SceneType 獲取該場景需要「啟動時開啟」的 UI 列表
            List<UIType> startPanels = GameManager.Instance.GameSo.uiConfig.GetUIPanelForScene(currentSceneType.ToString());

            // 4. (這是您缺失的關鍵邏輯) 遍歷列表，並開啟所有 UI
            if (startPanels != null && startPanels.Count > 0)
            {
                Debug.Log($"[UIManager] 根據配置，為場景 {currentSceneType} 加載 {startPanels.Count} 個 UI...");
        
                foreach (var uiType in startPanels)
                {
                    // 使用 OpenPanel 來加載、實例化、並正確管理堆疊
                    // 因為我們在 (async) void 方法中，所以使用 .Forget()
                    OpenPanel<BasePanel>(uiType).Forget(); 
                }
            }

            // 5. 根據「強型別」決定是否顯示 HUD
            // (我們假設 SceneType 的名稱可以安全地用於此判斷)
            string sceneNameStr = currentSceneType.ToString();
            bool shouldShowHUD = !sceneNameStr.Contains("Menu") && !sceneNameStr.Contains("Load");
    
            if (_hudPanel != null)
            {
                _hudPanel.gameObject.SetActive(shouldShowHUD);
            }
        }

        /// <summary>
        /// 核心邏輯：
        /// 1. 如果堆疊 (Stack) 中有 Panel，則關閉最頂層 (Peek) 的那一個，但排除特殊 Panel。
        /// 2. 如果堆疊是空的 (我們在 Gameplay 狀態)，才開啟設置選單。
        /// </summary>
        private void OnEscapeKeyPressedEvent(EscapeKeyPressedEvent cmd)
        {
            // 獲取當前場景資訊
            var currentScene = SceneManager.GetActiveScene();
    
            if (_panelStack.Count > 0)
            {
                var topPanel = _panelStack.Peek();

                // 【關鍵豁免邏輯】:
                // 檢查 1: 如果當前場景是主菜單場景 (MainMenuScene)
                // 檢查 2: 且堆疊頂層是 MainMenu 這個特定的 Panel
                // 則 ESC 鍵不執行關閉操作。
                if (currentScene.name.Contains("MainMenu") && topPanel.CurrentUIType == UIType.MainMenu)
                {
                    Debug.Log("[UIManager] 偵測到 MainMenu 位於堆疊頂層，ESC 鍵操作被豁免。");
            
                    // 💡 可以在這裡加入額外邏輯，例如：彈出「確認退出遊戲」的 Popup。
                    // OpenPanel<ConfirmExitPopup>(UIType.ConfirmExitPopup).Forget();
            
                    return; // 終止關閉流程
                }
        
                // 動作 1：關閉最頂層的 Panel（非 MainMenu 或不是在 MainMenu 場景）
                ClosePanel(topPanel.CurrentUIType); 
            }
            else
            {
                // 動作 2：堆疊為空，開啟設定
                // 只有當前沒有任何 UI 視窗時，ESC 才會開啟設定。
        
                // 只有在 GameScene 才會開啟設定選單，主菜單則豁免
                if (currentScene.name.Contains("GameScene"))
                {
                    // 注意：這裡假設 SettingsWindow 是 Popup 級別，以便能在 Gameplay 中開啟
                    OpenPanel<SettingsWindow>(UIType.SettingsWindow).Forget();
                }
            }
        }

        private void OnOpenBackpackKeyPressedEvent(OpenBackpackKeyPressedEvent cmd)
        {
            // 這個呼叫現在會受到 OpenPanel 中的「單一焦點」檢查
            OpenPanel<BagMenu>(UIType.BagMenu).Forget();
        }

        #endregion

        #region 打開與關閉 UI Panel

        /// <summary>
        /// 非同步開啟一個 UI Panel
        /// 實施單一焦點強制機制：一次只允許開啟一個 Menu 或 Popup
        /// </summary>
        public async UniTask<T> OpenPanel<T>(UIType uiType) where T : BasePanel
        {
            Debug.Log("嘗試打開" + uiType);
            if (PanelDict.ContainsKey(uiType))
            {
                Debug.LogWarning($"[UIManager] UI {uiType} 已經開啟。");
                return PanelDict[uiType] as T;
            }

            var group = GetUIGroup(uiType);

            // --- 核心：模態堆疊檢查 ---
            if (group != UIGroup.Persistent)
            {
                if (_panelStack.Count > 0)
                {
                    var topPanel = _panelStack.Peek();
            
                    // 邏輯修改：
                    // 1. 如果頂層 Panel 的 Group 是 Popup，則允許開啟任何新的 Popup
                    //    -> 允許 Popup 開在 Popup 之上 (例如：提示 -> 確認)
                    // 2. 如果新開啟的 Panel Group 是 Menu，則阻止開啟。
                    //    -> 不允許 Menu 開在 Menu/Popup 之上 (例如：背包 -> 設定)
            
                    // 檢查：如果**新**開啟的是 `Menu`，且堆疊非空，則阻止。
                    // 但如果新開啟的是 `Popup`，則允許。
                    if (group == UIGroup.Menu)
                    {
                        var topPanelType = topPanel.CurrentUIType;
                        Debug.LogWarning($"[UIManager] [模態限制] 無法開啟 {uiType} (Menu)，因為 {topPanelType} 正在開啟中。請先關閉當前 Panel。");
                        return null;
                    }
                }
                // 如果新開啟的是 Popup，則這裡會允許其繼續執行，並被推入堆疊
            }

            // 沿用同步加載 Prefab，但 OpenPanel 保持異步 (為未來動畫或異步加載保留)
            var prefab = LoadPanelPrefab(uiType);
            if (prefab == null)
            {
                Debug.LogError($"[UIManager] Cannot load prefab for {uiType}");
                return null;
            }
            
            var go = GameManager.Instance.InstantiateFromManager(prefab, _uiRoot, false);
            var panel = go.GetComponent<BasePanel>();
            if (panel == null)
            {
                Debug.LogError($"[UIManager] Prefab 上缺少 BasePanel 組件: {uiType}");
                UnityEngine.Object.Destroy(go);
                return null;
            }

            panel.Group = group;
            
            // 處理常駐與堆疊
            if (group == UIGroup.Persistent)
            {
                // 理論上只有 HUD 會走這裡，且只在初始化時
                _hudPanel = panel;
            }
            else
            {
                // 如果打開的是 Menu (全螢幕)，隱藏 HUD
                if (group == UIGroup.Menu && _hudPanel != null)
                {
                    _hudPanel.gameObject.SetActive(false);
                }
                
                // 推入字典和堆疊
                PanelDict[uiType] = panel;
                _panelStack.Push(panel);
                
                // TODO: 通知 InputManager 切換到 UI 模式
                // GameManager.Instance.InputManager.SwitchToUIMode();
            }

            panel.OpenPanel(uiType);
            return panel as T;
        }

        /// <summary>
        /// 關閉指定 UI Panel (現在主要由 ESC 邏輯或面板自帶的關閉按鈕觸發)
        /// </summary>
        public bool ClosePanel(UIType uiType)
        {
            if (!PanelDict.TryGetValue(uiType, out var panel))
            {
                Debug.LogWarning($"[UIManager] 試圖關閉一個不存在的 Panel: {uiType}");
                return false;
            }
            
            // --- 核心：堆疊移除 ---
            // 檢查它是否在堆疊頂層
            if (_panelStack.Count > 0 && _panelStack.Peek() == panel)
            {
                _panelStack.Pop();
                PanelDict.Remove(uiType);
                
                // BasePanel 應該在 ClosePanel 動畫結束後自行 Destroy(gameObject)
                // 並呼叫 UIManager.RemovePanelReference(this) 來安全移除
                panel.ExecuteClose(); 

                // 檢查堆疊狀態並切換模式
                CheckStackTopOnClose();
            }
            else
            {
                 Debug.LogError($"[UIManager] Panel {uiType} 不在堆疊頂層，無法透過此方式關閉！(這通常不該發生在單一焦點模式下)");
                 return false;
            }

            return true;
        }

        /// <summary>
        /// 關閉 Panel 後，檢查堆疊狀態，決定是否切換回 Gameplay 模式或顯示 HUD
        /// </summary>
        private void CheckStackTopOnClose()
        {
            if (_panelStack.Count == 0)
            {
                // 堆疊空了，回到 Gameplay 模式
                if (_hudPanel != null)
                {
                    // 只有在非 MainMenu 場景才恢復顯示 HUD
                    if (!SceneManager.GetActiveScene().name.Contains("Menu"))
                    {
                         _hudPanel.gameObject.SetActive(true);
                    }
                }
                // TODO: 通知 InputManager 切換回 Gameplay 模式
                // GameManager.Instance.InputManager.SwitchToGameplayMode();
            }
            else
            {
                // 堆疊上還有東西，檢查頂層 Panel 的類型
                var topPanel = _panelStack.Peek();
                if (topPanel.Group != UIGroup.Menu && _hudPanel != null)
                {
                    // 如果頂層是 Popup (例如提示或子選單)，且 HUD 之前被隱藏，則恢復顯示
                    _hudPanel.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// 關閉所有「可堆疊」的 UI Panel
        /// </summary>
        public void CloseAllPanels()
        {
            // 從堆疊中依序關閉，這樣最安全
            while (_panelStack.Count > 0)
            {
                var panel = _panelStack.Pop();
                if (panel != null)
                {
                    panel.ExecuteClose(); // BasePanel 應自行 Destroy
                }
            }

            PanelDict.Clear();
            
            // 檢查是否需要切換回 Gameplay 模式
            CheckStackTopOnClose();
        }

        /// <summary>
        /// 關閉指定 UIGroup 中的所有 Panel (通常不推薦在單一焦點模式下使用)
        /// </summary>
        public void CloseAllPanels(UIGroup group)
        {
            // 此方法在單一焦點模式下意義不大，
            // 但如果仍需保留，它必須修改為能處理堆疊
            
            // 簡單的實現：
            if (_panelStack.Count > 0 && _panelStack.Peek().Group == group)
            {
                ClosePanel(_panelStack.Peek().CurrentUIType);
            }
        }

        #endregion

        #region 載入 Prefab 與 UI 模板 (沿用同步邏輯)

        private UIDataBaseTemplete LoadPanelTemplate(UIType uiType)
        {
            if (_templateCache.TryGetValue(uiType, out var tpl))
                return tpl;

            string key = uiType.ToString();
            tpl = GameContainer.Get<DataManager>().GetDataByName<UIDataBaseTemplete>(key);
            if (tpl == null)
            {
                Debug.LogError($"[UIManager] UI template not found: {key}");
                return null;
            }

            _templateCache[uiType] = tpl;
            return tpl;
        }

        private GameObject LoadPanelPrefab(UIType uiType)
        {
            var tpl = LoadPanelTemplate(uiType);
            
            // 假設 tpl.PrefabPath 是一個已在 DataManager 加載過的 GameObject 引用
            if (tpl == null || tpl.PrefabPath == null)
            {
                Debug.LogError($"[UIManager] PrefabPath missing or null for UI {uiType}");
                return null;
            }
            return tpl.PrefabPath;
        }

        #endregion

        #region 幫助方法

        /// <summary>
        /// 根據 UI 模板中儲存的字串轉換成 UIGroup 枚舉（若無或錯誤則回傳默認值）
        /// </summary>
        private UIGroup GetUIGroup(UIType uiType)
        {
            var tpl = LoadPanelTemplate(uiType);
            if (tpl == null)
            {
                Debug.LogError($"UI template not found for {uiType}");
                return UIGroup.Menu;   // 預設值
            }

            if (string.IsNullOrEmpty(tpl.UIGroup))
            {
                Debug.LogWarning($"UIGroup is empty for {uiType}, fallback to Menu");
                return UIGroup.Menu;
            }

            if (Enum.TryParse<UIGroup>(tpl.UIGroup, ignoreCase: true, out var group))
            {
                return group;
            }
            else
            {
                Debug.LogError($"Invalid UIGroup '{tpl.UIGroup}' for {uiType}, fallback to Menu");
                return UIGroup.Menu;
            }
        }

        public string GetOpenedPanelsDetailedInfo()
        {
            if (PanelDict == null || PanelDict.Count == 0)
            {
                return "目前沒有開啟的 UI Panel";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== UI Panel 狀態報告 ===");
            sb.AppendLine($"總數: {PanelDict.Count}");
            sb.AppendLine("------------------------");

            foreach (var kvp in PanelDict)
            {
                sb.AppendLine($"Key: {kvp.Key}");
                sb.AppendLine($"Type: {kvp.Value?.GetType().FullName ?? "null"}");
                sb.AppendLine($"Active: {kvp.Value?.gameObject.activeSelf.ToString() ?? "null"}");
                sb.AppendLine("------------------------");
            }

            return sb.ToString();
        }
        
        /// <summary>
        /// 獲取當前 UI 堆疊 (_panelStack) 的詳細狀態報告。
        /// 報告從最上層 (Top) 開始，向下追蹤。
        /// </summary>
        public string GetPanelStackDetailedInfo()
        {
            // 假設 _panelStack 是 UIManager 的 private 欄位
            // 這裡我們需要使用 ToArray() 來安全地遍歷堆疊，而不影響其狀態。
            if (_panelStack == null || _panelStack.Count == 0)
            {
                return "=== UI Panel Stack 狀態報告 ===\n堆疊為空，目前沒有模態 UI 存在。";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== UI Panel Stack 狀態報告 ===");
            sb.AppendLine($"堆疊深度: {_panelStack.Count}");
            sb.AppendLine("----------------------------");

            // 將堆疊轉為陣列並反轉。堆疊的 ToArray() 是從 Bottom 到 Top
            // 為了讓 Top Panel 排在第一個，我們需要反轉這個陣列。
            BasePanel[] panels = _panelStack.ToArray();
            Array.Reverse(panels); 

            for (int i = 0; i < panels.Length; i++)
            {
                var panel = panels[i];
        
                // 為了安全，檢查 panel 是否為 null（可能發生在非同步操作中）
                if (panel == null)
                {
                    sb.AppendLine($"[{i}] (已銷毀/Null 引用) - 錯誤！");
                    continue;
                }

                string position = (i == 0) ? "Top" : (i == panels.Length - 1 ? "Bottom" : $"{i}");

                sb.Append($"[{position}] ");
                sb.Append($"Name: {panel.gameObject.name} (Type: {panel.GetType().Name}) ");
                sb.AppendLine($"[Active: {panel.gameObject.activeSelf}]");
            }

            sb.AppendLine("----------------------------");

            return sb.ToString();
        }
        
        public string GetTemplateCacheDetailedInfo()
        {
            if (_templateCache == null || _templateCache.Count == 0)
            {
                return "目前沒有已快取的 UI Template";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== UI Template 狀態報告 ===");
            sb.AppendLine($"總數: {_templateCache.Count}");
            sb.AppendLine("------------------------");

            foreach (var kvp in _templateCache)
            {
                sb.AppendLine($"Key: {kvp.Key}");
                sb.AppendLine($"Type: {kvp.Value?.GetType().FullName ?? "null"}");
                sb.AppendLine("------------------------");
            }

            return sb.ToString();
        }
        
        #endregion

        #region UI 狀態檢測

        public bool HasOpenUIInGroup(UIGroup group)
        {
            // 在單一焦點模式下，這個方法只需要檢查頂層
            if (_panelStack.Count > 0)
            {
                return _panelStack.Peek().Group == group;
            }
            return false;
        }
        
        public bool IsPanelOpen(UIType type) => PanelDict.ContainsKey(type);

        public T GetPanel<T>(UIType type) where T : BasePanel
        {
            PanelDict.TryGetValue(type, out var panel);
            return panel as T;
        }

        public IEnumerable<BasePanel> GetPanelsByGroup(UIGroup group)
        {
            return PanelDict.Values.Where(p => p.Group == group);
        }

        #endregion
    }
}