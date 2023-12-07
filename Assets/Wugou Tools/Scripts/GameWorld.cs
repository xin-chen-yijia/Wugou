using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wugou
{
    public static class GameWorld
    {

        // 记录已放置的物体，用于后续更改或删除
        public static List<GameEntity> gameEntities { get; private set; } = new List<GameEntity>();

        /// <summary>
        /// Unity当前加载的场景
        /// </summary>
        public static Scene activeScene { get; private set; }

        /// <summary>
        /// 当前加载的地图
        /// </summary>
        public static GameMap loadedMap { get; set; } = null;

        /// <summary>
        /// 当前从哪个assetbundle加载的场景
        /// </summary>
        public static AssetBundleScene loadedAssetbundleScene => loadedMap.scene;

        /// <summary>
        /// 当前场景的加载模式
        /// </summary>
        private static LoadSceneMode loadSceneMode = LoadSceneMode.Single;

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
        /// 根据名字查找脚本中的物体
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
        /// 检查对象是否是场景中的物体
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static bool Exists(GameObject gameObject)
        {
            for(int i = 0; i < gameEntities.Count; ++i)
            {
                if(gameObject == gameEntities[i].gameObject)
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
            gameEntities.Add(entity);

            return entity;
        }

        /// <summary>
        ///  向场景中添加新物体，同时会向加载的脚本中添加该项
        /// </summary>
        /// <param name="blueprint"></param>
        public static GameEntity AddGameEntity(GameEntityBlueprint blueprint)
        {
            var entity = GameEntityManager.CreateGameEntity(blueprint);
            SceneManager.MoveGameObjectToScene(entity.gameObject, GameWorld.activeScene);

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
            var bp = new GameEntityBlueprint();
            bp.asset = asset;
            bp.prototype = prototype;
            return AddGameEntity(bp);
        }

        /// <summary>
        /// 向GameWorld中添加一个已存在的物体
        /// </summary>
        /// <param name="gameObject"></param>
        public static void AddGameObject(GameObject gameObject)
        {
            if (gameObject.GetComponent<GameEntity>())
            {
                AddGameEntity(gameObject.GetComponent<GameEntity>());
            }
            else
            {
                Logger.Warning($"{gameObject.name} not have GameEntity");
            }
        }

        /// <summary>
        /// 从场景删除物体
        /// </summary>
        /// <param name="gameObject"></param>
        public static void RemoveGameObject(GameObject gameObject)
        {
            if (Exists(gameObject))
            {
                gameEntities.Remove(gameObject.GetComponent<GameEntity>());
                GameEntityManager.RemoveGameEntity(gameObject.GetComponent<GameEntity>());
            }
            else
            {
                Logger.Error($"GameWorld have no {gameObject.name}");
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

        /// <summary>
        /// 实例化物体，但不会在脚本中记录该项
        /// </summary>
        /// <param name="sceneObject"></param>
        /// <returns></returns>
        //public static async Task<GameObject> InstantiateSceneObject(GameEntity sceneObject)
        //{
        //    var go = await sceneObject.Instantiate(resourceRootDir);
        //    SceneManager.MoveGameObjectToScene(go, GameWorld.activeScene);

        //    // apply component
        //    sceneObject.ApplyComponents(go);

        //    sceneObjectsAndGameObjectsDict_.Add(go, sceneObject);



        //    return go;
        //}

        /// <summary>
        /// 加载脚本
        /// </summary>
        /// <param name="mapContent"></param>
        /// <param name="onLoadedGameMap"></param>
        /// <param name="mode"></param>
        public static void LoadMap(string mapContent, Action onLoadedGameMap, LoadSceneMode mode = LoadSceneMode.Single)
        {
            // 同一时间只能加载一个脚本
            if (loadedMap != null)
            {
                Logger.Error($"There already load a map. Please call UnloadGameMap first.");
                return;
            }
            if (string.IsNullOrEmpty(mapContent))
            {
                Logger.Info($"Load Empty map");
                return;
            }

            GameMap map = new GameMap();
            map.Parse(mapContent);

            if (map == loadedMap)
            {
                Logger.Info($"{mapContent} already loaded....");
                return;
            }

            LoadMap(map, onLoadedGameMap, mode);
        }

        public static void LoadMap(GameMap map, Action onLoadedGameMap, LoadSceneMode mode = LoadSceneMode.Single)
        {
            //  
            loadSceneMode = mode;

            // 新脚本
            if(string.IsNullOrEmpty(map.rawContent))
            {
                //map.content = "";
            }

            // read map file, use property first, because map may create with empty tempalte
            int mapVersion = map.version;
            if (mapVersion < GameMap.kLatestVersion)
            {
                Logger.Error($"game map version {mapVersion} less than current supported version {GameMap.kLatestVersion}.");
                return;
            }

            // 为了读取entity
            GameMapReader mapReader = new GameMapReader(map.rawContent);
            // enter scene
            isLoading = true;

            // 修正一下assebundle路径
            AssetBundleScene correctedScene = map.scene;
            AssetBundleDesc desc = map.scene.assetbundle;
            desc.path = $"{GamePlay.settings.resourcePath}/{desc.path}";
            correctedScene.assetbundle = desc;

            AssetBundleSceneManager.LoadScene(correctedScene, mode, async () => {

                // 加载天气系统
                WeatherSystem.Load();

                // 等一帧，有些插件如Unistorm weather等需要初始化
                await new YieldInstructionAwaiter(null).Task;

                if (interruptLoading)
                {
                    interruptLoading = false;
                    isLoading = false;

                    if(mode == LoadSceneMode.Additive)
                    {
                        await SceneManager.UnloadSceneAsync(map.scene.sceneName);
                    }

                    return;
                }

                activeScene = SceneManager.GetSceneByName(map.scene.sceneName);
                SceneManager.SetActiveScene(activeScene);

                // entity 实例化
                var entities = mapReader.ReadEntities();
                for (int i = 0; i < entities.Count; i++)
                {
                    var entity = entities[i];
                    await GameEntity.Instantiate(entity);
                    SceneManager.MoveGameObjectToScene(entity.gameObject, GameWorld.activeScene);

                    gameEntities.Add(entity);
                }

                // 读取天气
                WeatherSystem.activeWeather = map.weather;
                WeatherSystem.ApplyWeather();

                // 
                onLoadedGameMap?.Invoke();

                // 加载新脚本
                loadedMap = map;

                isLoading = false;
            });
        }

        /// <summary>
        /// 卸载已加载的脚本（异步）
        /// </summary>
        public static void UnloadGameMap()
        {
            if (loadedMap != null)
            {
                if (loadSceneMode != LoadSceneMode.Single)
                {
                    // 卸载场景
                    AssetBundleSceneManager.UnLoadSceneAsync(activeScene.name);
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
        }


        /// <summary>
        /// 保存
        /// </summary>
        /// <returns></returns>
        public static JObject ToJson()
        {
            JObject jo = new JObject();

            // 写入所有的assetbundle
            //Dictionary<string, AssetBundleDesc> assetbundlesDict = new Dictionary<string, AssetBundleDesc>();
            //assetbundlesDict.Add(GameWorld.loadedAssetbundleScene.assetbundle.path, GameWorld.loadedAssetbundleScene.assetbundle);
            //for (int i = 0; i < GameWorld.gameEntities.Count; i++)
            //{
            //    var ab = GameWorld.gameEntities[i].blueprint.assetDesc.assetbundle;
            //    if (!string.IsNullOrEmpty(ab.path))
            //    {
            //        assetbundlesDict[ab.path] = ab;
            //    }
            //}

            var serializer = JsonSerializerGlobal.commonSerializer;

            //jo.Add("assetbundles", JArray.FromObject(assetbundlesDict.Values.ToArray()));

            // scene
            jo.Add("scene", JToken.FromObject(loadedAssetbundleScene, serializer));

            // max player count
            jo.Add("maxPlayerCount", GetStartPositionCount());

            // 写入gameentity
            jo.Add("entities", JArray.FromObject(GameWorld.gameEntities, serializer));

            // 写入天气
            jo.Add("weather", JToken.FromObject(WeatherSystem.activeWeather));

            return jo;
        }

        public const string kStartPositionTypeStr = "StartPosition";
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
                startPositionCount += v.blueprint.prototype == kStartPositionTypeStr ? 1 : 0;
            }

            return startPositionCount;
        }
    }

}
