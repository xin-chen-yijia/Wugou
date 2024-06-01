using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Wugou;
using System;

namespace Wugou.UI
{
    /// <summary>
    /// UI结构设计为一颗树，RootWindow为树根，每一个树节点相当于一个子窗口
    /// </summary>
    public class UIRootWindow : UIBaseWindow
    {
        private static UIRootWindow _additionalWindow;
        public static UIRootWindow additionalWindow
        {
            get
            {
                if(_additionalWindow == null)
                {
                    GameObject pfb = Resources.Load<GameObject>("UI/Additional Window");
                    var obj = Instantiate<GameObject>(pfb);
                    _additionalWindow = obj.GetComponent<UIRootWindow>();
                    GameObject.DontDestroyOnLoad(obj);

                    var eventSys = GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
                    if (!eventSys)
                    {
                        obj.transform.Find("EventSystem").gameObject.SetActive(true);
                    }

                    SceneManager.sceneLoaded += OnSceneLoaded;  // 没有清理，暂时也不清理

                }

                return _additionalWindow;
            }

            set
            {
                _additionalWindow = value;
            }
        }

        private static Dictionary<Type, UIBaseWindow> pages_ = new Dictionary<Type, UIBaseWindow>();

        /// <summary>
        /// 用于从Resources文件夹中动态创建目标UI
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static UIBaseWindow GetOrAddWindow(Type type)
        {
            if(type == null)
            {
                return null;
            }

            if (pages_.ContainsKey(type))
            {
                if(pages_[type] != null)
                {
                    return pages_[type];
                }
                else
                {
                    Logger.Error($"{type} is null, maybe destroyed...");
                    pages_.Remove(type);
                }
            }

            var page = additionalWindow.GetComponentInChildren(type, true) as UIBaseWindow;
            if (!page)
            {
                page = additionalWindow.AddChildWindow(type);
            }

            pages_[type] = page;

            return page;
        }

        /// <summary>
        /// 获取指定UI，没有则根据类型，查找资源创建
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetOrAddWindow<T>() where T : UIBaseWindow
        {
            var type = typeof(T);
            var page = GetOrAddWindow(type);
            return page as T;
        }

        /// <summary>
        /// 获取指定UI
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetWindow<T>() where T: UIBaseWindow
        {
            var type = typeof(T);
            if (pages_.ContainsKey(type))
            {
                return pages_[type] as T;
            }

            return null;
        }

        /// <summary>
        /// 删除UI
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void DeleteWindow<T>() where T : UIBaseWindow
        {
            var type = typeof(T);
            if (pages_.ContainsKey(type))
            {
                GameObject.Destroy(pages_[type]);
                pages_.Remove(type);
            }
        }

        public static void Release()
        {
            if(additionalWindow != null)
            {
                GameObject.Destroy(additionalWindow.gameObject);
            }

            pages_.Clear();
        }

        /// <summary>
        /// 应对切换场景后没有EventSystem的时候
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="mode"></param>
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_additionalWindow)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                return;
            }

            var obj = _additionalWindow.transform.Find("EventSystem").gameObject;
            var eventSys = GameObject.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSys.Length == 0 && _additionalWindow)
            {
                obj.SetActive(true);
            }

            if(eventSys.Length > 1 && obj.activeInHierarchy)
            {
                obj.SetActive(false);
            }

        }
    }
}
