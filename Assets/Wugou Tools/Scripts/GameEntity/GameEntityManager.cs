using Wugou.MapEditor;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Wugou
{
    public static class GameEntityManager
    {
        /// <summary>
        /// 资源路径
        /// </summary>
        public static string resourcePath { get; set; }

        /// <summary>
        /// 记录所有的游戏实体
        /// </summary>
        public static List<GameEntity> allGameEntities { get; private set; } = new List<GameEntity>();

        /// <summary>
        /// 创建一个游戏实体
        /// </summary>
        /// <param name="desc"></param>
        /// <param name="prototype"></param>
        /// <returns></returns>
        public static GameEntity CreateGameEntity(AssetBundleAsset desc, string prototype)
        {
            return CreateGameEntity(new GameEntityBlueprint() { assetDesc = desc, prototype = prototype });
        }

        private static int sEntityid_ = 0;

        /// <summary>
        /// 创建一个游戏实体
        /// </summary>
        /// <param name="blueprint"></param>
        /// <returns></returns>
        public static GameEntity CreateGameEntity(GameEntityBlueprint blueprint)
        {
            var pfb = GameEntityManager.GetPrefab(blueprint.prototype);   //  获取原型
            if (!pfb)
            {
                pfb = GameEntityManager.GetPrefab(GameEntityManager.DefaultTypeName);
                Debug.Assert(pfb != null);
                if (!pfb)
                {
                    Logger.Error($"SceneObjectTypePrefabSystem have no prototype:{blueprint.prototype} and 'Default' type. Maybe there no register SceneObjectType.");
                }
                else
                {
                    Logger.Warning($"SceneObjectTypePrefabSystem have no prototype:{blueprint.prototype}. Use default fallback.");
                }
            }

            var go = GameObject.Instantiate<GameObject>(pfb);
            var entity = go.GetComponent<GameEntity>();
            entity.blueprint = blueprint;
            entity.id = sEntityid_++;   // 简单自增, TODO: 专门的id生成器

            allGameEntities.Add(entity);
            return entity;
        }

        public static void RemoveGameEntity(GameEntity entity)
        {
            allGameEntities.Remove(entity);
            GameObject.Destroy(entity.gameObject);
        }

        /// <summary>
        /// 实例化物体
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        public static async Task<GameObject> InstantiateGameObject(GameEntity entity)
        {
            var loader = await AssetBundleAssetLoader.GetOrCreate($"{resourcePath}/{entity.blueprint.assetDesc.assetbundle.path}");
            var goMem = await loader.LoadAssetAsync<GameObject>(entity.blueprint.assetDesc.asset);
            var go = GameObject.Instantiate(goMem, entity.transform);   // 挂到原型物体下
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            // 保持和entity相同tag和layer属性
            Utils.SetLayerRecursively(go, entity.gameObject.layer);
            go.tag = entity.gameObject.tag;

            return go;
        }

        public static void SerializeEntity(GameObject go)
        {
            //name = go.name;
            //position = go.transform.position;
            //eulerAngles = go.transform.eulerAngles;
            //localScale = go.transform.localScale;

            ////
            //components.Clear();
            //foreach (var v in go.GetComponents<MonoBehaviour>())
            //{
            //    if (v is ISerialize comSerializer)
            //    {
            //        components.Add(v.GetType().Name, comSerializer.Serialize());
            //    }
            //}
        }

        ///// <summary>
        ///// 应用保存的组件内容到指定物体，即反序列化
        ///// </summary>
        ///// <param name="go"></param>
        //public void ApplyComponents(GameObject go)
        //{
        //    foreach (var v in go.GetComponents<MonoBehaviour>())
        //    {
        //        if (v is ISerialize comSerializer)
        //        {
        //            var compName = v.GetType().Name;
        //            if (components.ContainsKey(compName))
        //            {
        //                comSerializer.Deserialize(components[compName]);
        //            }
        //        }
        //    }
        //}

        #region Prefab manager
        public const string DefaultTypeName = "Default";

        private static Dictionary<string, GameObject> sPrefabs_ = new Dictionary<string, GameObject>();
        public static void RegisterPrefab(string type, GameObject prefab)
        {
            sPrefabs_[type] = prefab;
        }

        public static GameObject GetPrefab(string type)
        {
            if (sPrefabs_.ContainsKey(type))
            {
                return sPrefabs_[type];
            }

            return null;
        }

        public static void ClearPrefab()
        {
            sPrefabs_.Clear();
        }
        #endregion
    }
}
