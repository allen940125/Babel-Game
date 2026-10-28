using UnityEngine;
using Gamemanager; 

[RequireComponent(typeof(RammingWeaponBehavior3D))]
public class SyringeMechanicAdapter : MonoBehaviour
{
    [Header("抽血針筒設定")]
    [Tooltip("每個階段最多可以吸血/補血的總循環次數")]
    [SerializeField] private int maxCyclesPerPhase = 2;
    
    [Tooltip("★ 當扎到 Interaction Layer (玩家) 時執行的指令")]
    [SerializeField] private GameCommandSO healCommand;
    
    [Header("★ 視覺控制 (Shader Fill)")]
    [SerializeField] private ShaderFillVisualizer fillVisualizer;
    
    private RammingWeaponBehavior3D _weapon;
    
    private int _currentHealCount = 0;
    private bool _hasBlood = true; 

    private void Awake()
    {
        _weapon = GetComponent<RammingWeaponBehavior3D>();

        // 訂閱武器的兩個新事件
        _weapon.OnInteractionHit.AddListener(HandleInteractionHit);
        _weapon.OnRammingSuccess.AddListener(HandleEnemyStabbed);
        
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(
                GameManager.Instance.MainGameEvent.OnBossEnterIdlePhaseEvent, 
                ResetSyringeForNewPhase
            );
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterIdlePhaseEvent>(ResetSyringeForNewPhase);
        }
    }

    private void Start()
    {
        UpdateSyringeState();
    }

    // ==========================================
    // 1. 扎入互動目標 (玩家)
    // ==========================================
    private void HandleInteractionHit(GameObject targetObject)
    {
        if (_hasBlood)
        {
            _hasBlood = false;
            _currentHealCount++;
            
            Debug.Log($"<color=cyan>[針筒] 注射成功！目標: {targetObject.name}。次數: {_currentHealCount}/{maxCyclesPerPhase}</color>");
            
            if (healCommand != null)
            {
                healCommand.Execute(targetObject); // 對目標執行指令
            }
            
            UpdateSyringeState();
        }
        else
        {
            Debug.Log($"<color=gray>[針筒] 空的！對 {targetObject.name} 扎針無效。</color>");
        }
    }

    // ==========================================
    // 2. 扎入傷害目標 (敵人)
    // ==========================================
    private void HandleEnemyStabbed(GameObject targetEnemy)
    {
        if (!_hasBlood && _currentHealCount < maxCyclesPerPhase)
        {
            _hasBlood = true;
            Debug.Log($"<color=green>[針筒] 成功從 {targetEnemy.name} 抽出鮮血！針筒變綠！</color>");
            UpdateSyringeState();
        }
    }

    private void ResetSyringeForNewPhase(BossEnterIdlePhaseEvent evt)
    {
        _currentHealCount = 0;
        _hasBlood = true;
        UpdateSyringeState();
    }

    private void UpdateSyringeState()
    {
        if (fillVisualizer != null)
        {
            float targetFill = _hasBlood ? 1.0f : 0.0f;
            fillVisualizer.SetFillAmount(targetFill);
        }
    }
}