using Gamemanager;
using UnityEngine;

/// <summary>
/// 通用指令按鈕控制器：取代所有 FightButton, TalkButton 等碎片化子類別
/// </summary>
public class CommandButtonController3D : BaseButtonController3D
{
    [Header("綁定的全域指令")]
    [SerializeField] private GameCommandSO targetCommand;

    protected override void OnButtonClicked()
    {
        if (targetCommand != null)
        {
            Debug.Log($"[3D按鈕觸發] 執行綁定指令: {targetCommand.commandName}");
            // 轉交給 SO 執行其內部的 GameManager.Send 邏輯
            targetCommand.Execute();
        }
        else
        {
            Debug.LogError($"[錯誤] {gameObject.name} 未綁定任何 GameCommandSO！");
        }
    }
}