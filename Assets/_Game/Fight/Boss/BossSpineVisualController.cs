using UnityEngine;
using Spine.Unity;
using System.Collections.Generic;

public class BossSpineVisualController : MonoBehaviour
{
    // ==========================================
    // 核心資料結構：血量變體
    // ==========================================
    [System.Serializable]
    public struct SpineVisualVariant
    {
        [Tooltip("當血量比例低於或等於此值時觸發。例如填 1.0 是滿血正常狀態，填 0.5 代表血量低於 50% 時的狂暴狀態")]
        [Range(0f, 1f)] public float healthThreshold;
        
        [SpineSkin] public string skinName;
        [SpineAnimation] public string animationName;
    }

    [System.Serializable]
    public struct PhaseSpineConfig
    {
        public BossStateMachine.BossPhase phase;
        public bool loop;
        public List<SpineVisualVariant> variants; // ★ 升級為陣列
    }

    [System.Serializable]
    public struct CustomSpineAction
    {
        public string actionName; 
        public bool loop;
        public List<SpineVisualVariant> variants; // ★ 升級為陣列
    }

    // ==========================================
    // 變數與初始化
    // ==========================================
    [Header("核心元件")]
    [SerializeField] private BossStateMachine stateMachine;
    [SerializeField] private SkeletonAnimation skeletonAnimation;

    [Header("自動排檔 (階段切換)")]
    [SerializeField] private List<PhaseSpineConfig> phaseConfigs;
    
    [Header("手動排檔 (特殊動作)")]
    [SerializeField] private List<CustomSpineAction> customActions;

    private Dictionary<BossStateMachine.BossPhase, PhaseSpineConfig> _phaseDict;
    private Dictionary<string, CustomSpineAction> _customActionDict;

    private void Awake()
    {
        if (stateMachine == null) stateMachine = GetComponentInParent<BossStateMachine>();
        if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();

        _phaseDict = new Dictionary<BossStateMachine.BossPhase, PhaseSpineConfig>();
        foreach (var config in phaseConfigs) _phaseDict[config.phase] = config;

        _customActionDict = new Dictionary<string, CustomSpineAction>();
        foreach (var action in customActions) _customActionDict[action.actionName] = action;
    }

    private void OnEnable()
    {
        if (stateMachine != null) stateMachine.OnPhaseEntered += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        if (stateMachine != null) stateMachine.OnPhaseEntered -= HandlePhaseChanged;
    }

    // ==========================================
    // 核心邏輯：自動計算當前血量並挑選最適合的變體
    // ==========================================
    private void HandlePhaseChanged(BossStateMachine.BossPhase newPhase)
    {
        if (_phaseDict.TryGetValue(newPhase, out PhaseSpineConfig config))
        {
            PlayVariant(config.variants, config.loop);
        }
    }

    public void PlayCustomAction(string actionName)
    {
        if (_customActionDict.TryGetValue(actionName, out CustomSpineAction action))
        {
            PlayVariant(action.variants, action.loop);
        }
        else
        {
            Debug.LogWarning($"[Spine] 找不到自定義動作: {actionName}");
        }
    }

    /// <summary>
    /// 根據當前血量，智慧挑選出應該播放的 Skin 與 Animation
    /// </summary>
    private void PlayVariant(List<SpineVisualVariant> variants, bool loop)
    {
        if (variants == null || variants.Count == 0) return;

        // 1. 取得當前 Boss 血量比例 (預設為 1.0 滿血)
        float currentHpRatio = 1f;
        if (stateMachine != null && stateMachine.BossRuntimeData != null)
        {
            currentHpRatio = (float)stateMachine.BossRuntimeData.CurrentHealth / stateMachine.BossRuntimeData.MaxHealth;
        }

        // 2. 將變體陣列依照「血量門檻」從小到大排序 (例如: 0.3 -> 0.7 -> 1.0)
        variants.Sort((a, b) => a.healthThreshold.CompareTo(b.healthThreshold));

        // 3. 尋找符合條件的變體
        SpineVisualVariant finalVariant = variants[0];
        foreach (var v in variants)
        {
            if (currentHpRatio <= v.healthThreshold)
            {
                finalVariant = v;
                break;
            }
        }

        // 4. 正式播放
        PlaySpecialAction(finalVariant.animationName, loop, finalVariant.skinName);
    }

    public void PlaySpecialAction(string animName, bool loop = false, string tempSkin = "")
    {
        if (!string.IsNullOrEmpty(tempSkin))
        {
            var renderer = skeletonAnimation.GetComponent<SkeletonRenderer>();
            if (renderer != null)
            {
                renderer.initialSkinName = tempSkin;
                renderer.Initialize(true);
            }
        }
        
        if (!string.IsNullOrEmpty(animName))
        {
            skeletonAnimation.AnimationState.SetAnimation(0, animName, loop);
        }
    }
}