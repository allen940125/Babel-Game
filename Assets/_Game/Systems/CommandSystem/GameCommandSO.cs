using System;
using Game.SceneManagement;
using Gamemanager; // 依賴你的事件系統
using UnityEngine;
using UnityEngine.Serialization;

// ==========================================
// 第一部分：定義介面 (這只是抽象合約，不是 SO)
// ==========================================
public interface ICommandAction
{
    void Invoke();
}

// ==========================================
// 第二部分：定義具體的行為 (這些是純資料類別，不是 SO！)
// ==========================================
[Serializable]
public class FightAction : ICommandAction
{
    // 戰鬥不需要參數，所以這裡完全空白
    public void Invoke()
    {
        Debug.Log("發送：戰鬥開始訊號");
        GameManager.Instance.MainGameEvent.Send(new FightButtonClickEvent());
    }
}

[Serializable]
public class ItemAction : ICommandAction
{
    // 道具需要參數，所以宣告在此
    public int itemId = 0;
    public int count = 1;

    public void Invoke()
    {
        Debug.Log($"發送：道具給予訊號，ID={itemId}, 數量={count}");
        // GameManager.Instance.MainGameEvent.Send(new ItemEvent(itemId, count));
    }
}

[Serializable]
public class SwitchSceneAction : ICommandAction
{
    // 道具需要參數，所以宣告在此
    public SceneType LoadSceneType;

    public void Invoke()
    {
        Debug.Log($"發送：切換場景{LoadSceneType}");
        GameManager.Instance.SceneTransitionManager.LoadScene(LoadSceneType);
    }
}


[Serializable]
public class PlayerHealAction : ICommandAction
{
    [FormerlySerializedAs("playerSO")] public EntityRuntime player; // 指定要對哪個 SO 執行
    public int healAmount = 30;

    public void Invoke()
    {
        if (player == null) return;
        
        HealPayload payload = new HealPayload()
        {
            HealAmount = this.healAmount,
            Source = null // 來自系統指令，無具體 GameObject 來源
        };
        
        if (player.TryGetTrait(out RuntimeAnchorTrait anchor))
        {
            anchor.TryHeal(payload);
        }
    }
}

[Serializable]
public class PlayerDamageAction : ICommandAction
{
    [FormerlySerializedAs("playerSO")] public EntityRuntime player;
    public int damageAmount = 50;
    public bool ignoreDefense = true; // 指令專屬設定：是否為真實傷害 (無視防禦)

    public void Invoke()
    {
        if (player == null) return;
        
        // 如果是無視防禦的真實傷害，可直接修改 SO 的血量 (跳過 EntityHealthComponent 的防禦計算)
        if (ignoreDefense)
        {
            //player.ModifyHealth(-damageAmount);
            Debug.Log($"[指令] 對玩家造成 {damageAmount} 點真實傷害");
        }
        else
        {
            DamagePayload payload = new DamagePayload() { Damage = this.damageAmount };
            //player.TryDamagePlayer(payload);
        }
    }
}

[Serializable]
public class ModifyPlayerStatAction : ICommandAction
{
    public enum StatType 
    { 
        MaxHealth, 
        MaxStamina, 
        MoveSpeed, 
        AttackPower,
        Defense
    }

    public enum ModifyType
    {
        Add,
        SetTo
    }

    [FormerlySerializedAs("playerSO")] public EntityRuntime player;
    public StatType statToModify;
    public ModifyType modifyType;
    public float value;

    public void Invoke()
    {
        if (player == null) return;

        switch (statToModify)
        {
            case StatType.MoveSpeed:
                // 這裡你需要先在 SO 裡寫好 SetMoveSpeed 方法
                // 否則無法修改 private 變數
                //float newSpeed = modifyType == ModifyType.Add ? player.MoveSpeed + value : value;
                //player.SetMoveSpeed(newSpeed); 
                break;
                
            case StatType.AttackPower:
                //int newAtk = modifyType == ModifyType.Add ? player.TotalAttackPower + (int)value : (int)value;
                //player.SetAttackPower(newAtk);
                break;
                
            // ... 依此類推擴充其他 Switch Case ...
        }

        Debug.Log($"[指令] 玩家狀態已變更：{statToModify} {(modifyType == ModifyType.Add ? "+" : "=")} {value}");
    }
}

[CreateAssetMenu(fileName = "Cmd_New", menuName = "Commands/Game Command")]
public class GameCommandSO : ScriptableObject
{
    [Header("指令識別 (供控制台與註解使用)")]
    public string commandName;       // 控制台指令名稱，例如 "fight", "give_sword"
    public string description;       // 功能說明，例如 "給予玩家指定ID的道具"

    [Header("執行邏輯插槽 (由企劃下拉選單配置)")]
    [SerializeReference] 
    public ICommandAction action;    // 指向具體的純資料行為類別

    /// <summary>
    /// 3D/2D/UI 按鈕或控制台觸發的唯一執行入口
    /// </summary>
    public void Execute()
    {
        if (action != null)
        {
            action.Invoke();
        }
        else
        {
            Debug.LogError($"[執行失敗] SO指令 '{name}' (指令代號: {commandName}) 尚未在 Inspector 配置 Action！");
        }
    }
}