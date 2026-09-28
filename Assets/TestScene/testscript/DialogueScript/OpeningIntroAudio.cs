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

    public async UniTask PlayIntroSequenceAsync()
    {
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
    }
}