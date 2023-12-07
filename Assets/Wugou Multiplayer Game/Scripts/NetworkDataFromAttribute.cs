using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.Multiplayer
{
    /// <summary>
    /// 用于指定数据来源
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public class NetworkDataFromAttribute : System.Attribute
    {
        public Type targetType { get; private set; }
        public NetworkDataFromAttribute(Type t) {
            targetType = t;
        }
    }
}
