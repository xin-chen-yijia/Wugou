using System;
using UnityEngine;
using TMPro;

namespace Wugou.Editor.UI
{
    /// <summary>
    /// 属性视图
    /// 
    /// 可优化：SetValue和GetValue的拆装箱操作，封装一个自定义的Value类型，提供GetInt,SetInt等操作
    /// </summary>
    public class PropertyView : MonoBehaviour
    {
        public TMP_Text headText;
        public virtual string head
        {
            get { return headText.text; }
            set { headText.text = value; }
        }

        public virtual void SetValue(object value)
        {
            throw new NotImplementedException();
        }

        public virtual object GetValue()
        {
            throw new NotImplementedException();
        }

        public virtual void SetValueChangedCallback(System.Action<object> action)
        {
            Logger.Warning($"{GetType()} not SetValueChangedCallback, maybe wrong..");
        }

        /// <summary>
        /// 用于需要某些特殊定制的场景，如滑杆定制最大、最小值等
        /// </summary>
        /// <param name="propertyType"></param>
        /// <param name="param"></param>
        public virtual void OnCustomUI(Type propertyType, object param)
        {

        }
    }    
}
