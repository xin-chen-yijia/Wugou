using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using Wugou.UI;

namespace Wugou.MapEditor
{
    public class PropertyViewManager : MonoBehaviour
    {
        public static PropertyViewManager instance { get; private set; }

        public GameObject viewContainer;

        public GameObject componentViewPrefab;

        public IntPropertyView intView;
        public FloatPropertyView floatView;
        public StringPropertyView stringView;
        public Vector3PropertyView vector3View;

        public ListPropertyView listView;

        Dictionary<Type, System.Func<MemberInfo, PropertyView>> views = new Dictionary<Type, System.Func<MemberInfo, PropertyView>>();

        private void Awake()
        {
            instance = this;

            views.Add(typeof(int), (MemberInfo t) =>
            {
                var po = Instantiate<GameObject>(intView.gameObject,transform);
                po.SetActive(true);

                return po.GetComponent<IntPropertyView>();
            });
            views.Add(typeof(float), (MemberInfo t) =>
            {
                var po = Instantiate<GameObject>(floatView.gameObject, transform);
                po.SetActive(true);

                return po.GetComponent<FloatPropertyView>();
            });
            views.Add(typeof(string), (MemberInfo t) =>
            {
                var po = Instantiate<GameObject>(stringView.gameObject, transform);
                po.SetActive(true);

                return po.GetComponent<StringPropertyView>();
            });
            views.Add(typeof(Vector3), (MemberInfo t) =>
            {
                var po = Instantiate<GameObject>(vector3View.gameObject, transform);
                po.SetActive(true);

                return po.GetComponent<Vector3PropertyView>();
            });
        }

        private static Dictionary<Type, GameComponentView> componentViews = new Dictionary<Type, GameComponentView>();

        /// <summary>
        /// 获取组件的界面
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public GameComponentView GetViewOfComponent(Type t)
        {
            var viewAttr = t.GetCustomAttribute<CustomGameComponentView>();
            if (viewAttr != null)
            {
                return viewContainer.GetComponentInChildren(viewAttr.componentViewType, true) as GameComponentView;
            }

            // use default
            if (componentViews.ContainsKey(t))
            {
                return componentViews[t];
            }

            // create default view
            GameObject viewObj = Instantiate<GameObject>(componentViewPrefab,componentViewPrefab.transform.parent);
            viewObj.SetActive(true);

            var defaultView = viewObj.GetComponent<DefaultGameComponentView>();
            defaultView.name = t.Name;
            defaultView.collapsibleView.head = t.Name;
            defaultView.Init(t);
            defaultView.transform.SetParent(viewContainer.transform);

            componentViews[t] = defaultView;

            return defaultView;
        }

        private void AddOnValueChange<T>(GameObject go, MemberInfo t)
        {
            var view = go.GetComponent<PropertyView>();
            view.head = t.Name;
            view.AddUpdateEvent(() =>
            {
                if (t is FieldInfo ft)
                {
                    ft.SetValue(GetComponentInParent<GameComponentView>().target, view.GetValue());
                }

                if (t is PropertyInfo pt)
                {
                    pt.SetValue(GetComponentInParent<GameComponentView>().target, view.GetValue());
                }
            });
        }

        public PropertyView CreateFieldView(FieldInfo field)
        {
            if (views.ContainsKey(field.FieldType))
            {
                return views[field.FieldType]?.Invoke(field);
            }

            return null;
        }

        public PropertyView CreatePropertyView(PropertyInfo property)
        {
            if (views.ContainsKey(property.PropertyType))
            {
                return views[property.PropertyType]?.Invoke(property);
            }

            return null;
        }

        /// <summary>
        /// 创建属性UI
        /// </summary>
        /// <param name="name"></param>
        /// <param name="valType"></param>
        /// <param name="setValue"></param>
        /// <param name="getValue"></param>
        /// <returns></returns>
        public GameObject CreateView(string name, Type valType, System.Action<object> setValue, System.Func<object> getValue)
        {
            if (valType == typeof(int))
            {
                var po = Instantiate<GameObject>(intView.gameObject, transform);
                po.SetActive(true);
                po.GetComponent<IntPropertyView>().SetValue((int)getValue());
                var view = po.GetComponent<IntPropertyView>();
                view.head = name;
                view.AddUpdateEvent(() =>
                {
                    setValue(view.GetValue());
                });

                return po;
            }
            else if (valType == typeof(float))
            {
                var po = Instantiate<GameObject>(floatView.gameObject, transform);
                po.SetActive(true);
                po.GetComponent<FloatPropertyView>().SetValue((float)getValue());
                var view = po.GetComponent<FloatPropertyView>();
                view.head = name;
                view.AddUpdateEvent(() =>
                {
                    setValue(view.GetValue());
                });

                return po;
            }
            else if(valType == typeof(string))
            {
                var po = Instantiate<GameObject>(stringView.gameObject, transform);
                po.SetActive(true);
                po.GetComponent<StringPropertyView>().SetValue((string)getValue());
                var view = po.GetComponent<StringPropertyView>();
                view.head = name;
                view.AddUpdateEvent(() =>
                {
                    setValue(view.GetValue());
                });

                return po;
            }

            return null;
        }

        /// <summary>
        /// 创建一个list的UI
        /// </summary>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public PropertyView CreateListView()
        {
            GameObject go = GameObject.Instantiate<GameObject>(listView.gameObject, listView.transform.parent);
            go.SetActive(true);

            return go.GetComponent<ListPropertyView>();
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
