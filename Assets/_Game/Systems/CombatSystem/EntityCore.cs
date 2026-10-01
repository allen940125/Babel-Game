using UnityEngine;

public class EntityCore : MonoBehaviour
{
    [Header("資料來源 (唯讀藍圖)")]
    [SerializeField] private EntityBlueprintSO _blueprint;

    [SerializeField] private EntityRuntime _runtimeData; 
    public EntityRuntime RuntimeData => _runtimeData;

    // ★ 快取所有依賴此大腦的組件
    private IEntityRuntimeDependent[] _dependents;

    private void Awake()
    {
        // 掃描包含自己在內，以及所有子物件中實作了該介面的組件 (包含隱藏物件)
        _dependents = GetComponentsInChildren<IEntityRuntimeDependent>(true);

        _runtimeData = new EntityRuntime();
        if (_blueprint != null)
        {
            _runtimeData.Initialize(_blueprint);
            BroadcastDataChange(); // 初始化完成，發送廣播
        }
    }

    public void InjectRuntimeData(EntityRuntime existingData)
    {
        if (existingData == null) return;
        
        _runtimeData = existingData;
        BroadcastDataChange(); // 替換完成，發送廣播
    }

    // 統一的推播中心
    private void BroadcastDataChange()
    {
        foreach (var dep in _dependents)
        {
            dep.OnRuntimeDataChanged(_runtimeData);
        }
    }
}