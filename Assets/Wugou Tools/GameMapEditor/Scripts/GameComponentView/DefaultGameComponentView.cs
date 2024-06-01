using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Wugou.Editor
{
    /// <summary>
    /// callback原型为void xxx(string value);
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CustomPropertyAttribute : Attribute
    {
        public string name { get;private set; }
        public string callback{ get;private set; }
        public CustomPropertyAttribute(string name, string callback = "")
        {
            this.name = name;
            this.callback = callback;
        }
    }

    /// <summary>
    /// 默认组件页面，目前不支持自定义类型属性
    /// </summary>
    public class DefaultGameComponentView : GameComponentView
    {
        public override bool CheckTargetValid()
        {
            return target && target.GetComponent<MonoBehaviour>() != null;
        }

        private Dictionary<FieldInfo, PropertyView> fieldViews = new Dictionary<FieldInfo, PropertyView>();
        private Dictionary<PropertyInfo, PropertyView> propertyViews = new Dictionary<PropertyInfo, PropertyView>();

        private Type targetComponentType = null;

        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();

        }

        // Update is called once per frame
        //void Update()
        //{

        //}

        public void Init(Type t)
        {
            targetComponentType = t;
            // hideininspector 优先
            // public 属性显示
            foreach (var field in t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if(field.GetCustomAttribute<HideInInspector>() != null)
                {
                    continue;
                }

                PropertyView viewObj = null;
                if (Utils.IsListOrArray(field.FieldType))
                {
                    if (!field.FieldType.IsArray) // TODO: 支持数组
                    {
                        viewObj = PropertyViewManager.instance.CreateListView();
                    }
                }
                else
                {
                    viewObj = PropertyViewManager.instance.CreateFieldView(field);

                }
                if (viewObj)
                {
                    viewObj.head = field.Name;
                    var customPropertyAttr = field.GetCustomAttribute<CustomPropertyAttribute>(false);
                    if (customPropertyAttr != null)
                    {
                        viewObj.head = customPropertyAttr.name;
                    }
                    viewObj.AddUpdateEvent(() =>
                    {
                        var compObj = GetComponentInParent<GameComponentView>().target.GetComponent(t);
                        field.SetValue(compObj, viewObj.GetValue());

                        // 回调
                        if (!string.IsNullOrEmpty(customPropertyAttr?.callback))
                        {
                            var cb = t.GetMethod(customPropertyAttr.callback, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            if (cb != null)
                            {
                                cb.Invoke(compObj, new object[] { viewObj.GetValue() });
                            }
                        }

                    });

                    viewObj.transform.SetParent(content);
                    fieldViews.Add(field, viewObj);
                }
            }

            //  property and serialize field
            foreach (var field in t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<HideInInspector>() != null)
                {
                    continue;
                }

                if (field.GetCustomAttribute<SerializeField>() != null)
                {
                    PropertyView viewObj = null;
                    if (Utils.IsListOrArray(field.PropertyType))
                    {
                        viewObj = PropertyViewManager.instance.CreateListView();
                    }
                    else
                    {
                        viewObj = PropertyViewManager.instance.CreatePropertyView(field);
                    }

                    if (viewObj)
                    {
                        var customPropertyAttr = field.GetCustomAttribute<CustomPropertyAttribute>(false);
                        if (customPropertyAttr != null)
                        {
                            viewObj.head = customPropertyAttr.name;
                        }
                        viewObj.AddUpdateEvent(() =>
                        {
                            var compObj = GetComponentInParent<GameComponentView>().target.GetComponent(t);
                            field.SetValue(compObj, viewObj.GetValue());

                            // 回调
                            if (!string.IsNullOrEmpty(customPropertyAttr?.callback))
                            {
                                var cb = t.GetMethod(customPropertyAttr.callback, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                                if (cb != null)
                                {
                                    cb.Invoke(compObj, new object[] { viewObj.GetValue() });
                                }
                            }

                        });

                        viewObj.transform.SetParent(content);
                        propertyViews.Add(field, viewObj);
                    }
                }

            }
        }

        public override void OnNewTarget(GameObject target)
        {
            base.OnNewTarget(target);

            foreach (var v in fieldViews)
            {
                v.Value.GetComponent<PropertyView>().SetValue(v.Key.GetValue(target.GetComponent(targetComponentType)));
            }

            foreach (var v in propertyViews)
            {
                v.Value.GetComponent<PropertyView>().SetValue(v.Key.GetValue(target.GetComponent(targetComponentType)));
            }

        }
    }

}
