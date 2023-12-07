using Wugou;
using Wugou.MapEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 继承MonoBehaviour是不想自己再实现这一套，因为之前是基于Unity的这一套做的
    /// 关于序列化：
    /// 1. 属性序列化需要加上SerializeField特性；
    /// 2. public 字段加NonSerialized则不序列化；
    /// </summary>
    public class GameComponent : MonoBehaviour
    {
        #region 自定义的生命周期函数
        /// <summary>
        /// 初始化调用
        /// </summary>
        public virtual void BeginPlay()
        {

        }

        /// <summary>
        /// 每一帧调用
        /// </summary>
        public virtual void Tick()
        {

        }

        /// <summary>
        /// 被删除时调用
        /// </summary>
        public virtual void EndPlay()
        {

        }
        #endregion

        /// <summary>
        /// 不用start，因为start的调用在使用instantiate时是下一帧，这对于代码逻辑有很大的影响
        /// </summary>
        public virtual void Start()
        {
            if (GamePlay.isGaming)
            {
                BeginPlay();
            }
            //else
            //{
            //    // 避免刚体起作用
            //    foreach (var v in GetComponentsInChildren<Rigidbody>())
            //    {
            //        v.isKinematic = true;
            //    }
            //}
        }

        public virtual void Update()
        {
            if(GamePlay.isGaming)
            {
                Tick();
            }
        }

        public virtual void OnDestroy()
        {
            if (GamePlay.isGaming)
            {
                EndPlay();
            }
        }
    }

}
