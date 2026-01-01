using UnityEngine;
using Game.Audio; // 引入您的 AudioManager 命名空間
using Gamemanager; // 引入您的 Gamemanager 命名空間
using UnityEngine.InputSystem; // 引入 Input System 命名空間

/// <summary>
/// 測試 AudioManager 各項功能與 SFX Pooling 性能的腳本 (使用 Input System 靜態存取)。
/// </summary>
public class AudioTestController : MonoBehaviour
{
    [Header("測試數據配置 (請在 Inspector 填入)")]
    [Tooltip("用於測試 Music 的 AudioData (單一音軌)")]
    public AudioData testMusicData; 
    
    [Tooltip("用於測試 Ambient 的 AudioData (單一音軌)")]
    public AudioData testAmbientData; 
    
    [Tooltip("用於測試 UI 的 AudioData (單一音軌)")]
    public AudioData testUIData;

    [Tooltip("用於測試 SFX Pooling 的多個音效")]
    public AudioData[] testSFXDatas;

    [Header("性能測試配置")]
    [Tooltip("每次點擊播放 SFX 的數量")]
    public int burstAmount = 15; 
    
    [Tooltip("每次爆發之間的時間間隔 (秒)")]
    public float burstInterval = 0.05f;

    private AudioManager audioManager;

    void Start()
    {
        // 確保 AudioManager 實例已經存在
        audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            Debug.LogError("AudioManager 實例未找到，請確認場景中有 AudioManager 或 Singleton 設置正確。");
            enabled = false;
            return;
        }

        // 設置初始音量（如果儲存系統未初始化）
        audioManager.MasterVolume = 1.0f;
        audioManager.MusicVolume = 0.5f;
        audioManager.AmbientVolume = 0.5f;
        audioManager.SFXVolume = 0.8f;
        audioManager.UIVolume = 0.7f;

        Debug.Log("AudioManager 測試腳本已啟動。請按下以下鍵執行測試：");
        Debug.Log("--- 基本播放測試 ---");
        Debug.Log("按 [Q]: 播放 Music (覆蓋)");
        Debug.Log("按 [W]: 播放 Ambient (覆蓋)");
        Debug.Log("按 [E]: 播放 UI 音效 (PlayOneShot)");
        Debug.Log("按 [R]: 播放 隨機 SFX (使用 Pooling)");
        Debug.Log("--- 音量測試 ---");
        Debug.Log("按 [Z]: Master 音量減 0.1");
        Debug.Log("按 [X]: Master 音量加 0.1");
        Debug.Log("--- SFX Pool 性能測試 ---");
        Debug.Log($"按 [P]: 爆發式播放 {burstAmount} 個 SFX (測試 Pooling 效率)");
    }

    void Update()
    {
        // 檢查鍵盤是否可用
        if (Keyboard.current == null) return;

        // BGM 測試 (Q 鍵)
        if (Keyboard.current.qKey.wasPressedThisFrame && testMusicData != null)
        {
            audioManager.PlayMusic(testMusicData);
            Debug.Log($"[Test] 播放 Music: {testMusicData.audioClip.name}");
        }

        // Ambient 測試 (W 鍵)
        if (Keyboard.current.wKey.wasPressedThisFrame && testAmbientData != null)
        {
            audioManager.PlayAmbient(testAmbientData);
            Debug.Log($"[Test] 播放 Ambient: {testAmbientData.audioClip.name}");
        }

        // UI 測試 (E 鍵 - 使用 PlayOneShot)
        if (Keyboard.current.eKey.wasPressedThisFrame && testUIData != null)
        {
            audioManager.PlayUISound(testUIData);
            Debug.Log($"[Test] 播放 UI Sound: {testUIData.audioClip.name}");
        }

        // 單個隨機 SFX 測試 (R 鍵 - 使用 Pool)
        if (Keyboard.current.rKey.wasPressedThisFrame && testSFXDatas.Length > 0)
        {
            audioManager.PlayRandomSFX(testSFXDatas);
            Debug.Log($"[Test] 播放 Random SFX (使用 Pool)");
        }

        // Master 音量測試 (Z 鍵 減)
        if (Keyboard.current.zKey.wasPressedThisFrame)
        {
            audioManager.MasterVolume = Mathf.Max(0f, audioManager.MasterVolume - 0.1f);
            Debug.Log($"[Test] Master Volume 調整為: {audioManager.MasterVolume:F2}");
        }
        
        // Master 音量測試 (X 鍵 加)
        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            audioManager.MasterVolume = Mathf.Min(1f, audioManager.MasterVolume + 0.1f);
            Debug.Log($"[Test] Master Volume 調整為: {audioManager.MasterVolume:F2}");
        }

        // SFX Pool 性能測試 (P 鍵)
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (testSFXDatas.Length == 0)
            {
                Debug.LogError("[Test] SFX 數據為空，無法進行性能測試。");
                return;
            }
            StartCoroutine(BurstPlaySFX());
        }
    }

    /// <summary>
    /// 協程：在短時間內密集播放大量 SFX，以測試音頻池的效率和限制。
    /// </summary>
    private System.Collections.IEnumerator BurstPlaySFX()
    {
        Debug.Log($"[Performance Test] 開始爆發播放 {burstAmount} 個 SFX (間隔 {burstInterval:F2}s)...");
        
        for (int i = 0; i < burstAmount; i++)
        {
            // 隨機選取一個音效播放
            AudioData randomData = testSFXDatas[Random.Range(0, testSFXDatas.Length)];
            audioManager.PlayRandomSFX(randomData);
            
            // 協程等待
            yield return new WaitForSeconds(burstInterval);
        }

        Debug.Log("[Performance Test] 爆發播放完成。");
    }
}