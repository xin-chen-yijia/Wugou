using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using Wugou.Editor;

namespace Wugou
{
    /// <summary>
    /// 游戏的一些管理配置
    /// </summary>
    public static class Gameplay
    {
        private static GamePlaySettings settings_ = null;
        public static GamePlaySettings settings
        {
            get
            {
                if (settings_ == null)
                {
#if UNITY_EDITOR
                    //settings_ = Resources.Load<GamePlaySettings>("GamePlaySettings");
                    settings_ = Resources.Load<GamePlaySettings>("GamePlaySettings dev");
#else
#if WUGOU_DEVELOPMENT
                        settings_ = Resources.Load<GamePlaySettings>("GamePlaySettings dev");
#else
                        settings_ = Resources.Load<GamePlaySettings>("GamePlaySettings");
#endif
#endif
                    // 深拷贝
                    settings_ = Utils.DeepClone(settings_);

                    // 使用绝对路径
                    //if (settings_.configPath.StartsWith("./"))
                    //{
                    //    settings.configPath = System.IO.Path.GetFullPath(settings_.configPath);
                    //}
                    //if (settings_.resourcePath.StartsWith("./"))
                    //{
                    //    settings.resourcePath = System.IO.Path.GetFullPath(settings_.resourcePath);
                    //}

                }

                return settings_;
            }
        }

        public static string configPath => Path.GetFullPath("./Config");
        public static string resourcePath => Path.GetFullPath("./Resources");

        /// <summary>
        /// 游戏模式，区别于编辑模式
        /// </summary>
        public static bool isGaming { get; set; } = false;

        /// <summary>
        /// 当前使用的天气系统
        /// </summary>
        public static IWeatherSystem weatherSystem { get; set; }

        public const string kGameMapPackageSuffix = ".em";
        public const string kGameMapFileSuffix = ".map";

        private static FileAssetsManager<GameMapPackage> _gameMapManager = null;
        /// <summary>
        /// 地图管理
        /// </summary>
        public static FileAssetsManager<GameMapPackage> gameMapManager {
            get {
                if (_gameMapManager == null)
                {
                    _gameMapManager = new FileAssetsManager<GameMapPackage>($"{Gameplay.gameMapsPath}", kGameMapPackageSuffix, new GameMapPackageParser());
                }
                return _gameMapManager;
            }
        }

        private static FolderAssetsManager<GameMapProj> _gameMapProjManager = null;
        /// <summary>
        /// 地图编辑工程管理
        /// </summary>
        public static FolderAssetsManager<GameMapProj> gameMapProjManager {
            get
            {
                if(_gameMapProjManager == null)
                {
                    _gameMapProjManager = new FolderAssetsManager<GameMapProj>($"{Gameplay.gameMapProjsPath}", new GameMapProjAssetParser());
                }

                return _gameMapProjManager;
            }
        }

        private static FolderAssetsManager<UnityScene> _unitySceneManager = null;
        /// <summary>
        /// 用于管理ab包中的场景
        /// </summary>
        public static FolderAssetsManager<UnityScene> unitySceneManager
        {
            get
            {
                if(_unitySceneManager == null)
                {
                    _unitySceneManager = new FolderAssetsManager<UnityScene>($"{Gameplay.scenesPath}", new UnitySceneParser());
                }

                return _unitySceneManager;
            }
        }

        public static FolderAssetsManager<UnityScene> builtInUnitySceneManager = new FolderAssetsManager<UnityScene>("./Resources/scenes", new UnitySceneParser());

        /// <summary>
        /// 加载的脚本文件名称
        /// </summary>
        public static string loadedGameMapFile { get; set; } = string.Empty;

        private static string _workplacePath;   // 不直接返回Application.persistentDataPath，是因为可能在其他线程中调用。。
        /// <summary>
        /// 工作区目录
        /// </summary>
        public static string workplacePath 
        {
            get
            {
                if (string.IsNullOrEmpty(_workplacePath))
                {
                    _workplacePath = Application.persistentDataPath;
                }

                return _workplacePath;
            }
        }

        public static string cachePath => $"{workplacePath}/cache";

        public static string webCachePath => $"{cachePath}/web";

        public static string gameMapCachePath => $"{cachePath}/gamemap";

        public static string gameMapsPath => $"{workplacePath}/maps";

        public static string gameMapProjsPath => $"{workplacePath}/projects";

        public static string downloadGameMapsPath => $"{gameMapsPath}/download";
        public static string scenesPath => $"{workplacePath}/scenes";
        public static string assetbunldesPath => $"{workplacePath}/assetbundles";

        /// <summary>
        /// 下载的资源文件目录，用于GameAssetDatabase的挂载点
        /// </summary>
        public const string kDownloadDir = "/Download";

        public const string kENV_PARAM_GAME = "-game";
        public const string kENV_PARAM_EDITOR = "-editor";

        public const string kPREFS_KEY_AUTH = "PlayerAuth";

        public const string kCONFIG_FILE_NAME = "config";

        private static int _fps = -1;
        /// <summary>
        /// fps
        /// </summary>
        public static int fps { 
            get
            {
                return _fps;
            }

            set
            {
                _fps = value;
                if(_fps < 0)
                {
                    Application.targetFrameRate = -1;
                    QualitySettings.vSyncCount = 1;
                }
                else
                {
                    QualitySettings.vSyncCount = 0;
                    Application.targetFrameRate = _fps;
                }
            }
        }

        public static int qualityLevel
        {
            get
            {
                return QualitySettings.GetQualityLevel();
            }

            set
            {
                QualitySettings.SetQualityLevel(value);
            }
        }

        /// <summary>
        /// 通用内容的初始化：
        /// 1. GameEntity的Prototype注册；
        /// 2. GameAssetDatabase 挂载；
        /// 3. GameMapManager 初始化；
        /// </summary>
        public static async void Init()
        {
            CreateWorkDirs();

            // 读取配置文件
            try
            {
                string configContent = File.ReadAllText($"{configPath}/{kCONFIG_FILE_NAME}");
                var jo = Newtonsoft.Json.Linq.JObject.Parse(configContent);
                fps = jo["fps"].ToObject<int>();
            }
            catch
            {
                Logger.Error("Read config error...");
            }

            // register sceneObject's types
            var typePrefabs = Resources.LoadAll<GameObject>("GameEntityPrototype");
            foreach (var v in typePrefabs)
            {
                GameEntityManager.RegisterPrototype(v.name, v);
            }

            // 默认资产
            GameAssetDatabase.MountDirectory($"{GameAssetDatabase.kBuiltInMountPoint}", $"{Gameplay.resourcePath}");
            // 下载的文件
            GameAssetDatabase.MountDirectory($"{Gameplay.kDownloadDir}", $"{Gameplay.workplacePath}");

            // 扫描AB包
            foreach (string f in Directory.GetFiles($"{Gameplay.resourcePath}", "*.annot", SearchOption.AllDirectories))
            {
                var dir = Path.GetDirectoryName(f);
                await GameAssetDatabase.MountAssetBundle($"/{Path.GetFileName(dir)}", dir);
            }

            // 注意，download中的ab如果和Resources中的同名时，会顶掉它
            foreach (string f in Directory.GetFiles($"{Gameplay.workplacePath}", "*.annot", SearchOption.AllDirectories))
            {
                var dir = Path.GetDirectoryName(f);
                await GameAssetDatabase.MountAssetBundle($"/{Path.GetFileName(dir)}", dir);
            }
        }

        /// <summary>
        /// 用于序列化配置更新
        /// </summary>
        public static void SaveConfigFile()
        {
            try
            {
                var file = $"{configPath}/{kCONFIG_FILE_NAME}";
                string configContent = File.ReadAllText(file);
                var jo = Newtonsoft.Json.Linq.JObject.Parse(configContent);
                jo["fps"] = fps;

                File.WriteAllText(file, jo.ToString());
            }
            catch
            {
                Logger.Error("Read config error...");
            }
        }

        /// <summary>
        /// 创建必备目录
        /// </summary>
        private static void CreateWorkDirs()
        {
            Directory.CreateDirectory(cachePath);
            Directory.CreateDirectory(gameMapCachePath);
            Directory.CreateDirectory(webCachePath);
            Directory.CreateDirectory(downloadGameMapsPath);
            Directory.CreateDirectory(scenesPath);
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public static void ClearCache()
        {
            if(Directory.Exists(cachePath))
            {
                Directory.Delete(cachePath, true);
            }

            Directory.CreateDirectory(cachePath);
        }

        public static void Release()
        {
            GameAssetDatabase.UnmountAll();
        }
    }

    #region Asset Parsers

    /// <summary>
    /// 按文件夹的方式组织
    /// </summary>
    //public class GameMapAssetParse : IAssetParser<GameMap>
    //{
    //    public const string kSuffix = ".map";
    //    public GameMap Parse(string path)
    //    {
    //        // 解析脚本
    //        var fileName = (new DirectoryInfo(path).Name);
    //        string content = File.ReadAllText($"{path}/{fileName}{kSuffix}");
    //        GameMap map = new GameMap();
    //        map.Parse(content);

    //        return map;
    //    }

    //    public void Save(string path, GameMap map, bool overwrite = true)
    //    {
    //        //if (!Directory.Exists(path))
    //        //{
    //        //    Directory.CreateDirectory(path);
    //        //}
    //        //var fileName = (new DirectoryInfo(path).Name);

    //        //GameMapWriter writer = new GameMapWriter();
    //        //writer.Save($"{path}/{fileName}{kSuffix}", map);

    //        throw new System.NotImplementedException();
    //    }
    //}

    /// <summary>
    /// GameMap文件解析
    /// </summary>
    public class GameMapFileParser : IAssetParser<GameMap>
    {
        public GameMap Parse(string path)
        {
            // 解析脚本
            if (File.Exists(path))
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        string content = reader.ReadToEnd();

                        GameMap map = GameMap.Create();
                        map.Parse(content);
                        return map;
                    }

                }
            }

            return null;
        }

        public void Save(string path, GameMap map, bool overwrite = true)
        {
            //GameMapWriter writer = new GameMapWriter();
            //writer.Save($"{path}", map);

            //// 生成缩略图
            //var dir = $"{Path.GetDirectoryName(path)}/.thumbnail";
            //if (!Directory.Exists(dir))
            //{
            //    Directory.CreateDirectory(dir);
            //}
            //Utils.CreateSceneThumbnail($"{dir}/{Path.GetFileNameWithoutExtension(path)}.png", Screen.width, Screen.height, Camera.main);

            throw new System.NotImplementedException();
        }
    }

    #endregion
}
