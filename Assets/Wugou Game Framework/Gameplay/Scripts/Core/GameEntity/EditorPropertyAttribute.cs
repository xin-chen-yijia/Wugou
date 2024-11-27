using System;

namespace Wugou
{
    /// <summary>
    /// callbackÔ­ÐÍÎªvoid xxx(string value);
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class EditorPropertyAttribute : Attribute
    {
        public string name { get; private set; }
        public string callback { get; private set; }
        public Type viewType { get; private set; }
        public object extra { get; private set; }

        public EditorPropertyAttribute(string name, string callback = "", Type viewType = null, object extra = null)
        {
            this.name = name;
            this.callback = callback;
            this.viewType = viewType;
            this.extra = extra;
        }
    }
}
