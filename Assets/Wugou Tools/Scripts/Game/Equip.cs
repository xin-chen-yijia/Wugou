using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 装备基类
    /// </summary>
    public abstract class Equip : MonoBehaviour
    {
        protected GameObject player_;
        /// <summary>
        /// 持有该装备时选择物体的处理
        /// </summary>
        /// <param name="entity"></param>
        public virtual void OnSelectEntity(GameEntity entity)
        {
            
        }

        /// <summary>
        /// 刚拿起时调用
        /// </summary>
        public virtual bool PickUp(GameObject player)
        {
            player_ = player;
            return true;
        }

        /// <summary>
        /// 被丢弃时调用
        /// </summary>
        public virtual bool Drop()
        {
            if(isRunning)
            {
                Wugou.Logger.Warning("Equip is running while it be drop...");
            }

            return true;
        }

        /// <summary>
        /// 是否正在使用
        /// </summary>
        public bool isRunning { get; protected set; }

        /// <summary>
        /// 进入使用状态
        /// </summary>
        public virtual void EnterRunning()
        {
            isRunning = true;
        }

        /// <summary>
        /// 退出使用状态
        /// </summary>
        public virtual void ExitRunning()
        {
            isRunning = false;
        }
    }
}
