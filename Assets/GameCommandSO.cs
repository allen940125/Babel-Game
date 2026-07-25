using System;
using Gamemanager; // 依賴你的事件系統
using UnityEngine;

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