using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Wugou.UI
{

    public class UIBaseWindow : MonoBehaviour
    {
        /// <summary>
        /// 根窗口
        /// </summary>
        public UIRootWindow rootWindow => transform.GetComponentInParent<UIRootWindow>();

        public virtual bool isShow
        {
            get
            {
                return gameObject.activeSelf;
            }
        }

        /// <summary>
        /// 显示
        /// </summary>
        /// <param name="asTop">显示在最前面</param>
        public virtual void Show(bool asTop = false)
        {
            gameObject.SetActive(true);
            if (asTop)
            {
                transform.SetAsLastSibling();
            }
        }

        /// <summary>
        /// 隐藏
        /// </summary>
        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 根据类型名称，找Canvas下同名的物体，并把脚本添加该物体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objName">子物体名字</param>
        /// <returns></returns>
        public T GetChildWindow<T>(string objName = "", bool includeinactive = true) where T : UIBaseWindow
        {
            if (string.IsNullOrEmpty(objName))
            {
                return transform.GetComponentInChildren<T>(includeinactive);
            }

            var go = transform.Find(objName);
            if (go)
            {
                return go.GetComponent<T>();
            }

            return null;

        }

        /// <summary>
        /// Resources中存放ui的目录
        /// </summary>
        public static string path = "UI";

        /// <summary>
        /// 从Resouces文件夹中创建新的window
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public UIBaseWindow AddChildWindow(Type type)
        {
            GameObject pagePfb = Resources.Load<GameObject>($"{path}/{type.Name}");
            Debug.Assert(pagePfb);
            GameObject pageObj = Instantiate<GameObject>(pagePfb,transform);
            var page = pageObj.GetComponent(type);
            if (!page)
            {
                page = pageObj.AddComponent(type);
            }
            return page as UIBaseWindow;
        }

        /// <summary>
        /// 在当前窗口下添加新窗口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        public T AddChildWindow<T>(GameObject obj) where T : UIBaseWindow
        {
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;

            if (!obj.GetComponent<T>())
            {
                obj.AddComponent<T>();
            }

            return obj.GetComponent<T>();
        }
    }

}

