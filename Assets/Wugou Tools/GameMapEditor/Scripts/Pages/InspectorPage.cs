using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.Editor;
using Wugou.UI;
using Wugou;

using Logger = Wugou.Logger;
using System;
using System.Reflection;

namespace Wugou.Editor.UI
{
    public class InspectorPage : UIBaseWindow
    {
        public GameObject target { get; private set; }

        //// Start is called before the first frame update
        //void Start()
        //{

        //}

        //// Update is called once per frame
        //void Update()
        //{

        //}

        /// <summary>
        /// 显示对象属性
        /// </summary>
        /// <param name="obj"></param>
        public void SetTarget(GameObject obj)
        {
            if (target != obj)
            {
                target = obj;

                // hide all
                foreach (var v in GetComponentsInChildren<GameComponentView>(true))
                {
                    v.Hide();
                }

                if (target != null)
                {
                    var commonView = GetComponentInChildren<GameEntityCommonView>(true);
                    commonView.target = target;
                    commonView.Show();

                    var transformView = GetComponentInChildren<GameEntityTransformView>(true);
                    transformView.target = target;
                    transformView.Show();

                    foreach (var comp in target.GetComponentsInChildren<MonoBehaviour>())
                    {
                        if (comp)   // 有script missing的时候
                        {
                            var view = PropertyViewManager.instance.GetViewOfComponent(comp.GetType());
                            if (view)
                            {
                                view.target = obj;
                                view.Show();
                            }
                        }

                    }
                }
            }

        }


    }
}
