using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Wugou
{


    /// <summary>
    /// ��װ�˳������ء��ű��༭�����صȹ���
    /// </summary>
    public static class GameMapManager //framework
    {
        /// <summary>
        /// �ű����·��
        /// </summary>
        public static string gameMapsDir { get; set; } = Path.Combine(Application.persistentDataPath, "maps");
        public static string gameMapDownloadDir => $"{gameMapsDir}/Download";

        /// <summary>
        /// ����ͼ·��
        /// </summary>
        public static string gameMapThumbnailDir=> $"{gameMapsDir}/thumbnails";


        public static string sceneConfigFilePath =>$"{GamePlay.settings.configPath}/scenes.json";

        /// <summary>
        /// �ű��ļ��ĺ�׺��
        /// </summary>
        public const string kGameMapFileSuffix = ".map";

        /// <summary>
        /// �����½ű�
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public static GameMap CreateGameMap(AssetBundleScene scene)
        {
            var map = new GameMap();
            map.version = GameMap.kLatestVersion;
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
        private static List<AssetBundleSceneCard> assetbundleSceneCards_ = null;
        public static List<AssetBundleSceneCard> assetbundleSceneCards {
            get 
            {
                if (assetbundleSceneCards_ == null)
                {
                    // read config
                    assetbundleSceneCards_ = JsonConvert.DeserializeObject<List<AssetBundleSceneCard>>(File.ReadAllText(sceneConfigFilePath), new AssetBundleDescConvert());
                }

                return assetbundleSceneCards_;
            }

            private set
            {
                assetbundleSceneCards_ = value;
            }
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

            GameMapWriter writer = new GameMapWriter();
            writer.WriteVersion(map.version);
            writer.WriteName(map.name);
            writer.WriteGameWorld();
            writer.WriteGameMapDetail(map);
            writer.Save(filePath);

            // ��������ͼ
            if(!Directory.Exists(gameMapThumbnailDir))
            {
                Directory.CreateDirectory(gameMapThumbnailDir);
            }
            Utils.CreateSceneThumbnail($"{gameMapThumbnailDir}/{fileName}.png", Screen.width, Screen.height, Camera.main);

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

            // remote icon
            string iconPath = $"{gameMapThumbnailDir}/{scriptName}.png";
            if(File.Exists(iconPath))
            {
                File.Delete(iconPath);
            }
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
        internal static GameMap GetGameMap(string fileName)
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
                var map = GetGameMap(v);
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

        /// <summary>
        /// ��ȡ����icon��ʹ��assetbundlescene��icon��Ϊ��ͼicon
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        public static string GetAssetbundleSceneIcon(string sceneName)
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

        /// <summary>
        /// ��ȡ��ͼ������ͼ
        /// </summary>
        /// <param name="mapName"></param>
        /// <returns></returns>
        public static Texture2D GetMapThumbnail(string mapName)
        {
            return Utils.LoadTextureFromFile($"{gameMapThumbnailDir}/{mapName}.png");
        }

        #region ģ��

        /// <summary>
        /// �ű�ģ��·��
        /// </summary>
        public static string gameMapTemplatePath { get; private set; } = $"{GamePlay.settings.resourcePath}/map-templates";

        /// <summary>
        /// ��ȡ���нű�ģ��
        /// </summary>
        /// <returns></returns>
        public static List<GameMapTemplateDesc> GetAllGameMapTemplates()
        {
            JObject jo = JObject.Parse(File.ReadAllText($"{GamePlay.settings.configPath}/map-templates.json"));
            var res = jo["templates"].ToObject<List<GameMapTemplateDesc>>();

            return res;
        }

        /// <summary>
        /// ��ȡ������ģ���ļ�·��
        /// </summary>
        /// <param name="templateName"></param>
        /// <returns></returns>
        public static string GetFullPathOfTemplate(string templateName)
        {
            return $"{gameMapTemplatePath}/{templateName}";
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
            if (template != null)
            {
                map.Parse(template.content);
            }
            else
            {
                map.Parse("{}");    // �հ�ģ��
                map.weather.time = 0.4f;
            }

            map.version = GameMap.kLatestVersion;
            map.createTime = DateTime.Now.ToString();
            map.name = "new map";

            return map;
        }

        #endregion

    }

    /// <summary>
    /// ģ������
    /// </summary>
    public class GameMapTemplateDesc
    {
        public string name;
        public string path;
        public string icon;
    }

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

}
