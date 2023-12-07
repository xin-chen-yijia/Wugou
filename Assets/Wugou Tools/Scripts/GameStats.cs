using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System;

namespace Wugou
{

    /// <summary>
    /// 游戏信息基类
    /// </summary>
    public class GameStats {
        public string name;
        public string gamemap;
        //public string startTime;
        public float duration;
    }

    /// <summary>
    /// 游戏记录管理
    /// </summary>
    public class GameStatsManager
    {
        /// <summary>
        /// 缓存一些简单的信息，避免每次读取所有记录，太耗时了
        /// </summary>
        public const string tempFileName = "~tmp";
        public string fullTempFilePath => Path.Combine(gameStatsDir, tempFileName);

        /// <summary>
        /// 文件后缀
        /// </summary>
        public const string kSuffix = ".gt";

        /// <summary>
        /// 游戏记录目录
        /// </summary>
        public string gameStatsDir { get; private set; }

        public GameStatsManager(string path)
        {
            gameStatsDir = path;
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public List<T> GetAllGameStats<T>() where T : GameStats
        {
            List<T> res = new List<T>();
            DirectoryInfo TheFolder = new DirectoryInfo(gameStatsDir);
            //遍历文件
            foreach (FileInfo NextFile in TheFolder.GetFiles($"*{kSuffix}"))
            {
                res.Add(JsonConvert.DeserializeObject<T>(File.ReadAllText(NextFile.FullName)));   
            }

            return res;
        }

        /// <summary>
        /// 获取游戏记录
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public T GetGameStats<T>(string fileName)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText($"{gameStatsDir}/{fileName}"));
        }

        public void AddGameStats<T>(T gameStats) where T : GameStats
        {
            File.WriteAllText($"{gameStatsDir}/{gameStats.name}{kSuffix}", JsonConvert.SerializeObject(gameStats).ToString());
        }

        public List<GameStats> GetGameStatBriefs()
        {
            // 
            List<GameStats> briefs = new List<GameStats>();
            if (File.Exists(fullTempFilePath))
            {
                briefs = JsonConvert.DeserializeObject<List<GameStats>>(File.ReadAllText(fullTempFilePath));
            }

            return briefs;
        }

        public void UpdateGameStatBriefs()
        {
            var briefs = GetGameStatBriefs();
            var tmpList = new List<GameStats>();
            DirectoryInfo TheFolder = new DirectoryInfo(gameStatsDir);
            //遍历文件
            foreach (FileInfo NextFile in TheFolder.GetFiles($"*{kSuffix}"))
            {
                var t = NextFile.Name.Replace(kSuffix, "");
                var bf = briefs.Find((d) => { return d.name == t; });
                if (bf == null)
                {
                    var stat = GetGameStats<GameStats>(NextFile.Name);
                    tmpList.Add(stat);
                }
                else
                {
                    tmpList.Add(bf);
                }
            }

            File.WriteAllText(fullTempFilePath, JsonConvert.SerializeObject(tmpList).ToString());
        }
    }
}
