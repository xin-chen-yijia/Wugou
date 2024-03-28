using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
        /// 根据名称查找脚本中的物体
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static GameEntity Find(string name)
        {
            foreach(var entity in gameEntities)
            {
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
        public static GameEntity AddExistGameEntity(GameEntity entity)
        {
            gameEntities.Add(entity);

            return entity;
        }

        /// <summary>
        /// 向场景和脚本中同时添加物体
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="prototype"></param>
        /// <returns></returns>
        public static GameEntity AddGameEntity(string asset, string prototype)
        {
            var entity = GameEntityManager.CreateGameEntity(asset, prototype);
            SceneManager.MoveGameObjectToScene(entity.gameObject, GameWorld.activeScene);

            entity.asset = asset;
            entity.prototype = prototype;

            gameEntities.Add(entity);

            return entity;
        }

        /// <summary>
        /// 从场景删除物体
        /// </summary>
        /// <param name="entity"></param>
        public static void DestroyGameEntity(GameEntity entity)
        {
            if (ExistsEntity(entity))
            {
                gameEntities.Remove(entity);
                GameEntityManager.DestroyGameEntity(entity);
            }
            else
            {
                Logger.Error($"GameWorld have no {entity.name}");
            }
        }

        /// <summary>
        /// 替换GameEntity,用于某些情况
        /// </summary>
        /// <param name="oldEntity"></param>
        /// <param name="newEntity"></param>
        public static void ReplaceGameEntity(GameEntity oldEntity, GameEntity newEntity)
        {
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
        }

        private static List<string> loadedResources_ = new List<string>();

        /// <summary>
        /// 用于加载GameMap所需的资源
        /// </summary>
        /// <param name="path"></param>
        private static async Task<bool> LoadGameMapAssetInternal(string path)
        {
            // 加载ab包
            var dirs = Directory.GetDirectories(path);
            foreach(var dir in dirs)
            {
                if(Directory.GetFiles(dir, "*.annot").Length > 0)
                {
                    var mountPoint = $"/{Path.GetFileName(dir)}";
                    await GameAssetDatabase.MountAssetBundle(mountPoint, dir);

                    loadedResources_.Add(mountPoint);
                }
            }

            return true;
        }

        /// <summary>
        /// 加载游戏脚本
        /// </summary>
        /// <param name="path"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static async Task<Error> LoadGameMapPackage(string path, LoadSceneMode mode)
        {
            var cachePath = $"{Gameplay.gameMapCachePath}";

            string packageName = Path.GetFileNameWithoutExtension(path);
            GameMapPackage.Extract($"{path}", cachePath);
            var packageDir = $"{cachePath}/{packageName}";
            if (!Directory.Exists(packageDir))
            {
                Logger.Error($"Extract {path} to {cachePath} fail...");
                return Error.kExtractGameMapFail;
            }

            _ = await LoadGameMapAssetInternal(cachePath);

            //
            GameMapFileParser mapParse = new GameMapFileParser();
            var gameMapObj = mapParse.Parse($"{cachePath}/{packageName}/{packageName}{Gameplay.kGameMapFileSuffix}");

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

            if (gameEntities.Count > 0)
            {
                Logger.Error("There maybe not unload gamemap.");
            }

            loadSceneMode = mode;

            // read map file, use property first, because map may create with empty tempalte
            int mapVersion = gameMap.version;
            if (mapVersion < GameMap.kLatestVersion)
            {
                Logger.Error($"game map version {mapVersion} less than current supported version {GameMap.kLatestVersion}.");
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

            // scene 要下一帧才真正完成
            await new YieldInstructionAwaiter(null);
            activeScene = SceneManager.GetSceneByName(sceneName.Replace(".unity",""));  // 不需要.unity的后缀

            // entity 实例化
            var entities = mapReader.ReadEntities();
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                await entity.InstantiateBody();
                SceneManager.MoveGameObjectToScene(entity.gameObject, GameWorld.activeScene);

                gameEntities.Add(entity);
            }

            // check gamemap dependencies
            var mapDependencies = mapReader.ReadDependencies();
            foreach(var v in mapDependencies)
            {
                var mountPoint = $"/{v.Key}";
                var fs = GameAssetDatabase.GetFileSystem(mountPoint);
                if (fs != null)
                {
                    var abFS = fs as AssetBundleFileSystem;
                    if(abFS != null)
                    {
                        if(abFS.assetLoader.GetDescFileMD5() != v.Value)
                        {
                            Logger.Warning($"Assetbundle: {v.Key}'s md5 is not same with that in gamemap");
                        }
                    }
                }
            }

            if (gameMap.needWeather)
            {
                // 加载天气系统
                WeatherSystem.Load();

                // 应用脚本中的天气
                WeatherSystem.activeWeather = gameMap.weather;
                WeatherSystem.ApplyWeather();
            }


            // 加载新脚本
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

            foreach (var v in loadedResources_)
            {
                GameAssetDatabase.Unmount(v);
            }

            // 清理天气系统
            WeatherSystem.Clear();

            isLoading = false;
            loadingSceneOperation = null;
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
