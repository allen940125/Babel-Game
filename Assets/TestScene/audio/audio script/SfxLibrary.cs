using System;
using System.Collections.Generic;
using Game.Audio;
using UnityEngine;

/// <summary>
/// 「名字 → 音效」對照表，讓 Ink 可以用名字播放單次音效：
///     哎呀呀，多謝撥冗。這次想拜託你的是件麻煩事。 # sfx: phone
///
/// 設定：場景裡建一個空物件掛上這支腳本，在 Entries 加一筆：
///     Key = phone（Ink 裡寫的名字）、Sfx = 拖入音檔、Random Pitch = 要不要隨機微調音調。
/// 用的是隊友的 AudioManager.PlaySFX，所以音量會跟設定視窗的「音效音量 × 主音量」。
/// </summary>
public class SfxLibrary : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public string key;
        public AudioData sfx;
        public bool randomPitch = false;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private static readonly Dictionary<string, Entry> registry = new Dictionary<string, Entry>();

    private void Awake()
    {
        foreach (var e in entries)
            if (e != null && !string.IsNullOrWhiteSpace(e.key))
                registry[e.key.Trim()] = e;
    }

    private void OnDestroy()
    {
        foreach (var e in entries)
            if (e != null && !string.IsNullOrWhiteSpace(e.key)
                && registry.TryGetValue(e.key.Trim(), out var cur) && cur == e)
                registry.Remove(e.key.Trim());
    }

    /// <summary>依名字播放一次（Ink 的 # sfx: key 會呼叫）。</summary>
    public static void PlayKey(string key)
    {
        if (!registry.TryGetValue(key, out var e) || e.sfx == null || e.sfx.audioClip == null)
        {
            Debug.LogWarning($"[SfxLibrary] 找不到 key = \"{key}\" 的音效（或音檔是空的）。");
            return;
        }
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[SfxLibrary] 找不到 AudioManager。");
            return;
        }
        if (e.randomPitch) AudioManager.Instance.PlayRandomSFX(e.sfx);
        else AudioManager.Instance.PlaySFX(e.sfx);
    }
}
