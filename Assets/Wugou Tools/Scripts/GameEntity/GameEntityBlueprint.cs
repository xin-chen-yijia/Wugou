using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 游戏物体的生成描述，用于序列化和反序列化
    /// </summary>
    public class GameEntityBlueprint
    {
        /// <summary>
        /// 资产，用于创建可见模型
        /// </summary>
        public string asset;

        /// <summary>
        /// 原型，当前使用Unity Prefab来表示原型，即一个空的GameObject+Components表示场景中的对象
        /// </summary>
        public string prototype;
    }
}

