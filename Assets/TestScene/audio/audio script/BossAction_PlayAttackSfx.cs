using Game.Audio;
using UnityEngine;

/// <summary>
/// 進入指定階段（Target Phase）時，播放攻擊音效。
/// - 全部留空 = 使用場景裡 AttackSfx 物件的預設音效（所有 Boss 共用）。
/// - 想讓這個 Boss 有自己的音效：在「專屬音效」填入 Audio Data（以及要的次數 / 間隔）。
/// 每個 Boss 各掛一份這個腳本，各填各的，就能有不同的攻擊音效。
/// </summary>
public class BossAction_PlayAttackSfx : BossPhaseAction
{
    [Header("專屬音效（可選，留空 = 用預設）")]
    [Tooltip("這個 Boss 專屬的攻擊音效。留空就使用 AttackSfx 物件上的預設音效。")]
    public AudioData customSfx;

    [Tooltip("連播次數。0 = 使用預設值。")]
    [Min(0)] public int repeatCount = 0;

    [Tooltip("每次之間的間隔（秒）。負數 = 使用預設值。")]
    public float interval = -1f;

    protected override void ExecuteAction()
    {
        Debug.Log($"<color=orange>[AttackSfx]</color> {gameObject.name} 進入階段 {targetPhase}，呼叫 AttackSfx.Play()");
        AttackSfx.Play(customSfx, repeatCount, interval);
    }
}