using UnityEngine;
using Game.Audio; // 引入你的音效系統命名空間

public class BossAction_PlayBGM : BossPhaseAction
{
    [Header("BGM 設定")]
    [Tooltip("進入此階段時，要播放的背景音樂")]
    public AudioData phaseBGM;

    protected override void ExecuteAction()
    {
        if (phaseBGM != null && phaseBGM.audioClip != null)
        {
            AudioManager.Instance.PlayMusic(phaseBGM);
            Debug.Log($"<color=magenta>[音效切換]</color> 進入階段 {targetPhase}，開始播放 Boss 音樂: {phaseBGM.audioClip.name}");
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 嘗試播放 Boss BGM，但未設定 AudioData！");
        }
    }
}