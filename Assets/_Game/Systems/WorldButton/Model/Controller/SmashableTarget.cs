using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 泛用受擊模板。任何掛載此腳本的物件，都能被 DropImpactSensor 砸中並觸發事件。
/// </summary>
public class SmashableTarget : MonoBehaviour
{
    [Tooltip("當這個物件被砸中時，要執行什麼事情？\n(在 Inspector 中將 Boss 機關的 ForceTrigger() 拖入此處)")]
    public UnityEvent OnSmashed;

    // 供外部 (如 DropImpactSensor) 呼叫的觸發點
    public void TriggerSmash()
    {
        OnSmashed?.Invoke();
    }
}