using System.Collections;
using Game.Audio;
using UnityEngine;

/// <summary>
/// 統一的「敵人攻擊音效」：不管哪種攻擊，都呼叫 AttackSfx.Play()，就會連播 N 次同一個音效。
///
/// 設定：
///   1. 場景裡建一個空物件（例如 "AttackSfx"），掛上這支腳本。
///   2. Attack Sfx → Audio Clip 拖入「開一槍」的音檔。
///   3. Repeat Count 設 3、Interval 設每槍間隔秒數（約 0.1~0.2，依音檔長度調整）。
///   4. 攻擊腳本裡在攻擊發生處呼叫：  AttackSfx.Play();
///
/// 之後想改音效、次數、間隔，只要改這一個物件，不用動任何攻擊腳本。
/// </summary>
public class AttackSfx : MonoBehaviour
{
    public static AttackSfx Instance { get; private set; }

    [SerializeField] private AudioData attackSfx;
    [SerializeField, Min(1)] private int repeatCount = 3;
    [Tooltip("每一槍之間的間隔（秒）。")]
    [SerializeField] private float interval = 0.12f;
    [Tooltip("勾選 = 每槍音調隨機微調，聽起來比較不呆板。")]
    [SerializeField] private bool randomPitch = true;
    [Tooltip("上一組還沒播完時，是否忽略新的呼叫（避免多個攻擊同時發出時音效疊成一團、用光音效池）。")]
    [SerializeField] private bool ignoreWhilePlaying = true;

    private bool playing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 播放一組攻擊音效。
    /// 不帶參數 = 使用 AttackSfx 物件上設定的預設音效、次數、間隔。
    /// 帶參數 = 只這一次改用指定的音效 / 次數 / 間隔（count、interval 傳負數代表用預設值）。
    /// </summary>
    public static void Play(AudioData overrideSfx = null, int count = -1, float interval = -1f)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[AttackSfx] 場景裡沒有 AttackSfx 物件。");
            return;
        }
        Instance.PlayBurst(overrideSfx, count, interval);
    }

    private void PlayBurst(AudioData overrideSfx, int count, float gap)
    {
        AudioData sfx = (overrideSfx != null && overrideSfx.audioClip != null) ? overrideSfx : attackSfx;
        int times = count > 0 ? count : repeatCount;
        float wait = gap >= 0f ? gap : interval;

        if (sfx == null || sfx.audioClip == null)
        {
            Debug.LogWarning("[AttackSfx] 沒有設定音檔（Attack Sfx → Audio Clip 是空的）。", this);
            return;
        }
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[AttackSfx] 場景裡找不到 AudioManager。", this);
            return;
        }
        if (ignoreWhilePlaying && playing) return;
        Debug.Log($"[AttackSfx] 播放 {sfx.audioClip.name} × {times}（SFX 音量 {AudioManager.Instance.SFXVolume}，主音量 {AudioManager.Instance.MasterVolume}）");
        StartCoroutine(BurstRoutine(sfx, times, wait));
    }

    private IEnumerator BurstRoutine(AudioData sfx, int times, float wait)
    {
        playing = true;
        for (int i = 0; i < times; i++)
        {
            if (AudioManager.Instance != null)
            {
                if (randomPitch) AudioManager.Instance.PlayRandomSFX(sfx);
                else AudioManager.Instance.PlaySFX(sfx);
            }
            if (i < times - 1) yield return new WaitForSeconds(wait);
        }
        playing = false;
    }

    private void OnDisable()
    {
        playing = false;
    }
}