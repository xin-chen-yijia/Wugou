using Wugou.MapEditor;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System;

namespace Wugou
{
    public static class GameEntityManager
    {
        /// <summary>
        /// 记录所有的游戏实体
        /// </summary>
        public static List<GameEntity> allGameEntities { get; private set; } = new List<GameEntity>();

        /// <summary>
        /// 用已有的物体创建一个Entity
        /// </summary>
        /// <returns></returns>
        public static GameEntity CreateGameEntity(GameObject gameObject)
        {
            var entity = gameObject.GetComponent<GameEntity>();
            if (!entity)
            {
                entity = gameObject.AddComponent<GameEntity>();
            }
            entity.id = AllocateEntityId();

            allGameEntities.Add(entity);
            return entity;
        }

        /// <summary>
        /// 创建一个游戏实体
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="prototype"></param>
        /// <returns></returns>
        public static GameEntity CreateGameEntity(string asset, string prototype)
        {
            return CreateGameEntity(new GameEntityBlueprint() { asset = asset, prototype = prototype });
        }

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
            entity.id = AllocateEntityId();   

            allGameEntities.Add(entity);
            return entity;
        }

        /// <summary>
        /// 分配EntityID
        /// </summary>
        /// <returns></returns>
        private static int AllocateEntityId()
        {
            // 假设有效期50年，需要6位，月份 4位 ， 日期 5位 ， 秒 17位
            // 这个假设应该够了。。。
            int id = 0;
            var date = DateTime.Now;
            id |= ((date.Year - 2000) << 26);
            id |= ((date.Month) << 22);
            id |= ((date.Day) << 17);
            id |= (date.Hour * 3600 +date.Minute * 60 +date.Second);

            return id;
        }

        /// <summary>
        /// 根据id找GameEntity
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static GameEntity Find(int id)
        {
            for(int i = 0; i < allGameEntities.Count; ++i)
            {
                if (allGameEntities[i].id == id)
                {
                    return allGameEntities[i];
                }
            }

            return null;
        }

        public static void RemoveGameEntity(GameEntity entity)
        {
            allGameEntities.Remove(entity);
            GameObject.Destroy(entity.gameObject);
        }

        #region Prefab manager
        public const string DefaultTypeName = "Default";

        private static Dictionary<string, GameObject> sPrefabs_ = new Dictionary<string, GameObject>()
        {
            {"Default", Resources.Load<GameObject>("GameEntityPrototypes/Default") },
            {"StartPosition", Resources.Load<GameObject>("GameEntityPrototypes/StartPosition") }
        };
        public static void RegisterPrefab(string type, GameObject prefab)
        {
            Debug.Assert(!string.IsNullOrEmpty(type) && prefab);
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
