using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Wugou
{

    /// <summary>
    /// �����ű���ģ��
    /// </summary>
    public class GameMapTemplate
    {
        public string name { get; set; }

        /// <summary>
        /// ��ѡ����
        /// </summary>
        public List<string> sceneTags { get; set; }

        /// <summary>
        /// �ű��༭�������
        /// </summary>
        public List<string> editorComponents { get; set; } = new List<string>();

        /// <summary>
        /// ��ͼ����
        /// </summary>
        public string content { get; set; }
    }

    /// <summary>
    /// ��װ�˳������ء��ű��༭�����صȹ���
    /// </summary>
    public static class GameMapManager //framework
    {
        public static AsyncOperation loadingSceneOperation => AssetBundleSceneManager.activeAsyncOperation;

        /// <summary>
        /// ��Դ�ļ����·��
        /// </summary>
        public static string resourceDir { get; private set; }

        /// <summary>
        /// �ű����·��
        /// </summary>
        public static string gameMapsDir { get; set; } = Path.Combine(Application.persistentDataPath, "maps");
        public static string gameMapDownloadDir => $"{gameMapsDir}/Download";


        private static string sceneConfigFilePath_ => Path.Combine(resourceDir, "scenes.json");

        /// <summary>
        /// �ű��ļ��ĺ�׺��
        /// </summary>
        public const string kGameMapFileSuffix = ".map";

        // ��ʱ����Ľű�����
        public const string tempScriptName = "~temp";       // ~��ͷ�Ľű�Ϊ���⹦��

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resourcePath"></param>
        public static void Initialize(string resourcePath)
        {
            resourceDir = resourcePath;
            //
            gameMapTemplatePath = Path.Combine(resourceDir, "map-templates");

            // read config
            assetbundleSceneCards = JsonConvert.DeserializeObject<List<AssetBundleSceneCard>>(File.ReadAllText(sceneConfigFilePath_));
        }

        /// <summary>
        /// �����½ű�
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public static GameMap CreateGameMap(AssetBundleScene scene)
        {
            var map = new GameMap();
            map.author = GamePlay.loginInfo.name;
            map.createTime = DateTime.Now.ToString();
            map.scene = scene;

            return map;
        }

        /// <summary>
        /// ��ȡ�ű�ȫ·��
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public static string GetFullPathOfGameMap(string relativePath)
        {
            if (relativePath.Contains(gameMapsDir))
            {
                return relativePath;
            }
            return $"{gameMapsDir}/{relativePath}{kGameMapFileSuffix}";
        }

        // ����UI�����ʾ
        public static List<AssetBundleSceneCard> assetbundleSceneCards { get;private set; }

        /// <summary>
        /// Ϊ�˲��𷽱㣬assetbundle��Ϣ�洢�������·��������ʱ��Ҫ����Ϊ���صľ���·��
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        private static string GetFullAssetBundlePath(string relativePath)
        {
            return $"{resourceDir}/{relativePath}";
        }

        /// <summary>
        /// �����ͼ
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="map"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public static bool SaveGameMap(string fileName, GameMap map, bool overwrite = true)
        {
            GamePlay.loadedGameMapFile = fileName;

            // Create Directory
            if (!Directory.Exists(gameMapsDir))
            {
                Directory.CreateDirectory(gameMapsDir);
            }

            string filePath = $"{gameMapsDir}/{fileName}{kGameMapFileSuffix}";
            if(File.Exists(filePath) && !overwrite)
            {
                Logger.Error($"{filePath} exists...");
                return false;
            }

            GameMapWriter writer = new GameMapWriter(filePath);
            writer.WriteVersion(map.version);
            writer.WriteName(map.name);
            writer.WriteGameWorld();
            writer.WriteGameMapDetail(map);
            writer.Save();
            return true;
        }

        /// <summary>
        /// ɾ���ű�
        /// </summary>
        /// <param name="form"></param>
        public static void RemoveGameMap(string scriptName)
        {
            // remove game map
            string fullPath = GetFullPathOfGameMap(scriptName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        /// <summary>
        /// �ű�ģ��·��
        /// </summary>
        public static string gameMapTemplatePath { get; set; } = $"{Application.persistentDataPath}/map-templates";

        /// <summary>
        /// ��ȡ���нű�ģ��
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllGameMapTemplates()
        {
            List<string> result = new List<string>() { "Empty" };
            DirectoryInfo TheFolder = new DirectoryInfo(gameMapTemplatePath);
            if (!TheFolder.Exists)
            {
                TheFolder.Create();
            }
            //�����ļ�
            foreach (FileInfo NextFile in TheFolder.GetFiles())
            {
                result.Add(Path.GetFileNameWithoutExtension(NextFile.FullName));
            }
            return result;
        }

        public const string kGameMapTemplateSuffix = ".gmt";

        /// <summary>
        /// ��ȡ������ģ���ļ�·��
        /// </summary>
        /// <param name="templateName"></param>
        /// <returns></returns>
        public static string GetFullPathOfTemplate(string templateName)
        {
            return $"{gameMapTemplatePath}/{templateName}{kGameMapTemplateSuffix}";
        }

        /// <summary>
        /// ��ȡ�ű�ģ��
        /// </summary>
        /// <param name="templateName"></param>
        /// <returns></returns>
        public static GameMapTemplate GetGameMapTemplate(string templateName)
        {
            string templateFileName = GetFullPathOfTemplate(templateName);
            return JsonConvert.DeserializeObject<GameMapTemplate>(File.ReadAllText(templateFileName));
        }

        /// <summary>
        /// ����ģ�崴��һ���½ű�
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public static GameMap CreateGameMapFromTemplate(GameMapTemplate template)
        {
            // copy template to scripts dir
            GameMap map = new GameMap();
            if(template != null)
            {
                map.Parse(template.content);
            }
            else
            {
                map.Parse("{}");    // �հ�ģ��
            }

            map.version = GamePlay.version;
            map.author = GamePlay.loginInfo.name;
            map.createTime = DateTime.Now.ToString();
            map.name = "new map";

            return map;
        }

        /// <summary>
        /// �ű������Ƿ��Ѵ���
        /// </summary>
        /// <param name="scriptName"></param>
        /// <returns></returns>
        public static bool ExistsGameMap(string scriptName)
        {
            string path = GetFullPathOfGameMap(scriptName);
            return File.Exists(path);
        }

        /// <summary>
        /// ���ļ��з����л��ű�
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static GameMap DeserializeGameMapFromFile(string fileName)
        {
            // �����ű�
            string content = File.ReadAllText(GetFullPathOfGameMap(fileName));
            GameMap map = new GameMap();
            map.Parse(content);
            return map;
        }

        /// <summary>
        /// ��ȡȫ���ű�
        /// </summary>
        /// <returns></returns>
        public static List<GameMap> GetAllGameMaps()
        {
            List<GameMap> maps = new List<GameMap>();
            foreach(var v in GetAllGameMapFiles())
            {
                var map = DeserializeGameMapFromFile(v);
                if(map != null)
                {
                    maps.Add(map);
                }
            }

            return maps;
        }

        /// <summary>
        /// ��ȡ�ű��ļ�����
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllGameMapFiles()
        {
            List<string> scripts = new List<string>();
            DirectoryInfo TheFolder = new DirectoryInfo(gameMapsDir);
            //�����ļ�
            if (TheFolder.Exists)
            {
                foreach (FileInfo NextFile in TheFolder.GetFiles())
                {
                    if (!NextFile.Name.StartsWith("~"))
                    {
                        scripts.Add(Path.GetFileNameWithoutExtension(NextFile.FullName));
                    }
                }
            }

            return scripts;
        }


        public static void UnloadAllAssetBundles()
        {
            // asset bundle
            AssetBundleAssetLoader.UnloadAllAssetBundle();
        }

        public static string GetSceneIcon(string sceneName)
        {
            foreach (var v in assetbundleSceneCards)
            {
                if (v.scene.sceneName == sceneName)
                {
                    return v.icon;
                }
            }

            return "";
        }

    }

}
