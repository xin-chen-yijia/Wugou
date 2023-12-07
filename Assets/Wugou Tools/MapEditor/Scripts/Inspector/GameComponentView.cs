using Wugou.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.MapEditor
{
    public abstract class GameComponentView : MonoBehaviour
    {
        protected GameObject target_;
        public GameObject target { 
            get { return target_; } 
            set { target_ = value; 
                if (CheckTargetValid()) { Show(); OnNewTarget(value); }
                else Hide(); 
            } 
        }

        public UICollapsibleView collapsibleView => GetComponent<UICollapsibleView>();
        public Transform content => collapsibleView.content;
        private List<PropertyView> properties_ = new List<PropertyView>();

        /// <summary>
        /// 检查目标是否对本属性面板有效（即是否包含目标组件）
        /// </summary>
        /// <returns></returns>
        public abstract bool CheckTargetValid();

        public virtual void Start()
        {
            Debug.Assert(collapsibleView != null);
        }

        //public virtual void Update()
        //{
        //}

        protected void AddProperty(PropertyView view)
        {
            collapsibleView.AddContent(view.gameObject.GetComponent<RectTransform>());
            properties_.Add(view);
        }

        public void Resize() => collapsibleView.Resize();

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Fold() => collapsibleView?.Fold();
        public void Unfold() => collapsibleView.Unfold();

        public virtual void OnNewTarget(GameObject target)
        {
        }
    }

    public class GameComponentView<T> : GameComponentView where T : GameComponent
    {
        protected T targetComponent => target.GetComponent<T>();

        public override bool CheckTargetValid()
        {
            return target && target.GetComponent<T>() != null;
        }
    }
}
