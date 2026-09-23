using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AttackPatternBase : MonoBehaviour
{
    [Header("★ 基礎設定 (全發射器共用)")]
    [Tooltip("彈幕發射完畢後，是否自動銷毀此發射器？(勾選可保持場景乾淨)")]
    public bool destroyOnFinish = true;

    // ★ 新增空間定位系統
    [Header("★ 空間定位 (Spatial Positioning)")]
    [Tooltip("勾選後，此發射器生成時會無視 Boss 的槍口位置，強制將自身移動到世界座標 (0,0,0)，並重置旋轉。非常適合全畫面陣型或序列。")]
    public bool snapToWorldOrigin = false;
    
    [Header("★ 預警系統 (Telegraph)")]
    [Tooltip("是否啟用發射前警告？")]
    public bool useWarning = false;
    [Tooltip("警告持續時間 (秒)")]
    public float warningTime = 1.0f;
    [Tooltip("要生成的警告 Prefab (如紅色射線、驚嘆號標誌)")]
    public GameObject warningPrefab;
    
    protected BossStateMachine _ownerBoss;

// ==========================================
    // 遊戲執行時的生命週期
    // ==========================================
    public void Execute(BossStateMachine boss, float speedMultiplier, bool isAngry)
    {
        _ownerBoss = boss;

        // ★ 核心攔截：在子類別執行任何數學運算前，強制校正空間位置
        if (snapToWorldOrigin)
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }

        OnExecute(boss, speedMultiplier, isAngry);
    }

    protected abstract void OnExecute(BossStateMachine boss, float speedMultiplier, bool isAngry);
    

    /// <summary>
    /// ★ 統一的結束方法：子類別發射完畢後，呼叫此方法即可，不要自己寫 Destroy()
    /// </summary>
    protected void FinishPattern()
    {
        if (destroyOnFinish)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (_ownerBoss != null)
        {
            _ownerBoss.UnregisterActivePattern(this.gameObject);
        }
    }

    // ==========================================
    // ★ 編輯器預覽系統 (統一由基底類別管理)
    // ==========================================
    
    private const string PREVIEW_CONTAINER_NAME = "--- Preview Container ---";

    [ContextMenu("👁️ 生成預覽 (預覽彈幕軌跡)")]
    public void GenerateEditorPreview()
    {
        ClearEditorPreview(); // 永遠先清除舊的，確保按第二次就是刷新

        // 建立一個專屬容器來放預覽物件
        GameObject container = new GameObject(PREVIEW_CONTAINER_NAME);
        container.transform.SetParent(this.transform);
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;
        
        // ★ 防呆標記：設定為不存檔，避免你存 Prefab 的時候把預覽垃圾也存進去
        container.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

        // 呼叫子類別，請子類別把預覽物件塞進這個容器
        OnGeneratePreview(container.transform);
        
        Debug.Log($"<color=cyan>[預覽成功]</color> 已在 {gameObject.name} 產生預覽幾何體！");
    }

    [ContextMenu("❌ 清除預覽")]
    public void ClearEditorPreview()
    {
        Transform oldContainer = transform.Find(PREVIEW_CONTAINER_NAME);
        if (oldContainer != null)
        {
            // 在 Editor 模式下必須用 DestroyImmediate
            DestroyImmediate(oldContainer.gameObject); 
        }
    }

    /// <summary>
    /// 強迫所有子類別實作：你該如何畫出你的預覽軌跡？
    /// </summary>
    protected abstract void OnGeneratePreview(Transform previewContainer);

    /// <summary>
    /// ★ 共用工具：給子類別呼叫，用來生成一個「長條形的假子彈」來指示位置與方向
    /// </summary>
    protected void CreatePreviewDummy(Transform container, Vector3 position, Vector3 direction)
    {
        // 創建一個內建的 Cube
        GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dummy.name = "Preview Bullet";
        dummy.transform.SetParent(container);
        dummy.transform.position = position;
        
        // 壓扁變成長條狀，像雷射或箭頭
        dummy.transform.localScale = new Vector3(0.1f, 0.4f, 0.1f);

        // 旋轉指向方向 (2D Z軸旋轉)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        dummy.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        // 把 Collider 刪掉，避免干擾你在 Editor 點擊其他東西
        DestroyImmediate(dummy.GetComponent<Collider>());
    }
    
    // 定義一個標準資料包，用來鎖死「算出來的位置與方向」
    public struct SpawnData
    {
        public Vector2 position;
        public Vector2 direction;
    }
    
    // ==========================================
    // ★ 核心：統一的預警處理協程
    // ==========================================
    /// <summary>
    /// 傳入算好的點位，此方法會自動生成警告、等待、並在結束時精準銷毀警告
    /// </summary>
    protected IEnumerator ShowWarningsAndWait(List<SpawnData> spawnDataList)
    {
        if (!useWarning || warningTime <= 0f || warningPrefab == null || spawnDataList.Count == 0) 
        {
            yield break; // 沒開警告，直接跳過不等待
        }

        List<GameObject> activeWarnings = new List<GameObject>();

        // 1. 在每個預定發射點生成警告圖示
        foreach (var data in spawnDataList)
        {
            GameObject warningObj = Instantiate(warningPrefab, data.position, Quaternion.identity);
            
            // 將警告圖示旋轉至與子彈同向 (如果是指向性警告線才需要)
            float angle = Mathf.Atan2(data.direction.y, data.direction.x) * Mathf.Rad2Deg;
            warningObj.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            
            activeWarnings.Add(warningObj);
        }

        // 2. 嚴格鎖死等待時間
        yield return new WaitForSeconds(warningTime);

        // 3. 時間一到，由程式強制抹殺所有警告，確保與下一行發射子彈的程式碼絕對同步
        foreach (var w in activeWarnings)
        {
            if (w != null) Destroy(w);
        }
    }
}