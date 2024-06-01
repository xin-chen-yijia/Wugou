using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 加载完GameMap时调用
    /// </summary>
    public interface ILoadedGameMap
    {
        void OnLoadedGameMap();
    }
}
