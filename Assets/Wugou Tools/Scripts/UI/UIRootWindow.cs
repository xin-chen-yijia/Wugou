using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Wugou;
using UnityEngine.Assertions.Must;
using System;

namespace Wugou.UI
{
    /// <summary>
    /// UI结构设计为一颗树，RootWindow为树根，每一个树节点相当于一个子窗口
    /// </summary>
    public class UIRootWindow : UIBaseWindow
    {
        /// <summary>
        /// UI 资源来自于Assetbundle
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="rootObjName"></param>
        /// <param name="dontDestroyOnload"></param>
        /// <returns></returns>
        public static async Task<UIRootWindow> Create(AssetBundleAssetLoader loader, string rootObjName = "Canvas",bool dontDestroyOnload=false)
        {
            var uiRootPrefab = await loader.LoadAssetAsync<GameObject>(rootObjName);
            Debug.Assert(uiRootPrefab);
            GameObject uiParent = GameObject.Instantiate<GameObject>(uiRootPrefab);
            uiParent.transform.position = Vector3.zero;
            uiParent.name = rootObjName;
            UIRootWindow tmp = uiParent.AddComponent<UIRootWindow>();

            if (dontDestroyOnload)
            {
                GameObject.DontDestroyOnLoad(tmp.gameObject);
            }

            if (!GameObject.Find("EventSystem"))
            {
                Debug.LogError("Can't find EventSystem. UI maybe can't interact");
            }

            return tmp;
        }

        /// <summary>
        /// UI 资源来自于Assetbundle
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="rootObjName"></param>
        /// <param name="dontDestroyOnload"></param>
        /// <returns></returns>
        public static void Create(CoroutineAssetBundleAssetLoader loader, string rootObjName = "Canvas",  bool dontDestroyOnload = false, Action<UIRootWindow> onLoaded = null)
        {
            loader.LoadAssetAsync<GameObject>(rootObjName,(GameObject obj) =>
            {
                Debug.Assert(obj);
                GameObject uiParent = GameObject.Instantiate<GameObject>(obj);
                uiParent.transform.position = Vector3.zero;
                uiParent.name = rootObjName;
                UIRootWindow tmp = new UIRootWindow();

                if (dontDestroyOnload)
                {
                    GameObject.DontDestroyOnLoad(tmp.gameObject);
                }

                if (!GameObject.Find("EventSystem"))
                {
                    Debug.LogError("Can't find EventSystem. UI maybe can't interact");
                }

                onLoaded?.Invoke(tmp);
            });
        }

        /// <summary>
        /// 直接使用场景中的Canvas创建UI Manager
        /// </summary>
        /// <param name="rootObj"></param>
        /// <param name="dontDestroyOnload"></param>
        /// <returns></returns>
        public static UIRootWindow Create(GameObject rootObj, bool dontDestroyOnload = false)
        {
            if(rootObj == null)
            {
                Debug.Log("UIManager create fail with null object...");
                return null;
            }

            UIRootWindow tmp = rootObj.AddComponent<UIRootWindow>();

            if (dontDestroyOnload)
            {
                GameObject.DontDestroyOnLoad(tmp.gameObject);
            }

            if (!GameObject.Find("EventSystem"))
            {
                Debug.LogError("Can't find EventSystem. UI maybe can't interact");
            }

            return tmp;
        }
    }
}
