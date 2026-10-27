using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

public class BossSpineVisualController : MonoBehaviour
{
    // ==========================================
    // 1. 定義與資料結構
    // ==========================================
    public enum VisualType { Spine, StaticSprite }

    [System.Serializable]
    public struct SpineVisualVariant
    {
        [Range(0f, 1f)] public float healthThreshold;
        
        [Header("視覺模式")]
        public VisualType visualType;

        [Header("★ 顏色與速度設定")]
        public Color targetColor;
        [Range(0f, 5f)] public float animationSpeed;

        [Header("Spine 設定")]
        [SpineSkin] public string skinName;
        [SpineAnimation] public string animationName;
        
        [Header("StaticSprite 設定")]
        public Sprite targetSprite;
    }

    [System.Serializable]
    public struct PhaseSpineConfig
    {
        public BossStateMachine.BossPhase phase;
        public bool loop;
        public List<SpineVisualVariant> variants;
    }

    [System.Serializable]
    public struct CustomSpineAction
    {
        public string actionName; 
        public bool loop;
        public List<SpineVisualVariant> variants;
    }

    [System.Serializable]
    public struct HitSpriteVariant
    {
        [Range(0f, 1f)] public float healthThreshold;
        public Sprite hitSprite;
    }

    // ==========================================
    // 2. 核心元件與參數配置
    // ==========================================
    [Header("核心元件")]
    [SerializeField] private BossStateMachine stateMachine;
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    
    [Header("受擊靜態圖片設定")]
    [SerializeField] private SpriteRenderer staticHitSprite;
    [SerializeField] private float hitFlashDuration = 0.15f;
    [SerializeField] private List<HitSpriteVariant> hitSpriteVariants;
    
    [Header("自動與手動排檔")]
    [SerializeField] private List<PhaseSpineConfig> phaseConfigs;
    [SerializeField] private List<CustomSpineAction> customActions;

    [Header("★ 測試專區")]
    public KeyCode testKey = KeyCode.T;
    public VisualType testVisualType = VisualType.Spine;
    public Color testColor = Color.white;
    public float testAnimationSpeed = 1f;
    [SpineSkin] public string testSkinName;
    [SpineAnimation] public string testAnimationName;
    public Sprite testSprite;
    public bool testLoop = false;

    // --- 內部狀態 ---
    private Dictionary<BossStateMachine.BossPhase, PhaseSpineConfig> _phaseDict;
    private Dictionary<string, CustomSpineAction> _customActionDict;
    private Coroutine _hitCoroutine;
    private bool _isFlashing = false;
    private BossStateMachine.BossPhase _pendingPhase;

    // ==========================================
    // 3. 初始化與事件綁定
    // ==========================================
    private void Awake()
    {
        if (stateMachine == null) stateMachine = GetComponentInParent<BossStateMachine>();
        if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();
        if (staticHitSprite != null) staticHitSprite.gameObject.SetActive(false);

        _phaseDict = new Dictionary<BossStateMachine.BossPhase, PhaseSpineConfig>();
        foreach (var config in phaseConfigs) _phaseDict[config.phase] = config;

        _customActionDict = new Dictionary<string, CustomSpineAction>();
        foreach (var action in customActions) _customActionDict[action.actionName] = action;
    }

    private void OnEnable()
    {
        if (stateMachine == null) return;
        stateMachine.OnPhaseEntered += HandlePhaseChanged;
            
        var healthComp = stateMachine.GetComponent<EntityHealthComponent>();
        if (healthComp != null) healthComp.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDisable()
    {
        if (stateMachine == null) return;
        stateMachine.OnPhaseEntered -= HandlePhaseChanged;
            
        var healthComp = stateMachine.GetComponent<EntityHealthComponent>();
        if (healthComp != null) healthComp.OnDamageTaken -= HandleDamageTaken;
    }

    private void Update()
    {
        if (Input.GetKeyDown(testKey)) TestSpineAction();
    }

    // ==========================================
    // 4. 階段與自定義動作路由
    // ==========================================
    private void HandlePhaseChanged(BossStateMachine.BossPhase newPhase)
    {
        if (newPhase == BossStateMachine.BossPhase.Dead)
        {
            if (_hitCoroutine != null) StopCoroutine(_hitCoroutine);
            _isFlashing = false;
            if (staticHitSprite != null) staticHitSprite.gameObject.SetActive(false);
        }
        else if (_isFlashing)
        {
            _pendingPhase = newPhase;
            return;
        }

        if (_phaseDict.TryGetValue(newPhase, out PhaseSpineConfig config))
        {
            PlayVariant(config.variants, config.loop);
        }
    }

    // ==========================================
    // 5. 受擊閃爍邏輯 
    // ==========================================
    private IEnumerator HitFlashRoutine()
    {
        _isFlashing = true; 
        _pendingPhase = stateMachine.CurrentPhase; 

        Sprite finalHitSprite = null;
        if (hitSpriteVariants != null && hitSpriteVariants.Count > 0)
        {
            float currentHpRatio = (float)stateMachine.BossRuntimeData.CurrentHealth / stateMachine.BossRuntimeData.MaxHealth;
            hitSpriteVariants.Sort((a, b) => a.healthThreshold.CompareTo(b.healthThreshold));
            
            finalHitSprite = hitSpriteVariants[0].hitSprite;
            foreach (var v in hitSpriteVariants)
            {
                if (currentHpRatio <= v.healthThreshold) { finalHitSprite = v.hitSprite; break; }
            }
        }

        if (finalHitSprite != null)
        {
            staticHitSprite.sprite = finalHitSprite;
            // 受擊強制設為純白，避免吃到其他變體的染色
            staticHitSprite.color = Color.white; 
            staticHitSprite.gameObject.SetActive(true);
            if (skeletonAnimation != null) skeletonAnimation.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(hitFlashDuration);

        _isFlashing = false; 
        
        if (staticHitSprite != null) staticHitSprite.gameObject.SetActive(false);

        if (stateMachine.BossRuntimeData.CurrentHealth > 0)
        {
            HandlePhaseChanged(_pendingPhase); 
        }
    }

    private void HandleDamageTaken(int damage)
    {
        if (staticHitSprite == null) 
        {
            Debug.LogError("[視覺控制器] staticHitSprite 遺失！");
            return;
        }
        if (staticHitSprite.gameObject.scene.rootCount == 0)
        {
            Debug.LogError("[視覺控制器] 嚴重錯誤：你把 Project 資料夾裡的 Prefab 拖進 staticHitSprite 了！請從 Hierarchy 拖曳子物件！");
            return;
        }

        if (_hitCoroutine != null) StopCoroutine(_hitCoroutine);
        _hitCoroutine = StartCoroutine(HitFlashRoutine());
    }
    
    // ==========================================
    // 6. 底層核心：資料運算與渲染執行
    // ==========================================
    private void PlayVariant(List<SpineVisualVariant> variants, bool loop)
    {
        if (variants == null || variants.Count == 0) return;

        float currentHpRatio = 1f;
        if (stateMachine != null && stateMachine.BossRuntimeData != null)
        {
            currentHpRatio = (float)stateMachine.BossRuntimeData.CurrentHealth / stateMachine.BossRuntimeData.MaxHealth;
        }

        variants.Sort((a, b) => a.healthThreshold.CompareTo(b.healthThreshold));

        SpineVisualVariant finalVariant = variants[0];
        foreach (var v in variants)
        {
            if (currentHpRatio <= v.healthThreshold) { finalVariant = v; break; }
        }

        ExecuteVisualVariant(finalVariant, loop);
    }

    private void ExecuteVisualVariant(SpineVisualVariant variant, bool loop)
    {
        if (variant.visualType == VisualType.Spine)
        {
            if (staticHitSprite != null) staticHitSprite.gameObject.SetActive(false);
            if (skeletonAnimation != null)
            {
                skeletonAnimation.gameObject.SetActive(true);
                
                // ★ 注入速度
                skeletonAnimation.timeScale = variant.animationSpeed;
                
                // ★ 注入顏色 (若 skeleton 已建立)
                if (skeletonAnimation.skeleton != null)
                {
                    skeletonAnimation.skeleton.SetColor(variant.targetColor);
                }

                if (!string.IsNullOrEmpty(variant.skinName))
                {
                    var renderer = skeletonAnimation.GetComponent<SkeletonRenderer>();
                    if (renderer != null)
                    {
                        renderer.initialSkinName = variant.skinName;
                        renderer.Initialize(true);
                        
                        // 初始化 Skin 可能會重置底層狀態，確保顏色被正確賦予
                        skeletonAnimation.skeleton.SetColor(variant.targetColor);
                    }
                }
                
                if (!string.IsNullOrEmpty(variant.animationName))
                {
                    skeletonAnimation.AnimationState.SetAnimation(0, variant.animationName, loop);
                }
            }
        }
        else if (variant.visualType == VisualType.StaticSprite)
        {
            if (skeletonAnimation != null) skeletonAnimation.gameObject.SetActive(false);
            if (staticHitSprite != null && variant.targetSprite != null)
            {
                staticHitSprite.gameObject.SetActive(true);
                staticHitSprite.sprite = variant.targetSprite;
                // ★ 注入靜態圖顏色
                staticHitSprite.color = variant.targetColor;
            }
        }
    }

    // ==========================================
    // 7. 測試指令
    // ==========================================
    [ContextMenu("★ 執行測試動作 (TestSpineAction)")]
    public void TestSpineAction()
    {
        Debug.Log($"<color=cyan>[視覺測試]</color> 執行模式: {testVisualType}");
        SpineVisualVariant dummyVariant = new SpineVisualVariant
        {
            visualType = testVisualType,
            targetColor = testColor,
            animationSpeed = testAnimationSpeed,
            skinName = testSkinName,
            animationName = testAnimationName,
            targetSprite = testSprite
        };
        ExecuteVisualVariant(dummyVariant, testLoop);
    }
}