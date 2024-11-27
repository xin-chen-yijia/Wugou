using System.Collections;
using System;

namespace Wugou
{
    /// <summary>
    /// 定义组件属性界面
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomGameComponentView : Attribute
    {
        public Type componentViewType { get; private set; }

        public CustomGameComponentView(Type componentviewType)
        {
            this.componentViewType = componentviewType;
        }
    }
}
