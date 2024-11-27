using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 查看模型，单独使用相机渲染某一个模型到纹理上，常用场景就是：游戏角色展示
    /// </summary>
    public class ModelCloseUpBooth
    {
        public static int boothLayer { get; set; } = 2; // 摄像只看这一层的对象

        private static Camera boothCamera;
        public static RenderTexture texture { get; private set; }

        private static GameObject rootObject;

        public static int textureWidth = 576;
        public static int textureHeight = 1024;

        public static GameObject currentTarget { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset">in camera coordinate</param>
        public static void SetTargetOffset(Vector3 offset)
        {
            currentTarget.transform.position = boothCamera.transform.TransformPoint(offset);
        }

        public static void Show(GameObject target)
        {
            if(rootObject== null)
            {
                var pfb = Resources.Load<GameObject>("ModelBoothRoot");
                rootObject = GameObject.Instantiate<GameObject>(pfb);
                rootObject.transform.position = new Vector3(0, 10000, 0);

                boothCamera = rootObject.GetComponentInChildren<Camera>();
                // 摄像机只看boothlayer的东西
                boothCamera.cullingMask = 1 << boothLayer;

                foreach(var light in rootObject.GetComponentsInChildren<Light>())
                {
                    light.cullingMask = 1 << boothLayer;
                }
            }

            // 9:16
            if (texture == null)
            {
                texture = new RenderTexture(textureWidth, textureHeight, 16);
            }

            boothCamera.targetTexture = texture;

            if (currentTarget)
            {
                currentTarget.SetActive(false);
            }
            currentTarget = target;
            target.transform.position = boothCamera.transform.position + boothCamera.transform.forward * 2;
            Utils.SetLayerRecursively(target, boothLayer);
            // 归到一起，好删除
            target.transform.SetParent(rootObject.transform);
        }

        public static void Destroy()
        {
            if (rootObject)
            {
                GameObject.Destroy(rootObject);
                rootObject = null;
            }   

            if(texture != null)
            {
                texture.Release();
                texture = null;
            }
        }

        //// Start is called before the first frame update
        //void Start()
        //{

        //}

        //// Update is called once per frame
        //void Update()
        //{

        //}
    }
}
