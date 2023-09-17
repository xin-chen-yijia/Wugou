using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    public class MathUtils
    {
        /// <summary>
        /// ��Ԫһ�η������
        /// </summary>
        /// <param name="a1"></param>
        /// <param name="b1"></param>
        /// <param name="c1"></param>
        /// <param name="a2"></param>
        /// <param name="b2"></param>
        /// <param name="c2"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool BinaryLinearEquation(float a1,float b1, float c1,
            float a2, float b2, float c2,
            out float x , out float y)
        {
            if(Mathf.Abs(a1 * b2 - a2 * b1) < 0.000001f)
            {
                x = 0; y=0;
                return false;
            }

            x = (b1 * c2 - b2 * c1) / (a1 * b2 - a2 * b1);
            y = -(a1 * c2 - a2 * c1) / (a1 * b2 - a2 * b1);

            Debug.Assert(Mathf.Abs(a1 *x +b1 *y +c1 - a2 * x + b2 * y + c2) < 0.000001f);   // float���㻹�ǲ���ȷ

            return true;
        }

        /// <summary>
        /// ��ֱ����ƽ��Ľ��� 
        /// </summary>
        /// <param name="rayOrigin"></param>
        /// <param name="rayDir"></param>
        /// <param name="planeNormal"></param>
        /// <param name="distanceToPlane"></param>
        /// <returns></returns>
        public static (Vector3,float) IntersectionPoints(Vector3 rayOrigin, Vector3 rayDir, Vector3 planeNormal, float distanceToPlane)
        {
            // ƽ�湫ʽ��p * n = d
            // ֱ�߹�ʽ��p(t) = p0 + t * d
            // t = (d-p0 * n) / d * n

            float t = (distanceToPlane - Vector3.Dot(rayOrigin, planeNormal)) / Vector3.Dot(rayDir,planeNormal);
            Vector3 point = rayOrigin + t * rayDir;

            return (point, t);
        }

        // ��ScreenPointToRay�����̫һ��  ==============================
        //float nearPlanHeight = Mathf.Tan(editorCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * editorCamera.nearClipPlane * 2;
        //float nearPlanWidth = nearPlanHeight * editorCamera.aspect;
        //Vector3 nearPlanePoint0 = new Vector3((Input.mousePosition.x / Screen.width - 0.5f) * nearPlanWidth, (Input.mousePosition.y / Screen.height - 0.5f) * nearPlanHeight, editorCamera.nearClipPlane);
        //nearPlanePoint0 = editorCamera.transform.TransformPoint(nearPlanePoint0);
    }
}
