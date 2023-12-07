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

        // ��¼�ѷ��õ����壬���ں������Ļ�ɾ��
        public static List<GameEntity> gameEntities { get; private set; } = new List<GameEntity>();

        /// <summary>
        /// Unity��ǰ���صĳ���
        /// </summary>
        public static Scene activeScene { get; private set; }

        /// <summary>
        /// ��ǰ���صĵ�ͼ
        /// </summary>
        public static GameMap loadedMap { get; set; } = null;

        /// <summary>
        /// ��ǰ���ĸ�assetbundle���صĳ���
        /// </summary>
        public static AssetBundleScene loadedAssetbundleScene => loadedMap.scene;

        /// <summary>
        /// ��ǰ�����ļ���ģʽ
        /// </summary>
        private static LoadSceneMode loadSceneMode = LoadSceneMode.Single;

        /// <summary>
        /// �Ƿ��Ѽ�����Ͻű�
        /// </summary>
        public static bool isLoading { get; private set; } = false;

        /// <summary>
        /// �жϼ��أ����ڼ��س������ģ�ͺ�ʱ�������������ʱUnity��������������ܶ��ⲿ�ⲿ�߼�����Ӱ�죬������ͨ��
        /// true:�����ʱ����Loading�����ж��������
        /// </summary>
        public static bool interruptLoading { get; set; } = false;

        /// <summary>
        /// �������ֲ��ҽű��е�����
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
        /// �������Ƿ��ǳ����е�����
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
        /// ��GameWorld�����һ���Ѵ��ڵ�GameEntity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static GameEntity AddGameEntity(GameEntity entity)
        {
            gameEntities.Add(entity);

            return entity;
        }

        /// <summary>
        ///  �򳡾�����������壬ͬʱ������صĽű�����Ӹ���
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
        /// �򳡾��ͽű���ͬʱ�������
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
        /// ��GameWorld�����һ���Ѵ��ڵ�����
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
        /// �ӳ���ɾ������
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
        /// �滻GameEntity,����ĳЩ���
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
        /// ʵ�������壬�������ڽű��м�¼����
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
        /// ���ؽű�
        /// </summary>
        /// <param name="mapContent"></param>
        /// <param name="onLoadedGameMap"></param>
        /// <param name="mode"></param>
        public static void LoadMap(string mapContent, Action onLoadedGameMap, LoadSceneMode mode = LoadSceneMode.Single)
        {
            // ͬһʱ��ֻ�ܼ���һ���ű�
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

            // �½ű�
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

            // Ϊ�˶�ȡentity
            GameMapReader mapReader = new GameMapReader(map.rawContent);
            // enter scene
            isLoading = true;

            // ����һ��assebundle·��
            AssetBundleScene correctedScene = map.scene;
            AssetBundleDesc desc = map.scene.assetbundle;
            desc.path = $"{GamePlay.settings.resourcePath}/{desc.path}";
            correctedScene.assetbundle = desc;

            AssetBundleSceneManager.LoadScene(correctedScene, mode, async () => {

                // ��������ϵͳ
                WeatherSystem.Load();

                // ��һ֡����Щ�����Unistorm weather����Ҫ��ʼ��
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

                // entity ʵ����
                var entities = mapReader.ReadEntities();
                for (int i = 0; i < entities.Count; i++)
                {
                    var entity = entities[i];
                    await GameEntity.Instantiate(entity);
                    SceneManager.MoveGameObjectToScene(entity.gameObject, GameWorld.activeScene);

                    gameEntities.Add(entity);
                }

                // ��ȡ����
                WeatherSystem.activeWeather = map.weather;
                WeatherSystem.ApplyWeather();

                // 
                onLoadedGameMap?.Invoke();

                // �����½ű�
                loadedMap = map;

                isLoading = false;
            });
        }

        /// <summary>
        /// ж���Ѽ��صĽű����첽��
        /// </summary>
        public static void UnloadGameMap()
        {
            if (loadedMap != null)
            {
                if (loadSceneMode != LoadSceneMode.Single)
                {
                    // ж�س���
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
        /// ����
        /// </summary>
        /// <returns></returns>
        public static JObject ToJson()
        {
            JObject jo = new JObject();

            // д�����е�assetbundle
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

            // д��gameentity
            jo.Add("entities", JArray.FromObject(GameWorld.gameEntities, serializer));

            // д������
            jo.Add("weather", JToken.FromObject(WeatherSystem.activeWeather));

            return jo;
        }

        public const string kStartPositionTypeStr = "StartPosition";
        /// <summary>
        /// ��ȡ�ű��е���ʼλ������
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
