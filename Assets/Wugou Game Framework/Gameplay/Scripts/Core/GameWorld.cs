using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wugou
{
    /// <summary>
    /// Game World, collection of GameEntities
    /// </summary>
    public static class GameWorld
    {
        // 记录已放置的物体，用于后续更改或删除
        public static List<GameEntity> gameEntities { get; private set; } = new List<GameEntity>();

        /// <summary>
        /// 所加载的场景中自带的GameEntity（非动态实例化出来的）
        /// </summary>
        private static List<GameEntity> gameEntitiesExistFromTheBeginning_ = new List<GameEntity>();

        /// <summary>
        /// Unity当前加载的场景
        /// </summary>
        public static Scene activeScene { get; private set; }

        /// <summary>
        /// 当前加载场景的操作
        /// </summary>
        public static AsyncOperation loadingSceneOperation { get; private set; }

        /// <summary>
        /// 当前加载的地图
        /// </summary>
        public static GameMap loadedMap { get; set; } = null;

        /// <summary>
        /// 当前场景的加载模式
        /// </summary>
        public static LoadSceneMode loadSceneMode { get; private set; } = LoadSceneMode.Single;

        /// <summary>
        /// 是否已加载完毕脚本
        /// </summary>
        public static bool isLoading { get; private set; } = false;

        /// <summary>
        /// 中断加载，用于加载场景或大模型耗时过长的情况，此时Unity会阻塞，这个可能对外部外部逻辑会有影响，如网络通信
        /// true:如果此时正在Loading，则中断这个过程
        /// </summary>
        public static bool interruptLoading { get; set; } = false;

        /// <summary>
        /// 当前使用的天气系统
        /// </summary>
        public static WeatherSystemBase weatherSystem { get; set; } = new DefaultWeatherSystem();

        /// <summary>
        /// 根据id查找脚本中的物体
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static GameEntity GetGameEntity(int id)
        {
            foreach(var v in gameEntities)
            {
                if(v.id == id)
                {
                    return v;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取一开始就存在于场景的GameEntity，主要用于实例化
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static GameEntity GetGameEntityExistFromTheBeginning(int id)
        {
            foreach (var v in gameEntitiesExistFromTheBeginning_)
            {
                if (v.id == id)
                {
                    return v;
                }
            }

            return null;
        }

        /// <summary>
        /// 根据名称查找脚本中的物体
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static GameEntity Find(string name)
        {
            foreach(var entity in gameEntities)
            {
                Debug.Assert(entity != null);
                if(entity.name == name)
                {
                    return entity;
                }
            }

            return null;
        }

        /// <summary>
        /// 检查对象是否是场景中的物体
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static bool ExistsEntity(GameEntity entity)
        {
            for(int i = 0; i < gameEntities.Count; ++i)
            {
                if(entity == gameEntities[i])
                {
                    Debug.Assert(GameEntityManager.Find(entity.id) != null);
                    return true;
                }
            }

            return false;
        }

        public static bool ExistsObject(GameObject gameObject)
        {
            for (int i = 0; i < gameEntities.Count; ++i)
            {
                if (gameObject == gameEntities[i].gameObject)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 向GameWorld中添加一个已存在的GameEntity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static GameEntity AddGameEntity(GameEntity entity)
        {
            Debug.Assert(GameEntityManager.Find(entity.id) != null);
            var root = entity.transform.root.gameObject;
            if (root.scene != activeScene)
            {
                SceneManager.MoveGameObjectToScene(root, activeScene);
            }

            gameEntities.Add(entity);

            return entity;
        }

        /// <summary>
        /// 从场景删除物体
        /// </summary>
        /// <param name="entity"></param>
        public static void RemoveGameEntity(GameEntity entity)
        {
            Debug.Assert(loadedMap == null || (loadedMap != null && ExistsEntity(entity)));
            gameEntities.Remove(entity);
        }

        /// <summary>
        /// 从场景删除物体
        /// </summary>
        /// <param name="entityId"></param>
        public static void RemoveGameEntity(int entityId)
        {
            var entity = GameWorld.GetGameEntity(entityId);
            RemoveGameEntity(entity);
        }

        /// <summary>
        /// 替换GameEntity,用于某些情况
        /// </summary>
        /// <param name="oldEntity"></param>
        /// <param name="newEntity"></param>
        public static void ReplaceGameEntity(GameEntity oldEntity, GameEntity newEntity)
        {
            Debug.Assert(GameEntityManager.Find(newEntity.id) != null);
            for (int i=0; i < gameEntities.Count; ++i)
            {
                if (gameEntities[i] == oldEntity)
                {
                    gameEntities[i] = newEntity;
                    break;
                }
            }
        }

        public enum Error
        {
            kSuccess = 0,
            kErrorLoadWhenLoading,
            kErrorNotUnloadGameMap,
            kErrorOldVersion,
            kErrorNoScene,
            kErrorInterruptLoading,
            kExtractGameMapFail,
            kErrorGameMapError
        }

        private static List<string> loadedResources_ = new List<string>();

        /// <summary>
        /// 用于加载GameMap所需的资源
        /// </summary>
        /// <param name="path"></param>
        private static async Task<bool> LoadGameMapAssetInternal(string path)
        {
            var resourcesDir = $"{path}/{GameConsole.resourceName}";
            if (!Directory.Exists(resourcesDir))
            {
                return false;
            }
            // 加载ab包
            return await GameConsole.MountAssetbundles(resourcesDir);

            // TODO: 加载文件夹？
        }

        /// <summary>
        /// 加载游戏脚本
        /// </summary>
        /// <param name="path"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static async Task<Error> LoadGameMapPackage(string path, LoadSceneMode mode)
        {
            var cachePath = $"{GameConsole.gameMapCachePath}";

            string packageName = Path.GetFileNameWithoutExtension(path);
            GameMapPackage.Extract($"{path}", cachePath);
            var packageDir = $"{cachePath}/{packageName}";
            if (!Directory.Exists(packageDir))
            {
                Logger.Error($"Extract {path} to {cachePath} fail...");
                return Error.kExtractGameMapFail;
            }

            _ = await LoadGameMapAssetInternal(packageDir);

            //
            GameMapFileParser mapParse = new GameMapFileParser();
            var gameMapObj = mapParse.Parse($"{packageDir}/{packageName}{GameConsole.kGameMapFileSuffix}");

            return await LoadGameMap(gameMapObj, mode);
        }

        /// <summary>
        /// 加载游戏脚本
        /// </summary>
        /// <param name="map"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static async Task<Error> LoadGameMap(GameMap gameMap, LoadSceneMode mode)
        {
            // 正在加载脚本
            if (isLoading)
            {
                Logger.Error("Is loading GameMap!");
                return Error.kErrorLoadWhenLoading;
            }

            // 未卸载一个脚本
            if (loadedMap != null)
            {
                Logger.Error($"There already load a map. Please call UnloadGameMap first.");
                return Error.kErrorNotUnloadGameMap;
            }

            Debug.Assert(gameEntities.Count == 0);
            Debug.Assert(GameEntityManager.GetGameEntitiesCount() == 0);
            if (gameEntities.Count > 0)
            {
                Logger.Error("There maybe not unload gamemap.");
            }

            loadSceneMode = mode;

            // read map file, use property first, because map may create with empty tempalte
            int mapVersion = gameMap.version;
            if (mapVersion < GameMap.kLatestVersion)
            {
                Logger.Error($"game map version error. latest supported version is {GameMap.kLatestVersion}, but the version of game map to load is {mapVersion}.");
                return Error.kErrorOldVersion;
            }

            // 为了读取entity
            GameMapReader mapReader = new GameMapReader(gameMap.rawContent);
            // enter scene
            isLoading = true;

            // 加载ab包中的scene
            var isLoadSucc = await GameAssetDatabase.LoadScene(gameMap.scene);
            if (!isLoadSucc)
            {
                Logger.Error($"GameAssetDatabase load {gameMap.scene} fail.");
                interruptLoading = false;
                isLoading = false;

                return Error.kErrorNoScene;
            }

            if (interruptLoading)
            {
                interruptLoading = false;
                isLoading = false;

                return Error.kErrorInterruptLoading;
            }

            string sceneName = GameAssetDatabase.GetRawAssetName(gameMap.scene);
            loadingSceneOperation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            await loadingSceneOperation;
            loadingSceneOperation = null;

            // 设置活动场景
            activeScene = SceneManager.GetSceneByName(sceneName.Replace(".unity",""));  // 不需要.unity的后缀
            SceneManager.SetActiveScene(activeScene);

            // 场景中已有的GameEntity
            gameEntitiesExistFromTheBeginning_.AddRange(GameObject.FindObjectsOfType<GameEntity>(true));
            gameEntities.AddRange(gameEntitiesExistFromTheBeginning_);

#if UNITY_EDITOR
            // 检查是否存在相同id
            HashSet<int> tmpEntityIds = new HashSet<int>();
            for (int i = 0; i < gameEntitiesExistFromTheBeginning_.Count; i++)
            {
                Debug.Assert(!tmpEntityIds.Contains(gameEntitiesExistFromTheBeginning_[i].id));
                tmpEntityIds.Add(gameEntitiesExistFromTheBeginning_[i].id);
            }
#endif

            // entity 实例化
            try
            {
                var entities = mapReader.ReadEntities();
                for (int i = 0; i < entities.Count; i++)
                {
                    var entity = entities[i];
                    Debug.Assert(!entity.isFromTheBeginning);
                    SceneManager.MoveGameObjectToScene(entity.gameObject, GameWorld.activeScene);

                    gameEntities.Add(entity);
                }

                // 等待全部实例化完成
                await new EnumeratorAwaiter(new WaitUntil(() =>
                {
                    for (int i = 0; i < entities.Count; i++)
                    {
                        if (entities[i].body == null)
                        {
                            return false;
                        }
                    }
                    return true;
                }));

            }
            catch (System.Exception ex)
            {
                Logger.Error($"读取游戏脚本出错！ {ex.Message}");
                Logger.LogExcpetion(ex);
                return Error.kErrorGameMapError;
            }


            // 加载额外的游戏相关逻辑 
            if (GameConsole.isGaming)
            {
                // 加载GameScript
                var pb = Resources.Load<GameObject>($"GameScripts/{gameMap.gameScript}");
                if (pb)
                {
                    var obj = GameObject.Instantiate<GameObject>(pb);
                    obj.name = $"GameScript {gameMap.gameScript}";
                }
                else
                {
                    Logger.Error($"GameScripts {gameMap.gameScript} not exists...");
                }

                // TODO: 加载js脚本

            }

            if (gameMap.needWeather)
            {
                LoadWeather(gameMap.weather);
            }

            // 开启天气的音量
            GameSoundsManager.activeHandler.SetMusicVolume(0.3f);
            GameSoundsManager.activeHandler.SetAmbienceVolume(0.3f);
            GameSoundsManager.activeHandler.SetWeatherVolume(0.3f);

            // 
            foreach (var v in gameEntities)
            {
                v.GetComponent<ILoadedGameMap>()?.OnLoadedGameMap();
            }

            // check gamemap dependencies
            var mapDependencies = mapReader.ReadDependencies();
            foreach (var v in mapDependencies)
            {
                var mountPoint = $"/{v.Key}";
                var fs = GameAssetDatabase.GetFileSystem(mountPoint);
                if (fs != null)
                {
                    var abFS = fs as AssetBundleFileSystem;
                    if (abFS != null)
                    {
                        if (abFS.assetLoader.GetDescFileMD5() != v.Value)
                        {
                            Logger.Warning($"Assetbundle: {v.Key}'s md5 is not same with that in gamemap");
                        }
                    }
                }
            }

            // 更新脚本
            loadedMap = gameMap;

            isLoading = false;

            return Error.kSuccess;
        }

        /// <summary>
        /// 卸载已加载的脚本（异步）
        /// 1. Single模式加载的脚本，只销毁GameEntity，不卸载场景；
        /// 2. Additive模式则卸载场景；
        /// 建议在UnloadGameMap后加载一个新场景
        /// </summary>
        public static void UnloadGameMap()
        {
            // 清理天气系统
            weatherSystem.Unload();

            if (loadedMap != null)
            {
                if (loadSceneMode != LoadSceneMode.Single)
                {
                    // 卸载场景
                    SceneManager.UnloadSceneAsync(activeScene);
                }
                else
                {

                    foreach(var v in gameEntities)
                    {
                        if (v && v.gameObject)
                        {
                            GameObject.Destroy(v.gameObject);
                        }
                    }
                }

                loadedMap = null;
            }

            // finally clear all gameobjects
            gameEntities.Clear();
            gameEntitiesExistFromTheBeginning_.Clear();

            foreach (var v in loadedResources_)
            {
                GameAssetDatabase.Unmount(v);
            }

            isLoading = false;
            loadingSceneOperation = null;
        }

        public static void LoadWeather(WeatherSetting weather)
        {
            // 加载天气系统
            Debug.Assert(weatherSystem != null);
            weatherSystem.Load(() =>
            {
                // 应用脚本中的天气
                weatherSystem.ChangeWeather(weather.type);
                weatherSystem.time = weather.time;
                weatherSystem.fogDensity = weather.fogDensity;
                weatherSystem.windForce = weather.windForce;
                weatherSystem.windDirection = weather.windDir;
            });
        }

        /// <summary>
        /// 获取脚本中的起始位置数量
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public static int GetStartPositionCount()
        {
            int startPositionCount = 0;
            foreach (var v in gameEntities)
            {
                startPositionCount += v.GetComponent<StartPosition>() ? 1 : 0;
            }

            return startPositionCount;
        }
    }

}
