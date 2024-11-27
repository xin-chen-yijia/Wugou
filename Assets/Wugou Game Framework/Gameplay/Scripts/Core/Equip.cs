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
        public GameObject owner { get; private set; }

        /// <summary>
        /// 用于UI显示使用说明
        /// </summary>
        public virtual string tipText { get; }

        /// <summary>
        /// 当被添加到玩家的背包中时调用
        /// </summary>
        public virtual void OnPickUp(GameObject player)
        {
            this.owner = player;

            transform.SetParent(player.transform);
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// 丢弃
        /// </summary>
        public virtual void OnDrop()
        {
            this.owner = null;
        }

        /// <summary>
        /// 切换到这个武器时调用
        /// </summary>
        public virtual void OnHoldOnEquip()
        {
        }

        public virtual bool CanPutdown()
        {
            if (isRunning)
            {
                Wugou.Logger.Warning("Equip is running while it be drop...");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// 切换到其它武器时调用
        /// </summary>
        public virtual void OnPutdownEquip()
        {

        }

        /// <summary>
        /// 前提条件，在使用前判断其能否使用的前提条件
        /// </summary>
        /// <returns></returns>
        public virtual bool CheckPrecondition()
        {
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

        /// <summary>
        /// 鼠标点击选择GameObject时调用
        /// </summary>
        /// <param name="go"></param>
        public virtual void OnPointerClick(GameObject go)
        {

        }
    }
}
