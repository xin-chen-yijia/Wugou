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
    public class  CustomPropertyView : Attribute
    {
        public string viewName { get; private set; }
        public CustomPropertyView(string viewName)
        {
            this.viewName = viewName;
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
    }

    public class PropertyView<T> : PropertyView
    {

        protected T value_ = default(T);
        public UnityEvent<T> onValueChanged = new UnityEvent<T>();
        public virtual void SetValue(T value)
        {
            transform.Find("Input").GetComponent<InputField>().text = value.ToString();
        }

        public virtual T ParseFromString(string value)
        {
            throw new NotImplementedException();
        }

        protected virtual void Start()
        {
            transform.Find("Input").GetComponent<InputField>().onValueChanged.AddListener((val) =>
            {
                onValueChanged.Invoke(ParseFromString(val));
            });
        }
    }
    
}
