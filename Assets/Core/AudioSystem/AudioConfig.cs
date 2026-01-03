using Game.SceneManagement;
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
            public SceneType sceneType; // ｳｺｦWｺﾙ
            public List<AudioData> startBGMData;
            public List<AudioData> startSFXData; // ｸﾓｳｺｪｺｪ・lｿ鬢Jｰﾊｧ@
        }

        public List<SceneAudio> sceneAudios; // ｩﾒｦｳｳｺｪｺｿ鬢Jｰtｸm

        /// <summary>
        /// ｮﾚｾﾚｷ戓eｳｺｦWｺﾙﾀ彧・ｳｪｺｿ鬢Jｰtｸm
        /// </summary>
        /// <param name="sceneName">ｳｺｦWｺﾙ</param>
        /// <returns>ｸﾓｳｺｪｺｿ鬢Jｰﾊｧ@ｦCｪ・/returns>
        public SceneAudio GetAudioDataForScene(string sceneName)
        {
            foreach (var sceneAudio in sceneAudios)
            {
                if (sceneAudio.sceneType.ToString() == sceneName)
                {
                    return sceneAudio;
                }
            }
            return null; // ｦpｪG･ｼｧ茯・・ｳｪｺｳｺｰtｸm｡Aｪ^ｪﾅ
        }
    }

}
