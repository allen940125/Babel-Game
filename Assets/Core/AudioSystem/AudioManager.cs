using Gamemanager;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using System.Collections.Generic;

namespace Game.Audio
{
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("必須資料")] [SerializeField] AudioSource ambientPlayer;
        [SerializeField] AudioSource musicPlayer;
        [SerializeField] AudioSource uIPlayer;

        // ----------------------------
        // SFX Pool
        // ----------------------------
        [Header("SFX 音頻池配置")] [SerializeField] AudioSource sfxPrefab;
        [SerializeField] int sfxPoolSize = 10;

        private readonly List<AudioSource> sfxPool = new List<AudioSource>();
        private readonly Queue<AudioSource> availableSfxPlayers = new Queue<AudioSource>();

        [Header("音效數值")]
        public AudioSettingsData CurrentAudioSettingsData => SaveManager.Instance.CurrentSettings.AudioData;

        [Header("各場景Audio配置")] [SerializeField]
        AudioConfig audioConfig;

        const float MIN_PITCH = 0.9f;
        const float MAX_PITCH = 1.1f;

        protected override void Awake()
        {
            base.Awake();
            InitializeAudioPool();
        }

        private void OnEnable()
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(
                GameManager.Instance.MainGameEvent.OnSceneLoadedEvent,
                OnSceneLoadedEvent
            );
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
            {
                GameManager.Instance.MainGameEvent.Unsubscribe<SceneLoadedEvent>(OnSceneLoadedEvent);
            }
        }

        // ----------------------------
        // 場景切換
        // ----------------------------
        private void OnSceneLoadedEvent(SceneLoadedEvent cmd)
        {
            LoadSceneAudioConfig();
        }

        // ----------------------------
        // 音量封裝（從舊版合併）
        // ----------------------------
        public float MasterVolume
        {
            get => CurrentAudioSettingsData.MasterVolume;
            set
            {
                CurrentAudioSettingsData.MasterVolume = value;
                UpdateAllVolumes();
                SaveManager.Instance.SaveSettings();
            }
        }

        public float AmbientVolume
        {
            get => CurrentAudioSettingsData.AmbientVolume;
            set
            {
                CurrentAudioSettingsData.AmbientVolume = value;
                UpdateAllVolumes();
                SaveManager.Instance.SaveSettings();
            }
        }

        public float MusicVolume
        {
            get => CurrentAudioSettingsData.MusicVolume;
            set
            {
                CurrentAudioSettingsData.MusicVolume = value;
                UpdateAllVolumes();
                SaveManager.Instance.SaveSettings();
            }
        }

        public float SFXVolume
        {
            get => CurrentAudioSettingsData.SFXVolume;
            set
            {
                CurrentAudioSettingsData.SFXVolume = value;
                SaveManager.Instance.SaveSettings();
            }
        }

        public float UIVolume
        {
            get => CurrentAudioSettingsData.UIVolume;
            set
            {
                CurrentAudioSettingsData.UIVolume = value;
                SaveManager.Instance.SaveSettings();
            }
        }

        private void UpdateAllVolumes()
        {
            ambientPlayer.volume = AmbientVolume * MasterVolume;
            musicPlayer.volume = MusicVolume * MasterVolume;
            // SFX & UI 在播放時才套用
        }

        // ----------------------------
        // SFX Pool 初始化
        // ----------------------------
        private void InitializeAudioPool()
        {
            Transform sfxParent = new GameObject("SFX_Pool_Parent").transform;
            sfxParent.SetParent(this.transform);

            for (int i = 0; i < sfxPoolSize; i++)
            {
                AudioSource newSfxPlayer = Instantiate(sfxPrefab, sfxParent);
                newSfxPlayer.playOnAwake = false;
                newSfxPlayer.gameObject.SetActive(false);
                newSfxPlayer.gameObject.name = $"SFX_Player_{i}";
                sfxPool.Add(newSfxPlayer);
                availableSfxPlayers.Enqueue(newSfxPlayer);
            }
        }

        private AudioSource GetAvailableSFXPlayer()
        {
            if (availableSfxPlayers.Count > 0)
                return availableSfxPlayers.Dequeue();

            Debug.LogWarning("SFX Pool exhausted. Ignoring sound request.");
            return null;
        }

        // ----------------------------
        // SFX 播放（優化版）
        // ----------------------------
        private void PlaySFXInternal(AudioData audioData, bool randomPitch)
        {
            AudioSource player = GetAvailableSFXPlayer();
            if (player == null) return;

            player.gameObject.SetActive(true);

            player.pitch = randomPitch ? Random.Range(MIN_PITCH, MAX_PITCH) : 1f;
            player.clip = audioData.audioClip;
            player.volume = SFXVolume * MasterVolume;

            player.Play();

            StartCoroutine(ReturnToPoolAfterDelay(player, audioData.audioClip.length));
        }

        private System.Collections.IEnumerator ReturnToPoolAfterDelay(AudioSource player, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (!availableSfxPlayers.Contains(player))
            {
                player.Stop();
                player.clip = null;
                player.gameObject.SetActive(false);
                availableSfxPlayers.Enqueue(player);
            }
        }

        // EXPOSED API
        public void PlaySFX(AudioData data) => PlaySFXInternal(data, false);
        public void PlayRandomSFX(AudioData data) => PlaySFXInternal(data, true);

        public void PlayRandomSFX(AudioData[] datas)
        {
            if (datas == null || datas.Length == 0) return;
            PlaySFXInternal(datas[Random.Range(0, datas.Length)], true);
        }

        // ----------------------------
        // UI 音效（沿用單一 AudioSource）
        // ----------------------------
        private void PlayUISoundInternal(AudioData data, bool randomPitch)
        {
            if (uIPlayer == null) return;

            uIPlayer.pitch = randomPitch ? Random.Range(MIN_PITCH, MAX_PITCH) : 1f;
            uIPlayer.PlayOneShot(data.audioClip, UIVolume * MasterVolume);
        }

        public void PlayUISound(AudioData data) => PlayUISoundInternal(data, false);
        public void PlayRandomUISound(AudioData data) => PlayUISoundInternal(data, true);

        public void PlayRandomUISound(AudioData[] datas)
        {
            if (datas == null || datas.Length == 0) return;
            PlayUISoundInternal(datas[Random.Range(0, datas.Length)], true);
        }

        // ----------------------------
        // Ambient / Music（保持不變）
        // ----------------------------
        public void PlayAmbient(AudioData data)
        {
            ambientPlayer.clip = data.audioClip;
            ambientPlayer.volume = AmbientVolume * MasterVolume;
            ambientPlayer.Play();
        }

        public void PlayMusic(AudioData data)
        {
            musicPlayer.clip = data.audioClip;
            musicPlayer.volume = MusicVolume * MasterVolume;
            musicPlayer.Play();
        }

        // ----------------------------
        // 場景音效配置（你原本已有）
        // ----------------------------
        // 修正後的 LoadSceneAudioConfig 應替換您 AudioManager 中的版本

        private void LoadSceneAudioConfig()
        {
            if (audioConfig == null) return;

            string currentSceneName = SceneManager.GetActiveScene().name;
            AudioConfig.SceneAudio sceneAudio = audioConfig.GetAudioDataForScene(currentSceneName);

            if (sceneAudio != null)
            {
                // 1. 播放 BGM (只取列表中的第一個)
                if (sceneAudio.startBGMData != null && sceneAudio.startBGMData.Count > 0)
                {
                    // 如果 BGM 列表中有多個，我們只播放第一個
                    PlayMusic(sceneAudio.startBGMData[0]);
                }

                // 2. 播放 SFX / Ambient (只取列表中的第一個作為 Ambient 或 SFX)
                if (sceneAudio.startSFXData != null && sceneAudio.startSFXData.Count > 0)
                {
                    // 這裡假設場景開始時的 SFX 列表的第一個是用於 Ambient
                    // 如果您希望它作為環境音循環播放，應使用 PlayAmbient
                    PlayAmbient(sceneAudio.startSFXData[0]);
                }
            }
            else
            {
                Debug.LogWarning($"No AudioConfig found for scene: {currentSceneName}");
            }
        }
    }
}

[System.Serializable]
public class AudioData
{
    public AudioClip audioClip;
}