using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 基本的游戏实体，作用类似Unity的GameObject
    /// </summary>
    public class GameEntity : GameComponent
    {
        /// <summary>
        /// id 用于唯一标识（包括网络同步的情况下）
        /// </summary>
        [HideInInspector]
        public int id;

        /// <summary>
        /// 名称，name被占用了，所以用这个
        /// </summary>
        [SerializeField]
        public string entityName { get { return name; } set { name = value; } }

        /// <summary>
        /// 用于实例化本物体
        /// </summary>
        public GameEntityBlueprint blueprint;

        [SerializeField]
        public Vector3 position { get { return transform.position; } set { transform.position = value; } }

        [SerializeField]
        public Vector3 eulerAngles { get { return transform.eulerAngles; } set { transform.eulerAngles = value; } }


    }
}
