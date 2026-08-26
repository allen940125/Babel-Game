using UnityEngine;

public class EntityCore : MonoBehaviour
{
    [Header("資料來源 (唯讀藍圖)")]
    [SerializeField] private EntityBlueprintSO _blueprint;

    [Header("動態狀態 (執行期可見)")]
    [SerializeField] private EntityRuntime _runtimeData; 

    public EntityRuntime RuntimeData => _runtimeData;

    private void Awake()
    {
        // 這是預設行為：如果是全新的怪，就給他一個全新的大腦
        _runtimeData = new EntityRuntime();
        
        if (_blueprint != null)
        {
            _runtimeData.Initialize(_blueprint);
        }
    }

    // ==========================================
    // ★ 新增：允許外部系統「覆蓋」大腦的接口
    // ==========================================
    public void InjectRuntimeData(EntityRuntime existingData)
    {
        if (existingData == null) return;
        
        // 核心動作：把 Awake 剛做好的新大腦丟掉，換成大地圖傳遞過來的舊大腦！
        _runtimeData = existingData;
        
        Debug.Log($"<color=cyan>[EntityCore] 成功注入已存在的動態資料！當前血量: {_runtimeData.CurrentHealth}</color>");
    }
}