using Gamemanager;
using UnityEngine;
using DG.Tweening; // 必須確保專案已匯入 DOTween

[RequireComponent(typeof(SpriteRenderer))]
public class PhaseSpriteColorController : MonoBehaviour
{
    [Header("Phase Colors")]
    [SerializeField] private Color _idleColor = Color.green;
    [SerializeField] private Color _attackingColor = Color.red;
    
    [Header("Transition Settings")]
    [SerializeField] private float _transitionDuration = 0.5f;
    [SerializeField] private Ease _easeType = Ease.InOutSine;

    private SpriteRenderer _spriteRenderer;
    private Tween _colorTween;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // 初始狀態直接賦值，不需要動畫過渡
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _idleColor;
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterAttackingPhaseEvent, OnBossAttacking);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterIdlePhaseEvent, OnBossIdle);
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterAttackingPhaseEvent>(OnBossAttacking);
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterIdlePhaseEvent>(OnBossIdle);
        }
        
        // 腳本禁用時必須強制中止進行中的動畫，防止記憶體洩漏或空參考
        _colorTween?.Kill();
    }

    private void OnBossAttacking(BossEnterAttackingPhaseEvent evt)
    {
        TransitionToColor(_attackingColor);
    }

    private void OnBossIdle(BossEnterIdlePhaseEvent evt)
    {
        TransitionToColor(_idleColor);
    }

    private void TransitionToColor(Color targetColor)
    {
        if (_spriteRenderer == null) return;

        // ★ 核心原則：觸發新狀態前，必須清除前一次可能未完成的動畫
        _colorTween?.Kill();
        _colorTween = _spriteRenderer.DOColor(targetColor, _transitionDuration).SetEase(_easeType);
    }
}