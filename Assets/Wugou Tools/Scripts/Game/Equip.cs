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
        public GameObject player { get; private set; }

        /// <summary>
        /// 当被添加到玩家的背包中时调用
        /// </summary>
        public virtual void OnAddToPackage(GameObject player)
        {
            this.player = player;

            transform.SetParent(player.transform);
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// 刚拿起时调用
        /// </summary>
        public virtual bool PickUp(GameObject player)
        {
            gameObject.SetActive(true);
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

            gameObject.SetActive(false);
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
