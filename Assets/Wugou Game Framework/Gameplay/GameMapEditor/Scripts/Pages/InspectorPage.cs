using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.Editor;
using Wugou.UI;


namespace Wugou.Editor.UI
{
    public class InspectorPage : UIBaseWindow
    {
        public GameObject componentViewContainer;
        public GameObject componentViewPrefab;
        public GameObject propertyViewParent;

        public GameObject target { get; private set; }

        // Start is called before the first frame update
        void Start()
        {
            // 
            PropertyViewManager.Init(componentViewContainer,componentViewPrefab,propertyViewParent);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            //
            PropertyViewManager.Unload();
        }

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
                    var commonView = PropertyViewManager.GetView<GameEntityCommonView>();
                    commonView.componentGameObject = target;
                    commonView.Show();

                    var transformView = PropertyViewManager.GetView<GameEntityTransformView>();
                    transformView.componentGameObject = target;
                    transformView.Show();

                    foreach (var comp in target.GetComponentsInChildren<MonoBehaviour>())
                    {
                        if (comp)   // 有script missing的时候
                        {
                            var view = PropertyViewManager.GetViewOfComponent(comp.GetType());
                            if (view)
                            {
                                view.componentGameObject = target;
                                view.Show();
                            }
                        }

                    }
                }
            }

        }


    }
}
