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

        const float PI = Mathf.PI;
        const float c1 = 1.70158f;
        const float c2 = c1 * 1.525f;
        const float c3 = c1 + 1;
        const float c4 = (2 * PI) / 3;
        const float c5 = (2 * PI) / 4.5f;

        public static float bounceOut(float x)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (x < 1 / d1)
            {
                return n1 * x * x;
            }
            else if (x < 2 / d1)
            {
                return n1 * (x -= 1.5f / d1) * x + 0.75f;
            }
            else if (x < 2.5 / d1)
            {
                return n1 * (x -= 2.25f / d1) * x + 0.9375f;
            }
            else
            {
                return n1 * (x -= 2.625f / d1) * x + 0.984375f;
            }
        }

        public static float easeInQuad(float x)
        {
            return x * x;
        }
        public static float easeOutQuad(float x)
        {
            return 1 - (1 - x) * (1 - x);
        }
        public static float easeInOutQuad(float x)
        {

            return x < 0.5 ? 2 * x * x : 1 - Mathf.Pow(-2 * x + 2, 2) / 2;
        }
        public static float easeInCubic(float x)
        {
            return x * x * x;
        }
        public static float easeOutCubic(float x)
        {
            return 1 - Mathf.Pow(1 - x, 3);
        }
        public static float easeInOutCubic(float x)
        {
            return x < 0.5f ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2;
        }
        public static float easeInQuart(float x)
        {
            return x * x * x * x;
        }
        public static float easeOutQuart(float x)
        {
            return 1 - Mathf.Pow(1 - x, 4);
        }
        public static float easeInOutQuart(float x)
        {
            return x < 0.5 ? 8 * x * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 4) / 2;
        }
        public static float easeInQuint(float x)
        {
            return x * x * x * x * x;
        }
        public static float easeOutQuint(float x)
        {
            return 1 - Mathf.Pow(1 - x, 5);
        }
        public static float easeInOutQuint(float x)
        {
            return x < 0.5 ? 16 * x * x * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 5) / 2;
        }
        public static float easeInSine(float x)
        {
            return 1 - Mathf.Cos((x * PI) / 2);
        }
        public static float easeOutSine(float x)
        {
            return Mathf.Sin((x * PI) / 2);
        }
        public static float easeInOutSine(float x)
        {
            return -(Mathf.Cos(PI * x) - 1) / 2;
        }
        public static float easeInExpo(float x)
        {
            return x == 0 ? 0 : Mathf.Pow(2, 10 * x - 10);
        }
        public static float easeOutExpo(float x)
        {
            return x == 1 ? 1 : 1 - Mathf.Pow(2, -10 * x);
        }
        public static float easeInOutExpo(float x)
        {
            return x == 0
                ? 0
                : x == 1
                ? 1
                : x < 0.5
                ? Mathf.Pow(2, 20 * x - 10) / 2
                : (2 - Mathf.Pow(2, -20 * x + 10)) / 2;
        }
        public static float easeInCirc(float x)
        {
            return 1 - Mathf.Sqrt(1 - Mathf.Pow(x, 2));
        }
        public static float easeOutCirc(float x)
        {
            return Mathf.Sqrt(1 - Mathf.Pow(x - 1, 2));
        }
        public static float easeInOutCirc(float x)
        {
            return x < 0.5
                ? (1 - Mathf.Sqrt(1 - Mathf.Pow(2 * x, 2))) / 2
                : (Mathf.Sqrt(1 - Mathf.Pow(-2 * x + 2, 2)) + 1) / 2;
        }
        public static float easeInBack(float x)
        {
            return c3 * x * x * x - c1 * x * x;
        }
        public static float easeOutBack(float x)
        {
            return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
        }
        public static float easeInOutBack(float x)
        {
            return x < 0.5
                ? (Mathf.Pow(2 * x, 2) * ((c2 + 1) * 2 * x - c2)) / 2
                : (Mathf.Pow(2 * x - 2, 2) * ((c2 + 1) * (x * 2 - 2) + c2) + 2) / 2;
        }
        public static float easeInElastic(float x)
        {
            return x == 0
                ? 0
                : x == 1
                ? 1
                : -Mathf.Pow(2, 10 * x - 10) * Mathf.Sin((x * 10 - 10.75f) * c4);
        }
        public static float easeOutElastic(float x)
        {
            return x == 0
                ? 0
                : x == 1
                ? 1
                : Mathf.Pow(2, -10 * x) * Mathf.Sin((x * 10 - 0.75f) * c4) + 1;
        }
        public static float easeInOutElastic(float x)
        {
            return x == 0
                ? 0
                : x == 1
                ? 1
                : x < 0.5
                ? -(Mathf.Pow(2, 20 * x - 10) * Mathf.Sin((20 * x - 11.125f) * c5)) / 2
                : (Mathf.Pow(2, -20 * x + 10) * Mathf.Sin((20 * x - 11.125f) * c5)) / 2 + 1;
        }
        public static float easeInBounce(float x)
        {
            return 1 - bounceOut(1 - x);
        }

        public static float easeInOutBounce(float x)
        {
            return x < 0.5
                ? (1 - bounceOut(1 - 2 * x)) / 2
                : (1 + bounceOut(2 * x - 1)) / 2;
        }
    }
}

