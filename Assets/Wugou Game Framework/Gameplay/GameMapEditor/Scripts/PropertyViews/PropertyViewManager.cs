using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Wugou.Editor.UI
{
    /// <summary>
    /// 用于编辑器中的对象属性编辑的界面
    /// </summary>
    public static class PropertyViewManager
    {
        public static GameObject componentViewContainer { get; private set; }

        public static GameObject componentViewPrefab { get; private set; }

        public static GameObject propertyViewsParent { get; private set; }  

        public static void Init(GameObject inViewContainer, GameObject inComponentViewPrefab, GameObject inPropertyViewsParent)
        {
            componentViewContainer = inViewContainer;
            componentViewPrefab = inComponentViewPrefab;
            propertyViewsParent = inPropertyViewsParent;
        }

        /// <summary>
        /// 清理创建的属性界面
        /// </summary>
        /// <param name="destroyAllViews"></param>
        public static void Unload(bool destroyAllViews = false)
        {
            if (destroyAllViews)
            {
                foreach(var v in componentViews)
                {
                    GameObject.Destroy(v.Value.gameObject);
                }
            }
            componentViews.Clear();
        }

        private static Dictionary<Type, GameComponentView> componentViews = new Dictionary<Type, GameComponentView>();

        /// <summary>
        /// 根据类型生成默认的属性界面
        /// </summary>
        /// <param name="headName"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private static DefaultGameComponentView GetDefaultGameComponentView(string headName, Type t)
        {
            GameObject viewObj = GameObject.Instantiate(componentViewPrefab, componentViewPrefab.transform.parent);
            viewObj.SetActive(true);

            var defaultView = viewObj.GetComponent<DefaultGameComponentView>();
            defaultView.name = headName;
            defaultView.collapsibleView.head = headName;
            defaultView.Apply(t);
            defaultView.transform.SetParent(componentViewContainer.transform);

            return defaultView;
        }

        /// <summary>
        /// 获取组件的界面
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static GameComponentView GetViewOfComponent(Type t)
        {
            var viewAttr = t.GetCustomAttribute<CustomGameComponentView>();
            if (viewAttr != null)
            {
                return componentViewContainer.GetComponentInChildren(viewAttr.componentViewType, true) as GameComponentView;
            }

            // use default
            if (componentViews.ContainsKey(t) && componentViews[t])
            {
                return componentViews[t];
            }

            var attribute = t.GetCustomAttribute<DefaultGameComponentViewAttribute>();
            if (attribute == null)
            {
                return null;
            }

            // create default view
            var defaultView = GetDefaultGameComponentView(attribute.name, t);
            componentViews[t] = defaultView;

            return defaultView;
        }

        /// <summary>
        /// 获取属性界面
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static GameComponentView GetView<T>() where T : GameComponentView
        {
            return GetView(typeof(T));
        }

        /// <summary>
        /// 获取属性界面
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static GameComponentView GetView(Type t)
        {
            if (componentViews.ContainsKey(t))
            {
                return componentViews[t];
            }

            return componentViewContainer.GetComponentInChildren(t,true) as GameComponentView;
        }

        /// <summary>
        /// 实例化目标类型的PropertyView组件
        /// </summary>
        /// <param name="viewType"></param>
        /// <returns></returns>
        public static PropertyView InstantiatePropertyView(Type viewType)
        {
            var comp = propertyViewsParent.GetComponentInChildren(viewType,true);
            if (comp)
            {
                var po = GameObject.Instantiate(comp.gameObject, propertyViewsParent.transform);
                po.SetActive(true);

                var viewObj = po.GetComponent<PropertyView>();
                return viewObj;
            }

            return null;
        }

        /// <summary>
        /// 默认属性面板类型
        /// </summary>
        private static Dictionary<Type, Type> defaultPropertyViewMap = new Dictionary<Type, Type>()
        {
            { typeof(int), typeof(IntPropertyView) },
            { typeof(float), typeof(FloatPropertyView) },
            { typeof(Vector3), typeof(Vector3PropertyView) },
            { typeof(bool), typeof(BoolPropertyView) },
            { typeof(string), typeof(StringPropertyView) },
            { typeof(Color), typeof(ColorPropertyView) },
        };

        /// <summary>
        /// 获取变量类型默认用的属性UI组件类型
        /// </summary>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static Type GetDefaultViewTypeOf(Type valueType)
        {
            if (Utils.IsListOrArray(valueType))
            {
                return typeof(ListPropertyView);
            }

            if (Utils.IsEnumType(valueType))
            {
                return typeof(EnumPropertyView);
            }

            if (defaultPropertyViewMap.ContainsKey(valueType))
            {
                return defaultPropertyViewMap[valueType];
            }

            return null;
        }

        /// <summary>
        /// 根据属性类型实例化组件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="propertyViewType"></param>
        /// <returns></returns>
        public static PropertyView CreatePropertyView(string name, Type propertyViewType)
        {
            return CreatePropertyView(name, propertyViewType, null, null);
        }

        /// <summary>
        /// 根据属性类型实例化组件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="propertyViewType"></param>
        /// <param name="onValueChanged"></param>
        /// <returns></returns>
        public static PropertyView CreatePropertyView(string name, Type propertyViewType, System.Action<object> onValueChanged)
        {
            return CreatePropertyView(name, propertyViewType, null, onValueChanged);
        }

        /// <summary>
        /// 根据属性类型实例化组件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="propertyViewType"></param>
        /// <param name="value"></param>
        /// <param name="onValueChanged"></param>
        /// <returns></returns>
        public static PropertyView CreatePropertyView(string name, Type propertyViewType, object value, System.Action<object> onValueChanged)
        {
            var viewObj = InstantiatePropertyView(propertyViewType);
            if (viewObj)
            {
                viewObj.head = name;
                viewObj.name = name;
                if (value != null)
                {
                    viewObj.SetValue(value);
                }

                viewObj.SetValueChangedCallback(onValueChanged);
            }

            return viewObj;
        }

        //// Start is called before the first frame update
        //void Start()
        //{

        //}

        //// Update is called once per frame
        //void Update()
        //{

        //}
    }
}
