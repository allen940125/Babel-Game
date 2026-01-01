using Game.SceneManagement; // 假設 SceneType 在這裡定義
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Audio
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Audio/AudioConfig")]
    public class AudioConfig : ScriptableObject
    {
        [Serializable]
        public class SceneAudio
        {
            public SceneType sceneType; // 場景類型
            public List<AudioData> startBGMData; // 場景開始時播放的 BGM
            public List<AudioData> startSFXData; // 場景開始時播放的 SFX (環境音或單次音效)
        }

        public List<SceneAudio> sceneAudios; // 所有場景的音頻配置

        /// <summary>
        /// 根據場景名稱獲取對應的音頻配置
        /// </summary>
        /// <param name="sceneName">當前場景名稱 (通常是 SceneManager.GetActiveScene().name)</param>
        /// <returns>場景音頻數據，若無則回傳 null</returns>
        public SceneAudio GetAudioDataForScene(string sceneName)
        {
            // 由於 sceneType 是 SceneType enum，我們使用它的 ToString() 與 sceneName 比較
            foreach (var sceneAudio in sceneAudios)
            {
                if (sceneAudio.sceneType.ToString() == sceneName)
                {
                    return sceneAudio;
                }
            }
            return null;
        }
    }
}