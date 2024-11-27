using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 针对于GameEntity的组件
    /// </summary>
    public class GameComponent : MonoBehaviour
    {
        public GameEntity gameEntity => GetComponent<GameEntity>();

        public GameObject body => gameEntity.body;
    }

}
