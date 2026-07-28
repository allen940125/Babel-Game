using UnityEngine;
using UnityEngine.UI;
using Gamemanager;

public class UIHUD : MonoBehaviour
{
    [Header("玩家 UI 設定")]
    [Tooltip("請拖入玩家血量 Slider (建議 MaxValue 設為 100)")]
    [SerializeField] private Slider playerHealthSlider;
    [Tooltip("請拖入玩家體力 Slider (建議 MaxValue 設為 100)")]
    [SerializeField] private Slider playerStaminaSlider;

    [Header("Boss UI 設定")]
    [Tooltip("Boss 血條的整個 CanvasGroup 或 GameObject Container (未開戰時可隱藏)")]
    [SerializeField] private GameObject bossHealthBarContainer;
    [Tooltip("請拖入 Boss 血量 Slider")]
    [SerializeField] private Slider bossHealthSlider;

    private void OnEnable()
    {
        if (GameManager.Instance?.MainGameEvent != null)
        {
            // 監聽玩家狀態
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnPlayerHealthChangedEvent, OnPlayerHealthChanged);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnPlayerStaminaChangedEvent, OnPlayerStaminaChanged);

            // ★ 監聽 Boss 狀態與血量
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossHealthChangedEvent, OnBossHealthChanged);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterSpecialPhaseEvent, OnBossEnterCombat);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterIdlePhaseEvent, OnBossEnterIdle);
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
            GameManager.Instance.MainGameEvent.Unsubscribe<PlayerStaminaChangedEvent>(OnPlayerStaminaChanged);

            GameManager.Instance.MainGameEvent.Unsubscribe<BossHealthChangedEvent>(OnBossHealthChanged);
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterSpecialPhaseEvent>(OnBossEnterCombat);
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterIdlePhaseEvent>(OnBossEnterIdle);
        }
    }

    private void Start()
    {
        // 遊戲起始時，依據標準規範預設隱藏 Boss 血條
        if (bossHealthBarContainer != null)
        {
            bossHealthBarContainer.SetActive(false);
        }
    }

    // ==========================================
    // 玩家 UI 事件回調 (數值制 / 百分比)
    // ==========================================

    private void OnPlayerHealthChanged(PlayerHealthChangedEvent evt)
    {
        if (playerHealthSlider == null) return;

        // 直接更新最大值與當前值，Slider 會自動幫你做百分比填充
        playerHealthSlider.maxValue = evt.MaxHealth;
        playerHealthSlider.value = evt.CurrentHealth;
    }

    private void OnPlayerStaminaChanged(PlayerStaminaChangedEvent evt)
    {
        if (playerStaminaSlider == null) return;

        playerStaminaSlider.maxValue = evt.MaxStamina;
        playerStaminaSlider.value = evt.CurrentStamina;
    }

    // ==========================================
    // ★ Boss UI 事件回調
    // ==========================================

    private void OnBossHealthChanged(BossHealthChangedEvent evt)
    {
        if (bossHealthSlider == null) return;

        bossHealthSlider.maxValue = evt.MaxHealth;
        bossHealthSlider.value = evt.CurrentHealth;
    }

    // 當 Boss 進入特殊階段 (開戰) 時，顯示 Boss 血條
    private void OnBossEnterCombat(BossEnterSpecialPhaseEvent evt)
    {
        if (bossHealthBarContainer != null)
        {
            bossHealthBarContainer.SetActive(true);
        }
    }

    // 當 Boss 死亡或回到 Idle 時，隱藏 Boss 血條
    private void OnBossEnterIdle(BossEnterIdlePhaseEvent evt)
    {
        if (bossHealthBarContainer != null)
        {
            bossHealthBarContainer.SetActive(false);
        }
    }
}