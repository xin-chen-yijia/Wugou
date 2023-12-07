using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.Multiplayer
{
    /// <summary>
    /// 用于指定GameComponent是否复制到客户端
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public class CopyToClientAttribute : System.Attribute
    {
    }
}
