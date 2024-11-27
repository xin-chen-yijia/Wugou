using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// GameEntity实例化Body时调用
    /// </summary>
    public interface IOnLoadedGameEntityBody
    {
        void OnLoadedGameEntityBody(GameObject body);
    }
}
