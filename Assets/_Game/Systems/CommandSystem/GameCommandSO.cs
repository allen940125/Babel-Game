using System;
using Game.SceneManagement;
using Game.UI;
using Gamemanager;
using UnityEngine;
using UnityEngine.Serialization;

public class CommandContext
{
    public GameObject TriggerSource; 
    public object Payload; 

    public bool TryGetPayload<T>(out T data) where T : class
    {
        data = Payload as T;
        return data != null;
    }
}

public interface ICommandAction
{
    // 強制所有指令都必須接收 Context
    void Invoke(CommandContext context);
}

// ==========================================
// 具體行為
// ==========================================

[Serializable]
public class FightAction : ICommandAction
{
    public void Invoke(CommandContext context)
    {
        Debug.Log("發送：戰鬥開始訊號");
        GameManager.Instance.MainGameEvent.Send(new FightButtonClickEvent());
    }
}

[Serializable]
public class ItemAction : ICommandAction
{
    [Header("測試與預設值 (若無動態資料則使用此值)")]
    public int defaultItemId = 0;
    public int defaultCount = 1;

    public void Invoke(CommandContext context)
    {
        // 1. 預設使用 Inspector 填寫的數值 (支援 CMD 測試)
        int targetId = defaultItemId;
        int targetCount = defaultCount;

        // 2. 如果有 Context，且裡面裝有動態物品資料，則「覆蓋」預設值
        if (context != null && context.TryGetPayload<InventoryItemRuntimeData>(out var itemData))
        {
            targetId = itemData.itemId;
            targetCount = itemData.quantity;
            Debug.Log("[ItemAction] 偵測到動態 Payload，使用動態數值。");
        }
        else
        {
            Debug.Log("[ItemAction] 無動態 Payload，使用 Inspector 預設數值。");
        }

        Debug.Log($"發送：道具給予訊號，ID={targetId}, 數量={targetCount}");
        GameManager.Instance.MainGameEvent.Send(new ItemAddedToBagEvent() 
        { 
            ItemID = targetId, 
            Quantity = targetCount 
        }); 
    }
}

[Serializable]
public class OpenBagpackAction : ICommandAction
{
    public void Invoke(CommandContext context)
    {
        Debug.Log($"發送：背包打開指令");
        GameManager.Instance.UIManager.OpenPanel<BagMenu>(UIType.BagMenu);
    }
}

[Serializable]
public class SwitchSceneAction : ICommandAction
{
    public SceneType LoadSceneType;

    public void Invoke(CommandContext context)
    {
        Debug.Log($"發送：切換場景{LoadSceneType}");
        GameManager.Instance.SceneTransitionManager.LoadScene(LoadSceneType);
    }
}

public enum CommandTargetType
{
    TriggerSource, // 誰觸發這個指令，就對誰生效 (適用：陷阱、範圍Buff、掉落的補血包)
    MainPlayer,    // 無視觸發者，強制對主控玩家生效 (適用：UI 背包喝水、跨遠端開關給玩家Buff)
    CurrentBoss    // 無視觸發者，強制對當前 Boss 生效 (適用：場景機關對 Boss 造成傷害)
}

[Serializable]
public class HealAction : ICommandAction // 拿掉 Player 前綴，因為現在通用了
{
    [Header("目標設定")]
    public CommandTargetType targetType = CommandTargetType.TriggerSource;
    public int healAmount = 30;

    public void Invoke(CommandContext context)
    {
        // 1. 決定目標實體
        EntityRuntime targetData = GetTargetEntity(context);
        
        if (targetData == null)
        {
            Debug.LogWarning($"<color=#F44747>[HealAction 失敗]</color> 找不到目標實體！" +
                             $"\n- TargetType 設定: {targetType}" +
                             $"\n- TriggerSource: {(context != null && context.TriggerSource != null ? context.TriggerSource.name : "null")}");
            return;
        }

        // 2. 決定數值來源 (支援 CSV BaseValue 或 Inspector 預設數值)
        int finalHeal = healAmount;
        string valueSource = "SO 預設值";

        if (context != null && context.TryGetPayload<InventoryItemRuntimeData>(out var itemData))
        {
            finalHeal = itemData.BaseTemplete.BaseValue;
            valueSource = $"道具 CSV 範本 ({itemData.BaseTemplete.Name})";
        }

        // 3. 記錄治療前狀態
        int healthBefore = targetData.CurrentHealth;

        // 4. 執行治療
        HealPayload payload = new HealPayload()
        {
            HealAmount = finalHeal,
            Source = null
        };
        
        if (targetData.TryGetTrait(out RuntimeAnchorTrait anchor))
        {
            anchor.TryHeal(payload);

            int healthAfter = targetData.CurrentHealth;
            int actualHealed = healthAfter - healthBefore;

            Debug.Log($"<color=#57A64A>[HealAction 成功]</color> 執行治療：" +
                      $"\n- 目標模式: <b>{targetType}</b>" +
                      $"\n- 來源: {valueSource}" +
                      $"\n- 預計回復: <b>+{finalHeal}</b>" +
                      $"\n- 血量變動: <color=yellow>{healthBefore}</color> -> <color=#4EC9B0>{healthAfter}</color> (實質增加: {actualHealed})");
        }
        else
        {
            Debug.LogError($"<color=#F44747>[HealAction 失敗]</color> 目標實體缺少 RuntimeAnchorTrait，無法分發 TryHeal！");
        }
    }

    // ★ 將尋找目標的邏輯完全封裝，根據企劃選的 Enum 來決定怎麼找大腦
    private EntityRuntime GetTargetEntity(CommandContext context)
    {
        switch (targetType)
        {
            case CommandTargetType.TriggerSource:
                // 誰踩到就找誰的大腦
                if (context != null && context.TriggerSource != null)
                {
                    EntityCore core = context.TriggerSource.GetComponentInParent<EntityCore>();
                    if (core != null) return core.RuntimeData;
                }
                return null;

            case CommandTargetType.MainPlayer:
                // 無視是誰踩的，直接找全局 GameManager 要玩家大腦
                return GameManager.Instance.MainGameMediator.CurrentPlayerRuntime;

            case CommandTargetType.CurrentBoss:
                // 無視是誰踩的，直接找全局 GameManager 要 Boss 大腦
                // (假設你的 Mediator 有提供 CurrentBossRuntime)
                return GameManager.Instance.MainGameMediator.CurrentBossRuntime; 

            default:
                return null;
        }
    }
}

[Serializable]
public class PlayerDamageAction : ICommandAction
{
    public int damageAmount = 50;
    public bool ignoreDefense = true;

    public void Invoke(CommandContext context)
    {
        // 使用與補血相同的尋找邏輯，移除錯誤的 public EntityRuntime 欄位
        EntityRuntime targetPlayer = GetTargetPlayer(context);
        if (targetPlayer == null) return;
        
        if (ignoreDefense)
        {
            Debug.Log($"[指令] 對玩家造成 {damageAmount} 點真實傷害 (需實作扣血方法)");
        }
        else
        {
            DamagePayload payload = new DamagePayload() { Damage = this.damageAmount };
            //targetPlayer.TryDamagePlayer(payload);
        }
    }

    private EntityRuntime GetTargetPlayer(CommandContext context)
    {
        if (context != null && context.TriggerSource != null)
        {
            if (context.TriggerSource.TryGetComponent<EntityRuntime>(out var entity)) return entity;
        }
        return GameManager.Instance.MainGameMediator.CurrentPlayerRuntime;
    }
}

[Serializable]
public class ModifyPlayerStatAction : ICommandAction
{
    public enum StatType { MaxHealth, MaxStamina, MoveSpeed, AttackPower, Defense }
    public enum ModifyType { Add, SetTo }

    public StatType statToModify;
    public ModifyType modifyType;
    public float value;

    public void Invoke(CommandContext context)
    {
        EntityRuntime targetPlayer = GetTargetPlayer(context);
        if (targetPlayer == null) return;

        switch (statToModify)
        {
            case StatType.MoveSpeed:
                break;
            case StatType.AttackPower:
                break;
        }

        Debug.Log($"[指令] 玩家狀態已變更：{statToModify} {(modifyType == ModifyType.Add ? "+" : "=")} {value}");
    }

    private EntityRuntime GetTargetPlayer(CommandContext context)
    {
        if (context != null && context.TriggerSource != null)
        {
            if (context.TriggerSource.TryGetComponent<EntityRuntime>(out var entity)) return entity;
        }
        return GameManager.Instance.MainGameMediator.CurrentPlayerRuntime;
    }
}

// ==========================================
// SO 定義
// ==========================================

[CreateAssetMenu(fileName = "Cmd_New", menuName = "Commands/Game Command")]
public class GameCommandSO : ScriptableObject
{
    [Header("指令識別 (供控制台與註解使用)")]
    public string commandName;
    public string description;

    [Header("執行邏輯插槽 (由企劃下拉選單配置)")]
    [SerializeReference] 
    public ICommandAction[] actions;

    /// <summary>
    /// 3D/2D/UI 按鈕或控制台觸發的唯一執行入口
    /// 如果是從 CMD 直接呼叫，可以傳入 null (例如 cmd.Execute(null);)
    /// </summary>
    public virtual void Execute(CommandContext context)
    {
        if (actions == null || actions.Length == 0) return;

        foreach (var act in actions)
        {
            act?.Invoke(context);
        }
    }

    // ★ 2. 封裝多載 A：最常用的方法 (傳入觸發者，可選傳入 Payload)
    public void Execute(GameObject triggerSource, object payload = null)
    {
        CommandContext ctx = new CommandContext()
        {
            TriggerSource = triggerSource,
            Payload = payload
        };
        Execute(ctx);
    }

    // ★ 3. 封裝多載 B：給 UI 按鈕的 UnityEvent 或是完全不需要參數的 CMD 測試用
    public void Execute()
    {
        Execute(null, null);
    }
}