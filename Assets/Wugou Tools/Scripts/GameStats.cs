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
    /// ��Ϸ��Ϣ����
    /// </summary>
    public class GameStats {
        public string name;
        public string gamemap;
        //public string startTime;
        public float duration;
    }

    /// <summary>
    /// ��Ϸ��¼����
    /// </summary>
    public class GameStatsManager
    {
        /// <summary>
        /// ����һЩ�򵥵���Ϣ������ÿ�ζ�ȡ���м�¼��̫��ʱ��
        /// </summary>
        public const string tempFileName = "~tmp";
        public string fullTempFilePath => Path.Combine(gameStatsDir, tempFileName);

        /// <summary>
        /// �ļ���׺
        /// </summary>
        public const string kSuffix = ".gt";

        /// <summary>
        /// ��Ϸ��¼Ŀ¼
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
            //�����ļ�
            foreach (FileInfo NextFile in TheFolder.GetFiles($"*{kSuffix}"))
            {
                res.Add(JsonConvert.DeserializeObject<T>(File.ReadAllText(NextFile.FullName)));   
            }

            return res;
        }

        /// <summary>
        /// ��ȡ��Ϸ��¼
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
            //�����ļ�
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
