using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Wugou.Editor.UI
{
    /// <summary>
    /// 默认组件页面，目前不支持自定义类型属性
    /// </summary>
    public class DefaultGameComponentView : GameComponentView
    {
        public override bool CheckTargetValid()
        {
            return componentGameObject && componentGameObject.GetComponent<MonoBehaviour>() != null;
        }

        private Dictionary<FieldInfo, PropertyView> fieldViews = new Dictionary<FieldInfo, PropertyView>();
        private Dictionary<PropertyInfo, PropertyView> propertyViews = new Dictionary<PropertyInfo, PropertyView>();

        private Type targetComponentType = null;

        public void Apply(Type t)
        {
            targetComponentType = t;

            // HideInComponentViewAttribute 优先
            // public 属性显示
            foreach (var field in t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<HideInComponentViewAttribute>() != null)
                {
                    continue;
                }

                if (field.GetCustomAttribute<EntitySerializeFieldAttribute>() != null)
                {
                    var customPropertyAttr = field.GetCustomAttribute<EditorPropertyAttribute>(false);
                    var headName = customPropertyAttr != null ? customPropertyAttr.name : field.Name;
                    PropertyView viewObj = CreatePropertyView(headName, customPropertyAttr, field.FieldType, t, (obj, val) =>
                    {
                        field.SetValue(obj, val);
                    });

                    if (viewObj)
                    {
                        viewObj.transform.SetParent(content);
                        fieldViews.Add(field, viewObj);
                    }
                }

            }

            //  property and serialize field
            foreach (var field in t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<HideInComponentViewAttribute>() != null)
                {
                    continue;
                }

                if (field.GetCustomAttribute<EntitySerializeFieldAttribute>() != null)
                {
                    var customPropertyAttr = field.GetCustomAttribute<EditorPropertyAttribute>(false);
                    var headName = customPropertyAttr != null ? customPropertyAttr.name : field.Name;
                    var setter = ReflectionHelper.CreatePropertySetterWrapper(field);   // 用委托优化
                    PropertyView viewObj = CreatePropertyView(headName, customPropertyAttr, field.PropertyType, t, (obj, val) =>
                    {
                        //field.SetValue(obj, val);
                        setter.Set(obj, val);
                    });

                    if (viewObj)
                    {
                        viewObj.transform.SetParent(content);
                        propertyViews.Add(field, viewObj);
                    }
                }

            }
        }

        private PropertyView CreatePropertyView(string name, EditorPropertyAttribute attribute, Type propertyType, Type declaringType, Action<object, object> setValue)
        {
            var headName = attribute != null ? attribute.name : name;
            // 回调
            MethodInfo setValCb = null;
            if (!string.IsNullOrEmpty(attribute?.callback))
            {
                setValCb = declaringType.GetMethod(attribute.callback, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[] { propertyType }, null);
            }

            Action<object> onValueChanged = (value) =>
            {
                var compObj = componentGameObject.GetComponent(declaringType);
                setValue(compObj, value);

                if (setValCb != null)
                {
                    setValCb.Invoke(compObj, new object[] { value });
                }
            };

            PropertyView viewObj = null;
            if (attribute?.viewType != null)
            {
                viewObj = PropertyViewManager.CreatePropertyView(headName, attribute.viewType, onValueChanged);
            }
            else
            {
                viewObj = PropertyViewManager.CreatePropertyView(headName, PropertyViewManager.GetDefaultViewTypeOf(propertyType), onValueChanged);
            }

            //  
            viewObj.OnCustomUI(propertyType, attribute?.extra);

            return viewObj;
        }

        public override void OnNewTarget(GameObject target)
        {
            base.OnNewTarget(target);

            var targetComp = target.GetComponent(targetComponentType);
            foreach (var v in fieldViews)
            {
                v.Value.SetValue(v.Key.GetValue(targetComp));
            }

            foreach (var v in propertyViews)
            {
                v.Value.SetValue(v.Key.GetValue(targetComp));
            }

        }

        // Start is called before the first frame update
        //public override void Start()
        //{
        //    base.Start();

        //}

        // Update is called once per frame
        //void Update()
        //{

        //}
    }

}
