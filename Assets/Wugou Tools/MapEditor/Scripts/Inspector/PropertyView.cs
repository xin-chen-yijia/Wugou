using Wugou.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Wugou.MapEditor
{
    using Text = TMPro.TMP_Text;
    using InputField = TMPro.TMP_InputField;

    [AttributeUsage(AttributeTargets.Class)]
    public class  CustomGameComponentView : Attribute
    {
        public Type componentViewType { get; private set; }
        public CustomGameComponentView(Type componentviewType)
        {
            this.componentViewType = componentviewType;
        }
    }

    /// <summary>
    ///  Ù–‘ ”Õº
    /// </summary>
    public class PropertyView : MonoBehaviour
    {
        public virtual string head
        {
            get { return gameObject.transform.Find("Name").GetComponent<Text>().text; }
            set { gameObject.transform.Find("Name").GetComponent<Text>().text = value; }
        }

        protected object value_;

        public virtual void SetValue(object value)
        {
            value_ = value;
            transform.Find("Input").GetComponent<InputField>().text = value.ToString();
        }

        public virtual object GetValue()
        {
            return value_;
        }

        public virtual void ParseFromString(string value)
        {
            throw new NotImplementedException();
        }

        public virtual void AddUpdateEvent(System.Action action)
        {
            transform.Find("Input").GetComponent<InputField>().onValueChanged.AddListener((val) =>
            {
                ParseFromString(val);
                action?.Invoke();
            });
        }
    }    
}
