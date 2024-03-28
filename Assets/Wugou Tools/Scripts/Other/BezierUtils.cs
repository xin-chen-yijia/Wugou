using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
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

        public static Vector3[] GetLineBeizerList(Vector3 startPoint, Vector3 endPoint, int segmentNum)
        {
            Vector3[] path = new Vector3[segmentNum];
            for (int i = 1; i <= segmentNum; i++)
            {
                float t = i / (float)segmentNum;
                Vector3 pixel = CalculateLineBezierPoint(t, startPoint, endPoint);
                path[i - 1] = pixel;
                Debug.Log(path[i - 1]);
            }
            return path;
        }

        public static Vector3[] GetCubicBeizerList(Vector3 startPoint, Vector3 controlPoint, Vector3 endPoint, int segmentNum)
        {
            Vector3[] path = new Vector3[segmentNum];
            for (int i = 1; i <= segmentNum; i++)
            {
                float t = i / (float)segmentNum;
                Vector3 pixel = CalculateCubicBezierPoint(t, startPoint,
                    controlPoint, endPoint);
                path[i - 1] = pixel;
                Debug.Log(path[i - 1]);
            }
            return path;
        }

        public static Vector3[] GetThreePowerBeizerList(Vector3 startPoint, Vector3 controlPoint1, Vector3 controlPoint2, Vector3 endPoint, int segmentNum)
        {
            Vector3[] path = new Vector3[segmentNum];
            for (int i = 1; i <= segmentNum; i++)
            {
                float t = i / (float)segmentNum;
                Vector3 pixel = CalculateThreePowerBezierPoint(t, startPoint,
                    controlPoint1, controlPoint2, endPoint);
                path[i - 1] = pixel;
                Debug.Log(path[i - 1]);
            }
            return path;
        }
    }
}