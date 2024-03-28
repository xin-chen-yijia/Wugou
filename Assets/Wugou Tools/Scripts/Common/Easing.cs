using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// »º¶¯º¯Êý
    /// ²Î¿¼ https://blog.csdn.net/z2014z/article/details/120691794
    /// </summary>
    public static class Easing
    {
        public static float EaseLinear(float t, float b, float c, float d)
        {
            return c * t / d + b;
        }

        public static float EaseOutQuad(float t, float b, float c, float d)
        {
            return -c * (t /= d) * (t - 2) + b;
        }

        public static float EaseInOutQuad(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1)
            {
                return c / 2 * t * t + b;
            }
            return -c / 2 * ((--t) * (t - 2) - 1) + b;
        }
    }
}

