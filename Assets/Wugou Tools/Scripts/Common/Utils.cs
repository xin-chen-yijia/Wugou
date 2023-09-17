using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Wugou
{
    public static class Utils
    {

        public static Transform FindRecursive(this Transform transform,string child)
        {
            if(transform.name == child)
            {
                return transform;
            }
            foreach(Transform t in transform)
            {
                Transform res = FindRecursive(t,child);
                if(res)
                {
                    return res; 
                }
            }

            return null;
        }

        public static void DontDestroyRootOnLoad(GameObject obj)
        {
            Transform t = obj.transform;
            while(t.parent)
            {
                t = t.parent;
            }

            GameObject.DontDestroyOnLoad(t.gameObject);
        }

        public static void SetLayerRecursively(this GameObject obj,int layer)
        {
            if(obj == null)
            {
                return;
            }
            obj.layer = layer;
            foreach(Transform t in obj.transform)
            {
                SetLayerRecursively(t.gameObject,layer);
            }
        }

        public static void SetColliderEnableRecursively(this GameObject obj, bool enable)
        {
            if (obj == null)
            {
                return;
            }
            if(obj.GetComponent<Collider>())
            {
                obj.GetComponent<Collider>().enabled = enable;
            }
            
            foreach (Transform t in obj.transform)
            {
                SetColliderEnableRecursively(t.gameObject, enable);
            }
        }

        public static Texture2D ConvertFromBase64(string base64)
        {
            //ThreadPool.SetMaxThreads(4, 4);
            //ThreadPool.QueueUserWorkItem(new WaitCallback((object obj) =>
            //{
            //    try
            //    {
            //        //Texture2D tex = obj as Texture2D;
            //        byte[] bytes = Convert.FromBase64String(base64);
            //        texture.LoadImage(bytes);
            //    }
            //    catch(Exception e)
            //    {
            //        Debug.Log(e.Message);
            //    }
            //}),null);
            try
            {
                byte[] bytes = Convert.FromBase64String(base64);

                //MemoryStream memStream = new MemoryStream(bytes);
                //BinaryFormatter binFormatter = new BinaryFormatter();
                //Image img = (Image)binFormatter.Deserialize(memStream);

                //bitmap = new System.Drawing.Bitmap(ms);//将MemoryStream对象转换成Bitmap对象
                
                Texture2D tex = new Texture2D(640, 360);
                //tex.LoadRawTextureData(bytes);
                tex.LoadImage(bytes);
                return tex;
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return null;
            }
        }

        public static void FillContent<T>(GameObject container, GameObject prefab, List<T> contents, UnityAction<GameObject,T> onInstantiatePrefab, bool needClear = true)
        {
            // clear
            if (needClear)
            {
                foreach (Transform t in container.transform)
                {
                    GameObject.Destroy(t.gameObject);
                }
            }

            float hh = 0.0f;
            System.Action<float> Add = null;
            System.Action Resize = null;

            // resize
            if (container.GetComponent<VerticalLayoutGroup>())
            {
                VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
                Add = (float v) => { hh += v + layout.spacing; };
                Resize = () =>
                {
                    var containerTrans = container.GetComponent<RectTransform>();
                    containerTrans.sizeDelta = new Vector2(containerTrans.sizeDelta.x, hh); // 多算了一个spacing
                };
            }
            else if (container.GetComponent<HorizontalLayoutGroup>())
            {
                HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
                Add = (float v) => { hh += v + layout.spacing; };
                Resize = () =>
                {
                    var containerTrans = container.GetComponent<RectTransform>();
                    containerTrans.sizeDelta = new Vector2(hh, containerTrans.sizeDelta.y); // 多算了一个spacing
                };

            }
            else if (container.GetComponent<GridLayoutGroup>())
            {
                GridLayoutGroup layout = container.GetComponent<GridLayoutGroup>();
                var containerTrans = container.GetComponent<RectTransform>();
                containerTrans.sizeDelta = new Vector2(containerTrans.sizeDelta.x, (layout.cellSize.y + layout.spacing.y) * ((contents.Count + layout.constraintCount - 1) / layout.constraintCount));
            }
            else
            {
                // not resize
            }

            foreach (var map in contents)
            {
                GameObject item = GameObject.Instantiate<GameObject>(prefab, container.transform);
                item.SetActive(true);
                onInstantiatePrefab(item,map);       // 这里可能改变item大小

                Add?.Invoke(item.GetComponent<RectTransform>().sizeDelta.y);
            }

            Resize?.Invoke();
            
            prefab.SetActive(false);
        }

        /// <summary>
        /// 用于滚动区域根据内容数量计算大小
        /// </summary>
        /// <param name="container"></param>
        /// <param name="delay"></param>
        public static void ResizeContainerHeight(GameObject container, float delay = 0.0f)
        {
            if(delay < 0.0001f)
            {
                ResizeContainerHeightInternal(container);
            }
            else
            {
                CoroutineLauncher.active.StartCoroutine(DelayResizeContainerHeight(container, delay));
            }

        }

        private static IEnumerator DelayResizeContainerHeight(GameObject container, float delay)
        {
            yield return new WaitForSeconds(delay);

            ResizeContainerHeightInternal(container);
        }

        private static void ResizeContainerHeightInternal(GameObject container)
        {
            var size = container.GetComponent<RectTransform>().sizeDelta;
            var layout = container.GetComponent<LayoutGroup>();

            var firstChild = container.transform.GetChild(0);
            var lastChild = container.transform.GetChild(container.transform.childCount - 1);
            size.y = layout.padding.top + layout.padding.bottom + (-(lastChild.localPosition.y - firstChild.localPosition.y)) + firstChild.GetComponent<RectTransform>().sizeDelta.y * 0.5f + lastChild.GetComponent<RectTransform>().sizeDelta.y * 0.5f;

            container.GetComponent<RectTransform>().sizeDelta = size;
        }

        // 缓存，避免重复加载
        private static Dictionary<string, Sprite> spritesCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// 清理Sprite的缓存
        /// </summary>
        public static void ReleaseSpriteCache()
        {
            spritesCache.Clear();
        }

        /// <summary>
        /// 使用本地文件生成sprite
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="pivot"></param>
        /// <returns></returns>
        public static Sprite LoadSpriteFromFile(string filePath, Vector2 pivot)
        {
            if (spritesCache.ContainsKey(filePath))
            {
                return spritesCache[filePath];
            }

            //创建文件读取流
            System.IO.FileStream fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            //创建文件长度缓冲区
            byte[] bytes = new byte[fileStream.Length];
            //读取文件
            fileStream.Read(bytes, 0, (int)fileStream.Length);
            //释放文件读取流
            fileStream.Close();
            fileStream.Dispose();
            fileStream = null;

            //创建Texture
            int width = 229;
            int height = 169;
            Texture2D texture = new Texture2D(width, height);
            texture.LoadImage(bytes);

            //创建Sprite
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot);
            spritesCache[filePath] = sprite;
            return sprite;
        }

        /// <summary>
        /// 使用协程加载本地文件为sprite
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="pivot"></param>
        /// <param name="onLoaded"></param>
        public static void LoadSpriteFromFileWithWebRequest(string filePath, Vector2 pivot, UnityAction<Sprite> onLoaded)
        {
            if (spritesCache.ContainsKey(filePath))
            {
                onLoaded?.Invoke(spritesCache[filePath]);
                return;
            }
            CoroutineLauncher.active.StartCoroutine(LoadTexture2D(filePath, pivot, onLoaded));
        }

        private static IEnumerator LoadTexture2D(string path, Vector2 pivot, UnityAction<Sprite> onLoaded)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(path);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var texture = DownloadHandlerTexture.GetContent(request);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                spritesCache[path] = sprite;
                onLoaded?.Invoke(sprite);
            }
            else
            {
                Wugou.Logger.Error($"Load {path} fail.. error:{request.error}");
            }
        }

        /// <summary>
        /// 复制某个组件及其序列化的值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="comp"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static T GetCopyOf<T>(this Component comp, T other) where T : Component
        {
            Type type = comp.GetType();
            if (type != other.GetType()) return null; // type mis-match
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            return comp as T;
        }

        public static Component GetCopyOf(this Component comp, Component other)
        {
            Type type = comp.GetType();
            Type otherType = other.GetType();
            if (type != otherType)
            {
                Debug.LogError($"GetCopyOf {type.Name} and {otherType.Name}..");
                return null; // type mis-match
            }
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            return comp;
        }

        /// <summary>
        /// 复制某个组件及其序列化的值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go"></param>
        /// <param name="toAdd"></param>
        /// <returns></returns>
        public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component
        {
            return go.AddComponent<T>().GetCopyOf(toAdd) as T;
        }

    }
}

