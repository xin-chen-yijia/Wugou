using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.MapEditor;
using Wugou.UI;
using Wugou;

using Logger = Wugou.Logger;
using System;
using System.Reflection;

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
            foreach(var v in GetComponentsInChildren<GameComponentView>(true))
            {
                v.Hide();
            }

            if(target != null)
            {
                GetComponentInChildren<GameEntityCommonView>(true).target = target;
                GetComponentInChildren<GameEntityTransformView>(true).target = target;
                foreach (var comp in target.GetComponentsInChildren<GameComponent>())
                {
                    var view = PropertyViewManager.instance.GetViewOfComponent(comp.GetType());
                    if(view)
                    {
                        view.target = obj;
                    }
                }
            }
        }

    }


}
