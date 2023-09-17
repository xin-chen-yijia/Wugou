using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.MapEditor;
using Wugou.UI;
using Wugou;

using Logger = Wugou.Logger;

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

            // 
            foreach(var v in GetComponentsInChildren<GameComponentView>())
            {
                v.target = obj;
            }
        }
    }
}
