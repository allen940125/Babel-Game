using Gamemanager;
using UnityEngine;

public class FightButtonController3D : BaseButtonController3D
{
    [Header("綁定的全域指令")]
    [SerializeField] private GameCommandSO targetCommand;
    
    protected override void OnButtonClicked()
    {
        Debug.Log("發送：戰鬥開始訊號");
        GameManager.Instance.MainGameEvent.Send(new FightButtonClickEvent());
    }
}