using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 搞一张摄像机的快照，通常用于UI显示
    /// </summary>
    public static class CameraSnaphot
    {
        /// <summary>
        /// 为目标相机创建一张快照
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static RenderTexture CreateSnapshot(this Camera camera, int width, int height)
        {
            RenderTexture rt = new RenderTexture(width,height,0);

            var cachedRt = camera.targetTexture;
            camera.targetTexture = rt;
            camera.Render();
            camera.targetTexture = cachedRt;

            return rt;
        }

        /// <summary>
        /// 为目标相机创建一张快照，使用RenderWithShader
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="shader"></param>
        /// <param name="replacementTag"></param>
        /// <returns></returns>
        public static RenderTexture CreateSnapshot(this Camera camera, int width, int height, Shader shader, string replacementTag)
        {
            RenderTexture rt = new RenderTexture(width, height, 0);

            var cachedRt = camera.targetTexture;
            camera.targetTexture = rt;
            camera.RenderWithShader(shader, replacementTag);
            camera.targetTexture = cachedRt;

            return rt;
        }

        /// <summary>
        /// 在指定的位置创建一个快照
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="position"></param>
        /// <param name="quaternion"></param>
        /// <returns></returns>
        public static RenderTexture CreateSnapshot(this Camera camera, int width, int height, Vector3 position, Quaternion quaternion)
        {
            RenderTexture rt = new RenderTexture(width, height, 0);

            // cache target texture,postion,rotation
            var cachedRt = camera.targetTexture;
            camera.targetTexture = rt;

            var cachedPos = camera.transform.position;
            var cachedRot = camera.transform.rotation;

            camera.transform.position = position;
            camera.transform.localRotation = quaternion;

            camera.Render();

            // restore
            camera.transform.position = cachedPos;
            camera.transform.rotation = cachedRot;
            camera.targetTexture = cachedRt;

            return rt;
        }
    }
}
