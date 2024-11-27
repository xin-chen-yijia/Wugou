using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    public interface IGameSoundsHandler
    {
        public void SetAmbienceVolume(float value);
        public float GetAmbienceVolume();

        public void SetMusicVolume(float value);
        public float GetMusicVolume();

        public void SetWeatherVolume(float value);
        public float GetWeatherVolume();
    }

    class DefaultGameSoundsManager : IGameSoundsHandler
    {
        public float GetAmbienceVolume()
        {
            return 0.0f;
        }

        public float GetMusicVolume()
        {
            return 0.0f;
        }

        public float GetWeatherVolume()
        {
            return 0.0f;
        }

        public void SetAmbienceVolume(float value)
        {
        }

        public void SetMusicVolume(float value)
        {
        }

        public void SetWeatherVolume(float value)
        {
        }
    }

    /// <summary>
    /// 场景中声音的管理，如音乐、音量等
    /// </summary>
    public static class GameSoundsManager
    {
        public static IGameSoundsHandler activeHandler { get; set; } = new DefaultGameSoundsManager();
    }
}
