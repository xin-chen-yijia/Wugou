using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.Editor;

namespace Wugou
{
    /// <summary>
    /// 用于存储文本数据，比如某些业务查询信息，序列化后的对象等，具体的使用逻辑放到业务脚本中
    /// 主要用于配合场景制作，避免每个场景中都有不同的脚本
    /// 类似ScriptableObject, 不用它的原因是都差不多，这个可以直接添到物体上，不需要再搞个名字对应了
    /// </summary>
    [DefaultGameComponentView]
    [System.Serializable]
    public class PlainTextComponent : GameComponent
    {
        [SerializeField]
        public string text;        
    }
}
