using UnityEngine;
using UnityEngine.Rendering.Universal;
using Gamemanager; // 引入你的 GameManager 命名空間

public class RenderFeatureController : MonoBehaviour
{
    [Header("Render Feature 設定")]
    [SerializeField] private UniversalRendererData rendererData;
    [SerializeField] private string featureName = "FullScreenPassRendererFeature"; 
    
    [Header("狀態顏色設定")]
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color attackingColor = Color.red;

    private Material featureMaterial;

    void Start()
    {
        // 1. 抓取 Material
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature.name == featureName && feature is FullScreenPassRendererFeature fullScreenFeature)
            {
                featureMaterial = fullScreenFeature.passMaterial;
                break;
            }
        }

        // 2. 註冊全域事件 (由這個腳本統一監聽，只會呼叫一次)
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(
                GameManager.Instance.MainGameEvent.OnBossEnterAttackingPhaseEvent, 
                OnBossAttacking
            );
            GameManager.Instance.MainGameEvent.SetSubscribe(
                GameManager.Instance.MainGameEvent.OnBossEnterIdlePhaseEvent, 
                OnBossIdle
            );
        }

        // 初始化預設顏色
        ChangeOutlineColor(idleColor);
    }

    void OnDestroy()
    {
        // 嚴格解除註冊
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterAttackingPhaseEvent>(OnBossAttacking);
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterIdlePhaseEvent>(OnBossIdle);
        }
    }

    private void OnBossAttacking(BossEnterAttackingPhaseEvent evt)
    {
        ChangeOutlineColor(attackingColor);
    }

    private void OnBossIdle(BossEnterIdlePhaseEvent evt)
    {
        ChangeOutlineColor(idleColor);
    }

    public void ChangeOutlineColor(Color newColor)
    {
        if (featureMaterial != null)
        {
            featureMaterial.SetColor("_OutlineColor", newColor);
        }
    }
}