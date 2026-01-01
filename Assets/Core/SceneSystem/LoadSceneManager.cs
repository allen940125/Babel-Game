using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Gamemanager;

namespace Game.SceneManagement
{
    public enum SceneType
    {
        // --- 預備的遊戲場景 ---
        InitScene,

        // --- 真實的遊戲場景 ---
        MainMenuScene,
        LoadingScene,
        GameScene,
    
        // --- 用於配置查找的「虛擬」場景類型 ---
        Persistent // 代表「常駐 UI」，例如 HUD
    }

    /// <summary>
    /// 負責場景轉換的管理器（建議命名為 SceneTransitionManager）
    /// </summary>
    public class SceneTransitionManager : IInitializable
    {
        /// <summary>
        /// 靜態變數，保存下一個目標場景（供 LoadingScreenController 參考）
        /// </summary>
        public static SceneType NextTargetScene;

        public void Initialize()
        {
            // 如果需要監聽 Unity 的場景載入完成事件，可啟用下面這行：
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        public void Cleanup()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        // 若需要在場景載入後通知其他系統，可使用此事件處理函數
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 1. 獲取剛載入場景的「字串名稱」
            string sceneName = scene.name;

            // 2. 將「字串名稱」轉換回「SceneType 枚舉」
            //    (這是必要的，因為 Unity 的 API 只返回 string)
            if (Enum.TryParse(sceneName, out SceneType loadedType))
            {
                // 3. 創建一個「帶有數據」的事件
                SceneLoadedEvent eventCmd = new SceneLoadedEvent(loadedType);

                // 4. 發送這個「帶有數據」的事件
                GameManager.Instance.MainGameEvent.Send(eventCmd);
            }
            else
            {
                Debug.LogError($"[SceneManager] 場景 {sceneName} 載入成功，但在 SceneType 枚舉中找不到對應值！");
            }
        }

        /// <summary>
        /// 載入場景，並可選擇是否跳過 Loading 畫面（skipLoadingScreen 為 true 時直接載入目標場景）
        /// </summary>
        /// <param name="sceneType">目標場景</param>
        /// <param name="skipLoadingScreen">是否跳過 Loading 畫面</param>
        public void LoadScene(SceneType sceneType, bool skipLoadingScreen = false)
        {
            if (skipLoadingScreen)
            {
                // 直接載入目標場景，不經 Loading 畫面
                SceneManager.LoadScene(sceneType.ToString());
            }
            else
            {
                // 記錄目標場景，LoadingScreenController 會讀取此變數
                NextTargetScene = sceneType;
                // 使用 GameManager 提供的 Coroutine 執行 (因為本類並非 MonoBehaviour)
                GameManager.Instance.StartCoroutineFromManager(IELoadScene());
            }
        }

        private IEnumerator IELoadScene()
        {
            // 傳送場景轉換開始事件，可用於觸發 UI 轉場動畫等效果
            GameManager.Instance.MainGameEvent.Send(new SceneTransitionStartedEvent());

            // 小延遲讓轉場動畫先播
            yield return new WaitForSeconds(0.5f);
            // 載入 LoadingScene
            SceneManager.LoadScene(SceneType.LoadingScene.ToString());
        }
    }
}
