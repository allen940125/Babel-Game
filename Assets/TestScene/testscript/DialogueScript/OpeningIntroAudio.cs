using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class OpeningIntroAudio : MonoBehaviour
{
    [Header("音效素材")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip doorOpenClip;
    [SerializeField] private AudioClip doorCloseClip;
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private AudioClip chairClip;

    [Header("腳步聲設定")]
    [SerializeField] private int footstepCount = 5;
    [SerializeField] private float footstepInterval = 0.4f;
    [SerializeField] private float footstepVolumeStart = 0.3f;
    [SerializeField] private float footstepVolumeEnd = 1.0f;

    [Header("黑畫面")]
    [Tooltip("開場音效播放前先蓋上全螢幕黑幕，並且在『開場對話的黑底完全蓋上』之前不放開，避免音效結束後閃出 Skybox。")]
    [SerializeField] private bool holdBlackout = true;

    private void Awake()
    {
        // 場景一載入就黑，不等 PlayIntroSequenceAsync 被呼叫（避免第一幀露出 Skybox）
        if (holdBlackout) ScreenBlackout.Hold();
    }

    public async UniTask PlayIntroSequenceAsync()
    {
        if (holdBlackout) ScreenBlackout.Hold();

        // 1. 開門
        audioSource.PlayOneShot(doorOpenClip);
        await UniTask.Delay(TimeSpan.FromSeconds(doorOpenClip.length));

        // 2. 關門
        audioSource.PlayOneShot(doorCloseClip);
        await UniTask.Delay(TimeSpan.FromSeconds(doorCloseClip.length));

        // 3. 腳步聲，音量由弱至強，重複 footstepCount 次
        for (int i = 0; i < footstepCount; i++)
        {
            float t = footstepCount <= 1 ? 1f : (float)i / (footstepCount - 1);
            float volume = Mathf.Lerp(footstepVolumeStart, footstepVolumeEnd, t);

            audioSource.PlayOneShot(footstepClip, volume);
            await UniTask.Delay(TimeSpan.FromSeconds(footstepInterval));
        }

        // 4. 椅子聲（單一音效，不另外播坐下）
        audioSource.PlayOneShot(chairClip);
        await UniTask.Delay(TimeSpan.FromSeconds(chairClip.length));

        // 注意：這裡「不」放開黑幕。黑幕由開場對話的黑底蓋上後自動放開（DialogueStageEffects）。
    }
}