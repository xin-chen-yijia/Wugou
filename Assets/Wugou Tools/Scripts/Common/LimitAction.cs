using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 受限的运动
    /// </summary>
    public class LimitAction
    {
        public float max;
        public float min;

        private float value_ = 0;   // 当前值

        private System.Action<float> action_;

        public LimitAction(float min, float max, System.Action<float> action)
        {
            this.max = max;
            this.min = min;
            this.action_ = action;
        }

        /// <summary>
        /// 步进
        /// </summary>
        /// <param name="delta"></param>
        /// <returns>走到最大值返回1，走到最小值返回-1，中间为0</returns>
        public int Step(float delta)
        {
            if (value_ + delta < min) // 最小值
            {
                action_(min - value_);  // 浮点数可能会有一点点的精度误差，累计很长时间后可能会有一点影响
                value_ = min;

                return -1;
            }
            else if (value_ + delta > max)
            {
                action_(max - value_);
                value_ = max;

                return 1;
            }
            else
            {
                action_(delta);
                value_ += delta;

                return 0;
            }
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        public void Reset()
        {
            action_(-value_);
            value_ = 0;
        }
    }
}
