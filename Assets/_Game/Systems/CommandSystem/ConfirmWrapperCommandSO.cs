using UnityEngine;
using System;
using Game.UI;
using Cysharp.Threading.Tasks; // 必須引入 UniTask

[CreateAssetMenu(fileName = "Cmd_ConfirmWrapper", menuName = "Commands/Confirm Wrapper")]
public class ConfirmWrapperCommandSO : GameCommandSO
{
    [Header("★ 介面顯示設定")]
    public string confirmTitle = "系統確認";
    
    [TextArea(2, 5)]
    public string confirmMessage = "確定要執行此操作嗎？";

    [Header("★ 真正要執行的目標")]
    public GameCommandSO targetCommand;

    public override void Execute(CommandContext context)
    {
        if (targetCommand == null)
        {
            Debug.LogError($"[指令防呆] {name} 缺少 targetCommand！");
            return;
        }

        Action onConfirmAction = () => 
        {
            targetCommand.Execute(context); // 玩家按確定時，把最初的 Context 傳給真正的指令
        };

        if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
        {
            // 在同步方法中，呼叫非同步方法並加上 .Forget() 讓它在背景執行
            OpenUIAndInjectDataAsync(onConfirmAction).Forget();
        }
        else
        {
            Debug.LogError("[系統錯誤] 找不到 UIManager！");
        }
    }

    /// <summary>
    /// 專門用來等待 UIManager 非同步加載並注入資料的方法
    /// </summary>
    private async UniTaskVoid OpenUIAndInjectDataAsync(Action onConfirmAction)
    {
        // 1. 等待 UIManager 完成資源載入、生成，並拿回實例
        ConfirmPanel panel = await GameManager.Instance.UIManager.OpenPanel<ConfirmPanel>(UIType.ConfirmPanel);

        // 2. 防呆與資料注入
        if (panel != null)
        {
            // 將字串與委派塞進面板
            panel.ShowConfirm(confirmTitle, confirmMessage, onConfirmAction);
        }
        else
        {
            Debug.LogError($"[UI 錯誤] ConfirmPanel 開啟失敗或回傳為 null！");
        }
    }
}