using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 描边, 使用cakeslice的插件
    /// </summary>
    public static class OutlineEffect
    {
        #region cakeslice
        //private static Camera targetCamera;

        ///// <summary>
        ///// 需要摄像机添加相应的组件
        ///// </summary>
        ///// <param name="camera"></param>
        //public static void Apply(Camera camera)
        //{
        //    if (targetCamera == camera) 
        //    {
        //        return;
        //    }

        //    if (targetCamera)
        //    {
        //        Release();
        //    }

        //    targetCamera = camera;
        //    var outlineEffectInstance = camera.GetComponent<cakeslice.OutlineEffect>();
        //    if (!outlineEffectInstance)
        //    {
        //        var comp = camera.gameObject.AddComponent<cakeslice.OutlineEffect>();
        //        comp.lineThickness = 1;
        //        comp.lineIntensity = 1.51f;
        //        comp.fillAmount = 0.1f;
        //        ColorUtility.TryParseHtmlString("#FFC300", out comp.lineColor0);
        //        ColorUtility.TryParseHtmlString("#BC5BB9", out comp.lineColor1);
        //        ColorUtility.TryParseHtmlString("#0096FF", out comp.lineColor2);
        //    }
        //}

        ///// <summary>
        ///// 释放OutlineEffect的相关资源
        ///// </summary>
        //public static void Release()
        //{
        //    if (targetCamera)
        //    {
        //        GameObject.DestroyImmediate(targetCamera.GetComponent<cakeslice.OutlineEffect>());
        //        targetCamera = null;
        //    }
        //}

        //public static bool enable{ 
        //    get { return cakeslice.OutlineEffect.Instance && cakeslice.OutlineEffect.Instance.enabled; } 
        //    set 
        //    {
        //        if (cakeslice.OutlineEffect.Instance)
        //        {
        //            cakeslice.OutlineEffect.Instance.enabled = value;
        //        }
        //    } 
        //}

        ///// <summary>
        ///// 给指定物体添加描边
        ///// </summary>
        ///// <param name="go"></param>
        //public static void AddOrEnableOutline(GameObject go)
        //{
        //    Debug.Assert(enable);
        //    foreach (var v in go.GetComponentsInChildren<Renderer>())
        //    {
        //        var outlineComp = v.GetComponent<cakeslice.Outline>();
        //        if (outlineComp)
        //        {
        //            outlineComp.enabled = true;
        //        }
        //        else
        //        {
        //            if (!v.GetComponent<IgnoreOutline>())
        //            {
        //                outlineComp = v.gameObject.AddComponent<cakeslice.Outline>();
        //                outlineComp.color = 0;
        //            }

        //        }
        //    }
        //}

        ///// <summary>
        ///// 取消描边
        ///// </summary>
        ///// <param name="go"></param>
        //public static void RemoveOutline(GameObject go)
        //{
        //    Debug.Assert(enable);
        //    if (go)
        //    {
        //        foreach (var v in go.GetComponentsInChildren<cakeslice.Outline>())
        //        {
        //            v.enabled = false;
        //        }
        //    }
        //}
        #endregion

        private static HighlightPlus.HighlightManager outlineManger_;

        /// <summary>
        /// 需要摄像机添加相应的组件
        /// </summary>
        /// <param name="camera"></param>
        public static void Apply(Camera camera)
        {
            if (outlineManger_ == null)
            {
                var prefab = Resources.Load<GameObject>("OutlineManager");
                var go = GameObject.Instantiate(prefab);
                outlineManger_ = go.GetComponent<HighlightPlus.HighlightManager>();
            }

            outlineManger_.raycastCamera = camera;
        }

        /// <summary>
        /// 释放OutlineEffect的相关资源
        /// </summary>
        public static void Release()
        {
            Debug.Assert(outlineManger_ != null);
            GameObject.DestroyImmediate(outlineManger_.gameObject);
        }

        public static bool enable
        {
            get { return outlineManger_ && outlineManger_.enabled; }
            set
            {
                if (outlineManger_)
                {
                    outlineManger_.enabled = value;
                }
            }
        }

        /// <summary>
        /// 给指定物体添加描边
        /// </summary>
        /// <param name="go"></param>
        public static void AddOutline(GameObject go)
        {
            Debug.Assert(enable);
            outlineManger_.SelectObject(go.transform);
        }

        /// <summary>
        /// 取消描边
        /// </summary>
        /// <param name="go"></param>
        public static void RemoveOutline(GameObject go)
        {
            Debug.Assert(enable);
            outlineManger_.UnselectObject(go.transform);
        }
    }
}
