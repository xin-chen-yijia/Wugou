using System;

namespace Wugou
{
    /// <summary>
    /// 用于隐藏某个属性在右侧属性面板的显示，同HideInInspector，不用HideInInspector的原因是方便调试
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class HideInComponentViewAttribute : Attribute { }
}
