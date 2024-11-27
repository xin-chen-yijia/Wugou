using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Wugou
{
    /// <summary>
    /// 用于创建GameEntity和管理GameEntity
    /// </summary>
    public static class GameEntityManager
    {
        /// <summary>
        /// 记录所有的游戏实体
        /// </summary>
        private static List<GameEntity> allGameEntities { get; } = new List<GameEntity>();

        /// <summary>
        /// 用已有的物体创建一个Entity
        /// </summary>
        /// <returns></returns>
        public static GameEntity CreateGameEntity(GameObject gameObject)
        {
            return CreateGameEntity(gameObject, AllocateEntityId());
        }

        /// <summary>
        /// 用已有的物体创建一个Entity，并指定id
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public static GameEntity CreateGameEntity(GameObject gameObject, int entityId)
        {
            return CreateGameEntity(gameObject, entityId, GameEntity.DefaultPrototype, "");
        }

        /// <summary>
        /// 用已有的物体创建一个Entity，并指定id
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public static GameEntity CreateGameEntity(GameObject gameObject, int entityId, string prototype, string asset)
        {
            Debug.Assert(gameObject.GetComponent<GameEntity>() == null);

            var entity = gameObject.AddComponent<GameEntity>();
            entity.id = entityId;
            entity.prototype = prototype;
            entity.asset = asset;

            allGameEntities.Add(entity);
            return entity;
        }

        /// <summary>
        /// 创建一个游戏实体
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="asset"></param>
        /// <returns></returns>
        public static GameEntity CreateGameEntity(string prototype, string asset)
        {
            return CreateGameEntity(AllocateEntityId(), prototype, asset);
        }

        /// <summary>
        /// 创建一个游戏实体
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="prototype"></param>
        /// <param name="asset"></param>
        /// <returns></returns>
        public static GameEntity CreateGameEntity(int entityId, string prototype, string asset)
        {
            var pfb = GetPrototype(prototype);   //  获取原型
            if (!pfb)
            {
                pfb = GetPrototype(GameEntity.DefaultPrototype);
                Debug.Assert(pfb != null);
                if (!pfb)
                {
                    Logger.Error($"SceneObjectTypePrefabSystem have no prototype:{prototype} and 'Default' type. Maybe there no register SceneObjectType.");
                }
                else
                {
                    Logger.Warning($"SceneObjectTypePrefabSystem have no prototype:{prototype}. Use default fallback.");
                }
            }

            var go = GameObject.Instantiate<GameObject>(pfb);
            var entity = go.GetComponent<GameEntity>();
            Debug.Assert(entity != null);
            entity.asset = asset;
            entity.prototype = prototype;
            entity.id = entityId;

            allGameEntities.Add(entity);
            return entity;
        }

        /// <summary>
        /// 添加已有的GameEntity
        /// </summary>
        /// <param name="entity"></param>
        public static void RegisterGameEntity(GameEntity entity)
        {
            Debug.Assert(entity != null && !allGameEntities.Contains(entity));

            allGameEntities.Add(entity);
        }

        /// <summary>
        /// 分配EntityID
        /// 基于时间分配id，注意因为是int 32位，1秒的精度也不太可能达到，不保证没有重复
        /// </summary>
        /// <returns></returns>
        public static int AllocateEntityIdTime64517()
        {
            // 年6位（假设有效期50年），月份4位 ， 日期5位， 秒17位
            // 24个小时的秒数应该要24位，但不想用long类型，将就用int。。。
            int id = 0;
            var date = DateTime.Now;
            id |= ((date.Year - 2000) << 26);
            id |= ((date.Month) << 22);
            id |= ((date.Day) << 17);
            id |= (date.Hour * 3600 +date.Minute * 60 +date.Second);

            return id;

        }

        /// <summary>
        /// 使用Guid生成整形唯一id
        /// </summary>
        /// <returns></returns>
        public static int AllocateEntityId()
        {
            Guid uniqueId = Guid.NewGuid(); // 创建新的全局唯一标识符（GUID）
            int id = BitConverter.ToInt32(uniqueId.ToByteArray(), 0); // 将GUID转换为int类型的唯一ID
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

        public static void DestroyGameEntity(GameEntity entity)
        {
            Debug.Assert(entity != null && allGameEntities.Contains(entity));
            allGameEntities.Remove(entity);
            GameObject.Destroy(entity.gameObject);
        }


        public static GameEntity DuplicateGameEntity(GameEntity entity)
        {
            Debug.Assert(entity);
            var newEntity = GameObject.Instantiate<GameObject>(entity.gameObject);
            var entityComp = newEntity.GetComponent<GameEntity>();
            entityComp.id = AllocateEntityId();
            entityComp.name = $"{entity.name} clone";
            entityComp.asset = entity.asset;
            entityComp.prototype = entity.prototype;

            allGameEntities.Add(entityComp);

            return entityComp;
        }

        public static void DestroyAllGameEntity()
        {
            foreach(var v in allGameEntities)
            {
                if(v != null)
                {
                    GameObject.Destroy(v.gameObject);
                }
            }

            allGameEntities.Clear();
        }

        public static int GetGameEntitiesCount()
        {
            return allGameEntities.Count;
        }

        #region Prefab manager

        private static Dictionary<string, GameObject> _sPrototypes = null;
        private static Dictionary<string, GameObject> sPrototypes
        {
            get
            {
                if(_sPrototypes == null)
                {
                    _sPrototypes = new Dictionary<string, GameObject>()
                    {
                        {"Default", Resources.Load<GameObject>("DefaultGameEntityPrototypes/Default") },
                        {"StartPosition", Resources.Load<GameObject>("DefaultGameEntityPrototypes/StartPosition") },
                        {"Trigger", Resources.Load<GameObject>("DefaultGameEntityPrototypes/Trigger") },
                        {"TextBoard", Resources.Load<GameObject>("DefaultGameEntityPrototypes/TextBoard") },
                        {"InformationBox", Resources.Load<GameObject>("DefaultGameEntityPrototypes/InformationBox") },
                        {"CameraPose", Resources.Load<GameObject>("DefaultGameEntityPrototypes/CameraPose") },
                    };
                }

                return _sPrototypes;
            }
        } 

        public static void RegisterPrototype(string type, GameObject prefab)
        {
            Debug.Assert(!string.IsNullOrEmpty(type) && prefab);
            sPrototypes[type] = prefab;
        }

        public static GameObject GetPrototype(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return null;
            }

            if (sPrototypes.ContainsKey(type))
            {
                return sPrototypes[type];
            }

            return null;
        }

        public static List<GameObject> GetAllPrototypes()
        {
            return new List<GameObject>(sPrototypes.Values);
        }

        public static void ClearPrototypes()
        {
            sPrototypes.Clear();
        }

        #endregion
    }
}
