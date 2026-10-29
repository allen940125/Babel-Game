using System.Collections;
using System.Collections.Generic;
using Game.Audio;
using UnityEngine;

/// <summary>
/// 場景內的循環音效（例如海浪）。有自己的 AudioSource，支援淡入 / 淡出，
/// 音量會乘上設定視窗的「環境音量 × 主音量」，所以玩家調音量也有效。
///
/// 使用：
///   1. 空物件掛上這支腳本，Clip 拖入海浪音檔。
///   2. 程式呼叫 loop.Play()（淡入）與 loop.Stop()（淡出）。
///   3. 也可勾 Play On Start 讓它一進場景就播。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SceneLoopSfx : MonoBehaviour
{
    [Tooltip("給 Ink 用的名字（例如 wave）。Ink 寫 # loop: wave 開始、# loopstop: wave 停止。不用 Ink 可留空。")]
    [SerializeField] private string key;
    [SerializeField] private AudioClip clip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.4f;
    [SerializeField] private float fadeInSeconds = 2f;
    [SerializeField] private float fadeOutSeconds = 1.5f;
    [SerializeField] private bool playOnStart = false;

    private AudioSource source;
    private Coroutine fadeRoutine;
    private bool wantPlaying;

    private static readonly Dictionary<string, SceneLoopSfx> registry = new Dictionary<string, SceneLoopSfx>();

    /// <summary>依 key 開始播放（Ink 的 # loop: key 會呼叫）。</summary>
    public static void PlayKey(string k)
    {
        if (registry.TryGetValue(k, out var l) && l != null) l.Play();
        else Debug.LogWarning($"[SceneLoopSfx] 找不到 key = \"{k}\" 的循環音效物件。");
    }

    /// <summary>依 key 淡出停止（Ink 的 # loopstop: key 會呼叫）。</summary>
    public static void StopKey(string k)
    {
        if (registry.TryGetValue(k, out var l) && l != null) l.Stop();
    }

    /// <summary>淡出所有已註冊的循環音效。</summary>
    public static void StopAllLoops()
    {
        foreach (var l in registry.Values)
            if (l != null) l.Stop();
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(key) && registry.TryGetValue(key, out var l) && l == this)
            registry.Remove(key);
    }

    private void Awake()
    {
        if (!string.IsNullOrEmpty(key)) registry[key] = this;
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
    }

    private void Start()
    {
        if (playOnStart) Play();
    }

    /// <summary>淡入並循環播放。</summary>
    public void Play()
    {
        if (clip == null)
        {
            Debug.LogWarning("[SceneLoopSfx] 沒有指定 Clip。", this);
            return;
        }
        wantPlaying = true;
        if (source.clip != clip) source.clip = clip;
        if (!source.isPlaying) source.Play();
        StartFade(TargetVolume(), fadeInSeconds, stopAtEnd: false);
    }

    /// <summary>淡出並停止。</summary>
    public void Stop()
    {
        wantPlaying = false;
        if (!source.isPlaying) return;
        StartFade(0f, fadeOutSeconds, stopAtEnd: true);
    }

    private void Update()
    {
        // 玩家在播放中調音量，也要跟著變（只在穩定播放時同步）
        if (wantPlaying && fadeRoutine == null && source.isPlaying)
            source.volume = TargetVolume();
    }

    private float TargetVolume()
    {
        float setting = 1f;
        try
        {
            var am = AudioManager.Instance;
            if (am != null) setting = am.AmbientVolume * am.MasterVolume;
        }
        catch { /* SaveManager 還沒就緒時，先用 1 */ }
        return volume * setting;
    }

    private void StartFade(float to, float seconds, bool stopAtEnd)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(to, seconds, stopAtEnd));
    }

    private IEnumerator FadeRoutine(float to, float seconds, bool stopAtEnd)
    {
        float from = source.volume;
        float t = 0f;
        while (seconds > 0f && t < seconds)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        source.volume = to;
        if (stopAtEnd) source.Stop();
        fadeRoutine = null;
    }
}