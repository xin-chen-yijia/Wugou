using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Wugou
{

    /// <summary>
    /// 创建脚本的模板
    /// </summary>
    public class GameMapTemplate
    {
        public string name { get; set; }

        /// <summary>
        /// 可选场景
        /// </summary>
        public List<string> sceneTags { get; set; }

        /// <summary>
        /// 脚本编辑器的组件
        /// </summary>
        public List<string> editorComponents { get; set; } = new List<string>();

        /// <summary>
        /// 地图内容
        /// </summary>
        public string content { get; set; }
    }

    /// <summary>
    /// 封装了场景加载、脚本编辑、加载等功能
    /// </summary>
    public static class GameMapManager //framework
    {
        public static AsyncOperation loadingSceneOperation => AssetBundleSceneManager.activeAsyncOperation;

        /// <summary>
        /// 资源文件存放路径
        /// </summary>
        public static string resourceDir { get; private set; }

        /// <summary>
        /// 脚本存放路径
        /// </summary>
        public static string gameMapsDir { get; set; } = Path.Combine(Application.persistentDataPath, "maps");
        public static string gameMapDownloadDir => $"{gameMapsDir}/Download";


        private static string sceneConfigFilePath_ => Path.Combine(resourceDir, "scenes.json");

        /// <summary>
        /// 脚本文件的后缀名
        /// </summary>
        public const string kGameMapFileSuffix = ".map";

        // 临时保存的脚本名称
        public const string tempScriptName = "~temp";       // ~开头的脚本为特殊功用

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
        /// 创建新脚本
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
        /// 获取脚本全路径
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

        // 场景UI相关显示
        public static List<AssetBundleSceneCard> assetbundleSceneCards { get;private set; }

        /// <summary>
        /// 为了部署方便，assetbundle信息存储的是相对路径，加载时需要修正为本地的绝对路径
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        private static string GetFullAssetBundlePath(string relativePath)
        {
            return $"{resourceDir}/{relativePath}";
        }

        /// <summary>
        /// 保存地图
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
        /// 删除脚本
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
        /// 脚本模板路径
        /// </summary>
        public static string gameMapTemplatePath { get; set; } = $"{Application.persistentDataPath}/map-templates";

        /// <summary>
        /// 获取所有脚本模板
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
            //遍历文件
            foreach (FileInfo NextFile in TheFolder.GetFiles())
            {
                result.Add(Path.GetFileNameWithoutExtension(NextFile.FullName));
            }
            return result;
        }

        public const string kGameMapTemplateSuffix = ".gmt";

        /// <summary>
        /// 获取完整的模板文件路径
        /// </summary>
        /// <param name="templateName"></param>
        /// <returns></returns>
        public static string GetFullPathOfTemplate(string templateName)
        {
            return $"{gameMapTemplatePath}/{templateName}{kGameMapTemplateSuffix}";
        }

        /// <summary>
        /// 获取脚本模板
        /// </summary>
        /// <param name="templateName"></param>
        /// <returns></returns>
        public static GameMapTemplate GetGameMapTemplate(string templateName)
        {
            string templateFileName = GetFullPathOfTemplate(templateName);
            return JsonConvert.DeserializeObject<GameMapTemplate>(File.ReadAllText(templateFileName));
        }

        /// <summary>
        /// 基于模板创建一个新脚本
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
                map.Parse("{}");    // 空白模板
            }

            map.version = GamePlay.version;
            map.author = GamePlay.loginInfo.name;
            map.createTime = DateTime.Now.ToString();
            map.name = "new map";

            return map;
        }

        /// <summary>
        /// 脚本名字是否已存在
        /// </summary>
        /// <param name="scriptName"></param>
        /// <returns></returns>
        public static bool ExistsGameMap(string scriptName)
        {
            string path = GetFullPathOfGameMap(scriptName);
            return File.Exists(path);
        }

        /// <summary>
        /// 从文件中反序列化脚本
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static GameMap DeserializeGameMapFromFile(string fileName)
        {
            // 解析脚本
            string content = File.ReadAllText(GetFullPathOfGameMap(fileName));
            GameMap map = new GameMap();
            map.Parse(content);
            return map;
        }

        /// <summary>
        /// 获取全部脚本
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
        /// 获取脚本文件集合
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllGameMapFiles()
        {
            List<string> scripts = new List<string>();
            DirectoryInfo TheFolder = new DirectoryInfo(gameMapsDir);
            //遍历文件
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
