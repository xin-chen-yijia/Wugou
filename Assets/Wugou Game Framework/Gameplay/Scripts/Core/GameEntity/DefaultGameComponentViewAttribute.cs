using System;

namespace Wugou
{
    /// <summary>
    /// 默认的组件属性界面
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DefaultGameComponentViewAttribute : Attribute
    {
        public string name { get; private set; }
        public DefaultGameComponentViewAttribute(string name)
        {
            this.name = name;
        }
    }
}
