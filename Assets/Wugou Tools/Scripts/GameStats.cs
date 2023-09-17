using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

namespace Wugou
{

    /// <summary>
    /// ��Ϸ��Ϣ����
    /// </summary>
    public class GameStats { }

    /// <summary>
    /// ��Ϸ��Ϣ
    /// </summary>
    public class GameStats<T> : GameStats
    {
        public string name;
        public string gamemap;
        public float duration;
        public Dictionary<string, T> playerStats = new Dictionary<string, T>();
    }

    /// <summary>
    /// ��Ϸ��¼����
    /// </summary>
    public class GameStatsManager
    {
        /// <summary>
        /// ��Ϸ��¼Ŀ¼
        /// </summary>
        public string gameStatsFilePath { get; private set; }

        public GameStatsManager(string path)
        {
            gameStatsFilePath = path;
        }

        public List<T> GetAllGameStats<T>()
        {
            if (File.Exists(gameStatsFilePath))
            {
                return JsonConvert.DeserializeObject<List<T>>(File.ReadAllText(gameStatsFilePath));
            }

            return new List<T>();
        }

        public void AddGameStats<T>(T gameStats)
        {
            var statsList = GetAllGameStats<T>();
            statsList.Add(gameStats);

            File.WriteAllText(gameStatsFilePath, JsonConvert.SerializeObject(statsList).ToString());
        }
    }
}
