using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Wugou.Multiplayer;

namespace Wugou
{
    /// <summary>
    /// 基本的游戏实体，作用类似Unity的GameObject
    /// </summary>
    [CopyToClient]
    public class GameEntity : GameComponent
    {
        /// <summary>
        /// id 用于唯一标识（包括网络同步的情况下）
        /// </summary>
        [HideInInspector]
        public int id;

        /// <summary>
        /// 名称，name被GameObject占用了，所以用这个
        /// </summary>
        [HideInInspector]
        [SerializeField]
        public string entityName { get { return name; } set { name = value; } }

        /// <summary>
        /// 用于实例化本物体
        /// </summary>
        public GameEntityBlueprint blueprint;

        [HideInInspector]
        [SerializeField]
        public Vector3 position { get { return transform.position; } set { transform.position = value; } }

        [HideInInspector]
        [SerializeField]
        public Vector3 eulerAngles { get { return transform.eulerAngles; } set { transform.eulerAngles = value; } }

        /// <summary>
        /// 实例化物体
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        public static async Task<GameObject> Instantiate(GameEntity entity)
        {
            var entityAsset = entity.blueprint.asset;
            GameObject goPfb = await GameAssetDatabase.LoadAssetAsync<GameObject>(entityAsset);
            GameObject go = GameObject.Instantiate<GameObject>(goPfb, entity.transform);
            if (!go)
            {
                go = new GameObject();
                go.transform.SetParent(entity.transform);
            }

            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            // 保持和entity相同tag和layer属性
            go.layer = entity.gameObject.layer;
            go.tag = entity.gameObject.tag;
            go.name = "Body";

            return go;
        }

    }
}
