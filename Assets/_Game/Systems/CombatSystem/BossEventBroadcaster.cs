using Gamemanager;
using UnityEngine;

public class BossEventBroadcaster : MonoBehaviour
{
    [SerializeField] private EntityRuntime entityData;

    private void Start()
    {
        entityData = GetComponent<EntityCore>().RuntimeData;
        // 監聽 Model 的血量變化
        if (entityData != null)
        {
            entityData.OnHealthRatioChanged += HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(float ratio)
    {
        Debug.Log($"HandleHealthChanged: {ratio}");
        // 只要血量變動（受擊），就發送全域震動事件
        GameManager.Instance.MainGameEvent.Send(new BossTakeDamageEvent 
        { 
            Intensity = 0.5f, 
            Duration = 0.2f 
        });

        // 發送瀕死狀態
        GameManager.Instance.MainGameEvent.Send(new BossLowHealthStateEvent 
        { 
            IsActive = ratio < 0.2f 
        });
    }

    private void OnDestroy()
    {
        if (entityData != null)
        {
            entityData.OnHealthRatioChanged -= HandleHealthChanged;
        }
    }
}