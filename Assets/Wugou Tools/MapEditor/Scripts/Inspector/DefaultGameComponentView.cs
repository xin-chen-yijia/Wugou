using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Wugou.MapEditor
{
    /// <summary>
    /// 默认组件页面，目前不支持自定义类型属性
    /// </summary>
    public class DefaultGameComponentView : GameComponentView
    {
        public override bool CheckTargetValid()
        {
            return target && target.GetComponent<GameComponent>() != null;
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
                if (Utils.IsList(field.FieldType))
                {
                    viewObj = PropertyViewManager.instance.CreateListView();
                }
                else
                {
                    viewObj = PropertyViewManager.instance.CreateFieldView(field);

                }
                if (viewObj)
                {
                    //viewObj.SetValue(field.GetValue(target.GetComponent(t)));
                    viewObj.head = field.Name;
                    viewObj.AddUpdateEvent(() =>
                    {
                        field.SetValue(GetComponentInParent<GameComponentView>().target.GetComponent(t), viewObj.GetValue());
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
                    if (Utils.IsList(field.PropertyType))
                    {
                        viewObj = PropertyViewManager.instance.CreateListView();
                    }
                    else
                    {
                        viewObj = PropertyViewManager.instance.CreatePropertyView(field);
                    }

                    if (viewObj)
                    {
                        //viewObj.SetValue(field.GetValue(target.GetComponent(t)));
                        viewObj.head = field.Name;
                        viewObj.AddUpdateEvent(() =>
                        {
                            field.SetValue(GetComponentInParent<GameComponentView>().target.GetComponent(t), viewObj.GetValue());
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

            if(gameObject.activeInHierarchy)
            {
                foreach (var v in fieldViews)
                {
                    v.Value.GetComponent<PropertyView>().SendMessage("SetValue", v.Key.GetValue(target.GetComponent(targetComponentType)));
                }

                foreach (var v in propertyViews)
                {
                    v.Value.GetComponent<PropertyView>().SendMessage("SetValue", v.Key.GetValue(target.GetComponent(targetComponentType)));
                }
            }

        }
    }

}
