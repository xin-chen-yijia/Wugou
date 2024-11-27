using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 贝塞尔曲线
    /// </summary>
    public class BezierUtils
    {
        public static Vector3 CalculateLineBezierPoint(float t, Vector3 p0, Vector3 p1)
        {
            float u = 1 - t;
            Vector3 p = u * p0;
            p += t * p1;
            return p;
        }

        public static Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            Vector3 p = uu * p0;
            p += 2 * u * t * p1;
            p += tt * p2;
            return p;
        }

        public static Vector3 CalculateThreePowerBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float ttt = tt * t;
            float uuu = uu * u;
            Vector3 p = uuu * p0;
            p += 3 * t * uu * p1;
            p += 3 * tt * u * p2;
            p += ttt * p3;
            return p;
        }

        /// <summary>
        /// B(t)=-3P0 * (1-t) * (1-t) + 3P1((1-t)*(1-t) - 2 * t * (1-t))+ 3*P2*(2*t*(1-t)-t*t)+3*P3*t*t
        /// 参考：https://blog.csdn.net/u011643833/article/details/78540554
        /// </summary>
        /// <param name="t"></param>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        public static Vector3 CalculateThreePowerBezierTangent(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float uu = u * u;
            float tu = t * u;
            float tt = t * t;

            Vector3 P = p0 * 3 * uu * (-1.0f);
            P += p1 * 3 * (uu - 2 * tu); 
            P += p2 * 3 * (2 * tu - tt);
            P += p3 * 3 * tt;

            //返回单位向量
            return P.normalized;
        }

        public static Vector3[] GetLineBeizerList(Vector3 startPoint, Vector3 endPoint, int segmentNum)
        {
            Vector3[] path = new Vector3[segmentNum + 1];
            for (int i = 0; i <= segmentNum; i++)
            {
                float t = i / (float)segmentNum;
                Vector3 pixel = CalculateLineBezierPoint(t, startPoint, endPoint);
                path[i] = pixel;
            }
            return path;
        }

        public static Vector3[] GetCubicBeizerList(Vector3 startPoint, Vector3 controlPoint, Vector3 endPoint, int segmentNum)
        {
            Vector3[] path = new Vector3[segmentNum + 1];
            for (int i = 0; i <= segmentNum; i++)
            {
                float t = i / (float)segmentNum;
                Vector3 pixel = CalculateCubicBezierPoint(t, startPoint,
                    controlPoint, endPoint);
                path[i] = pixel;
            }
            return path;
        }

        public static Vector3[] GetThreePowerBeizerList(Vector3 startPoint, Vector3 controlPoint1, Vector3 controlPoint2, Vector3 endPoint, int segmentNum)
        {
            Vector3[] path = new Vector3[segmentNum + 1];
            for (int i = 0; i <= segmentNum; i++)
            {
                float t = i / (float)segmentNum;
                Vector3 pixel = CalculateThreePowerBezierPoint(t, startPoint,
                    controlPoint1, controlPoint2, endPoint);
                path[i] = pixel;
            }
            return path;
        }
    }

    /// <summary>
    /// Simpson's 3/8 rule 参考：https://zhuanlan.zhihu.com/p/130247362?utm_medium=social&utm_oi=986551514533609472&utm_id=0
    /// Simpson八分之三法则是一种数值积分方法，常用于数学领域中的函数逼近和曲线拟合等问题，它通过使用多个小区间内的函数值来估计整个区间上的积分值，从而达到提高计算精度的目的。
    /// </summary>
    struct NumericalIntegration
    {
        static double simpson_3_8(System.Func<double,double> derivative_func, double L, double R)
        {
            double mid_L = (2 * L + R) / 3.0, mid_R = (L + 2 * R) / 3.0;
            return (derivative_func(L) + 
                    3.0 * derivative_func(mid_L) + 
                    3.0 * derivative_func(mid_R) + 
                    derivative_func(R)) * (R - L) / 8.0;
        }

        static double adaptive_simpson_3_8(System.Func<double, double> derivative_func, double L, double R, double eps = 0.0001)
        {
            double mid = (L + R) / 2.0;
            double ST = simpson_3_8(derivative_func, L, R),
                   SL = simpson_3_8(derivative_func, L, mid),
                   SR = simpson_3_8(derivative_func, mid, R);
            double ans = SL + SR - ST;
            if (System.Math.Abs(ans) <= 15.0 * eps) return SL + SR + ans / 15.0;
            return adaptive_simpson_3_8(derivative_func, L, mid, eps / 2.0) +
                   adaptive_simpson_3_8(derivative_func, mid, R, eps / 2.0);
        }
    };
}