using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Wugou
{
    public static class Utils
    {
        /// <summary>
        /// 双击间隔
        /// </summary>
        public const float doubleClickMaxInterval = 0.2f;


        public static Transform FindRecursive(this Transform transform, string child)
        {
            if (transform.name == child)
            {
                return transform;
            }
            foreach (Transform t in transform)
            {
                Transform res = FindRecursive(t, child);
                if (res)
                {
                    return res;
                }
            }

            return null;
        }

        public static void DontDestroyRootOnLoad(GameObject obj)
        {
            Transform t = obj.transform;
            while (t.parent)
            {
                t = t.parent;
            }

            GameObject.DontDestroyOnLoad(t.gameObject);
        }

        public static void SetLayerRecursively(this GameObject obj, int layer)
        {
            if (obj == null)
            {
                return;
            }
            obj.layer = layer;
            foreach (Transform t in obj.transform)
            {
                SetLayerRecursively(t.gameObject, layer);
            }
        }

        public static void SetColliderEnableRecursively(this GameObject obj, bool enable)
        {
            if (obj == null)
            {
                return;
            }
            if (obj.GetComponent<Collider>())
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

        /// <summary>
        /// 填充列表，并自动调整框高，注意，函数中有异步操作，对于被使用来填充的prefab中有Resize的情况时需要谨慎处理
        /// 注意，以左上角为锚点
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="container"></param>
        /// <param name="prefab"></param>
        /// <param name="contents"></param>
        /// <param name="onInstantiatePrefab"></param>
        /// <param name="needClear"></param>
        public static void FillContent<T>(GameObject container, GameObject prefab, List<T> contents, UnityAction<GameObject, T> onInstantiatePrefab, bool needClear = true)
        {
            // clear
            if (needClear)
            {
                foreach (Transform t in container.transform)
                {
                    GameObject.Destroy(t.gameObject);
                }
            }

            // hide first
            prefab.SetActive(false);

            for (int i = 0; i < contents.Count; i++)
            {
                var content = contents[i];
                GameObject item = GameObject.Instantiate<GameObject>(prefab, container.transform);
                item.SetActive(true);
                onInstantiatePrefab(item, content);       // 这里可能改变item大小
            }
            
            _ = ResizeContainer(container);
        }

        /// <summary>
        /// 根据布局重型调整容器大小，主要用于scrollrect中的content resize
        /// </summary>
        /// <param name="container"></param>
        public static async Task<bool> ResizeContainer(GameObject container)
        {
            var containerTrans = container.GetComponent<RectTransform>();
            if (container.transform.childCount > 0)
            {
                // 注意，这个地方不一定靠谱，对于如gridlayout，不一定能触发重排
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerTrans);

                var firstChild = container.transform.GetChild(0) as RectTransform;
                var lastChild = container.transform.GetChild(container.transform.childCount - 1) as RectTransform;
                if (container.GetComponent<VerticalLayoutGroup>())
                {
                    VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
                    //containerTrans.sizeDelta = new Vector2(containerTrans.sizeDelta.x, layout.padding.top + -(lastChild.anchoredPosition.y - firstChild.anchoredPosition.y) + lastChild.sizeDelta.y + layout.padding.bottom);
                    containerTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, layout.padding.top + -(lastChild.anchoredPosition.y - firstChild.anchoredPosition.y) + lastChild.sizeDelta.y + layout.padding.bottom);
                }
                else if (container.GetComponent<HorizontalLayoutGroup>())
                {
                    HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
                    //containerTrans.sizeDelta = new Vector2(layout.padding.left + lastChild.anchoredPosition.x - firstChild.anchoredPosition.x + lastChild.sizeDelta.x + layout.padding.right, containerTrans.sizeDelta.y);
                    containerTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, layout.padding.left + lastChild.anchoredPosition.x - firstChild.anchoredPosition.x + lastChild.sizeDelta.x + layout.padding.right);
                }
                else if (container.GetComponent<GridLayoutGroup>())   //  too complicated, make it simple
                {
                    GridLayoutGroup layout = container.GetComponent<GridLayoutGroup>();
                    if (layout.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
                    {
                        //containerTrans.sizeDelta = new Vector2(containerTrans.sizeDelta.x, -((containerTrans.GetChild(containerTrans.childCount - 1) as RectTransform).anchoredPosition.y - (containerTrans.GetChild(0) as RectTransform).anchoredPosition.y) + layout.cellSize.y + layout.padding.top + layout.padding.bottom);
                        containerTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, -((containerTrans.GetChild(containerTrans.childCount - 1) as RectTransform).anchoredPosition.y - (containerTrans.GetChild(0) as RectTransform).anchoredPosition.y) + layout.cellSize.y + layout.padding.top + layout.padding.bottom);
                    }
                    else if (layout.constraint == GridLayoutGroup.Constraint.FixedRowCount)
                    {
                        //containerTrans.sizeDelta = new Vector2((containerTrans.GetChild(containerTrans.childCount - 1) as RectTransform).anchoredPosition.x - (containerTrans.GetChild(0) as RectTransform).anchoredPosition.x + layout.cellSize.x + layout.padding.left + layout.padding.right, containerTrans.sizeDelta.y);
                        containerTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (containerTrans.GetChild(containerTrans.childCount - 1) as RectTransform).anchoredPosition.x - (containerTrans.GetChild(0) as RectTransform).anchoredPosition.x + layout.cellSize.x + layout.padding.left + layout.padding.right);
                    }
                    else
                    {
                        await new YieldInstructionAwaiter(null);
                        containerTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, -((containerTrans.GetChild(containerTrans.childCount - 1) as RectTransform).anchoredPosition.y - (containerTrans.GetChild(0) as RectTransform).anchoredPosition.y) + layout.cellSize.y + layout.padding.top + layout.padding.bottom);

                    }
                }
                else
                {
                    // not resize
                }
            }

            return true;
        }

        /// <summary>
        /// 用于滚动区域根据内容数量计算大小
        /// </summary>
        /// <param name="container"></param>
        /// <param name="delay"></param>
        public static async void ResizeContainerHeight(GameObject container)
        {
            await new YieldInstructionAwaiter(null);
            await new YieldInstructionAwaiter(null);
            await new YieldInstructionAwaiter(null);

            //var size = container.GetComponent<RectTransform>().sizeDelta;
            if(container.transform.childCount > 0)
            {
                var layout = container.GetComponent<LayoutGroup>();

                var firstChild = container.transform.GetChild(0).GetComponent<RectTransform>();
                var lastChild = container.transform.GetChild(container.transform.childCount - 1).GetComponent<RectTransform>();
                //size.y = layout.padding.top + layout.padding.bottom + (-(lastChild.localPosition.y - firstChild.localPosition.y)) + firstChild.GetComponent<RectTransform>().sizeDelta.y * 0.5f + lastChild.GetComponent<RectTransform>().sizeDelta.y * 0.5f;

                container.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, layout.padding.top + layout.padding.bottom + (-(lastChild.localPosition.y - firstChild.localPosition.y)) + firstChild.GetComponent<RectTransform>().sizeDelta.y * 0.5f + lastChild.GetComponent<RectTransform>().sizeDelta.y * 0.5f);
            }

        }

        /// <summary>
        /// 使用本地文件生成Texture2d
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Texture2D LoadTextureFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            if (!File.Exists(filePath))
            {
                Logger.Warning($"{filePath} not exists..");
                return null;
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
            Texture2D texture = new Texture2D(256, 256);
            texture.LoadImage(bytes);

            return texture;
        }

        /// <summary>
        /// 异步记载纹理,web 平台不适用
        /// 只支持png和jpg格式
        /// </summary>
        /// <param name="filePath">支持网络路径</param>
        /// <returns></returns>
        public static async Task<Texture2D> LoadTextureAsync(string filePath)
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(filePath))
            {
                await www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Fail load texture {filePath}");
                    return null;
                }
#if UNITY_EDITOR
                // 程序退出时可能www对应的资源清空了，GetContent会报异常
                if (!Application.isPlaying)
                {
                    return null;
                }
#endif
                var tex = DownloadHandlerTexture.GetContent(www);
                return tex;
            }
        }

        /// <summary>
        /// 使用本地文件生成sprite
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="pivot"></param>
        /// <returns></returns>
        public static Sprite CreateSprite(Texture2D texture)
        {
            return CreateSprite(texture, new Rect(0, 0, texture.width, texture.height));
        }

        public static Sprite CreateSprite(Texture2D texture, Rect rect)
        {
            //创建Sprite
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
            return sprite;
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
        public static T CopyComponent<T>(this GameObject go, T toAdd) where T : Component
        {
            var comp = go.AddComponent(toAdd.GetType());
            if (comp)
            {
                return comp.GetCopyOf(toAdd) as T;
            }

            return null;
        }

        /// <summary>
        /// 移动GameObject上的组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toMove"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static T MoveComponentTo<T>(T toMove, GameObject target) where T : Component
        {
            var comp = CopyComponent(target, toMove);
            if (!comp)
            {
                Logger.Error("MoveComponent Fail...");
                return null;
            }
            GameObject.Destroy(toMove);

            return comp;
        }

        /// <summary>
        /// 专门用于生成缩略图
        /// </summary>
        private static Camera thumbnailCam { get; set; }
        /// <summary>
        /// 创建缩略图
        /// </summary>
        /// <param name="path"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="cam"></param>
        /// <returns></returns>
        public static Texture2D CreateSceneThumbnail(string path, int width, int height, Camera cam)
        {
            if(thumbnailCam == null)
            {
                var camObj = new GameObject("thumbnail camera",typeof(Camera));
                //var camObj = GameObject.Instantiate<GameObject>(Camera.main.gameObject,null);    // use camera on scene
                camObj.name = "thumbnail camera";
                DontDestroyRootOnLoad(camObj);
                thumbnailCam = camObj.GetComponent<Camera>();
                thumbnailCam.enabled = false;

            }
            // 
            thumbnailCam.CopyFrom(cam);
            thumbnailCam.transform.position = cam.transform.position;
            thumbnailCam.transform.rotation = cam.transform.rotation;

            width = (int)Mathf.Clamp(width, 1, 2048);
            height = (int)Mathf.Clamp(height, 1, 2048);

            var rtd = new RenderTextureDescriptor(width, height) { depthBufferBits = 24, msaaSamples = 8, useMipMap = false, sRGB = true };
            var rt = new RenderTexture(rtd);
            rt.Create();

            thumbnailCam.targetTexture = rt;
            thumbnailCam.aspect = (float)width / (float)height;
            thumbnailCam.Render();
            thumbnailCam.targetTexture = null;
            thumbnailCam.ResetAspect();
            var icon = new Texture2D(width, height, TextureFormat.RGBA32, true, false);


            var oldActive = RenderTexture.active;
            RenderTexture.active = rt;
            icon.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            icon.Apply();
            RenderTexture.active = oldActive;
            rt.Release();

            // save
            var bytes = icon.EncodeToJPG();
            File.WriteAllBytes(path, bytes);
            return icon;

        }

#if UNITY_EDITOR || WUGOU_NETWORK_INFOMATION
        /// <summary>
        /// 获取第一个网卡的mac地址
        /// </summary>
        /// <returns></returns>
        public static string GetFirstMACAddress()
        {
            System.Net.NetworkInformation.NetworkInterface[] nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            String sMacAddress = string.Empty;
            foreach (var adapter in nics)
            {
                if(adapter.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                {
                    return adapter.GetPhysicalAddress().ToString();
                }

                //if (sMacAddress == String.Empty)// only return MAC Address from first card
                //{
                //    //IPInterfaceProperties properties = adapter.GetIPProperties();
                //    sMacAddress = adapter.GetPhysicalAddress().ToString();

                //}
            }
            return "";
        }

        /// <summary>
        /// 获取本机所有的额mac地址
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllMacAddress()
        {
            System.Net.NetworkInformation. NetworkInterface[] nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            HashSet<string> res = new HashSet<string>();
            foreach (var adapter in nics)
            {
                res.Add(adapter.GetPhysicalAddress().ToString());
            }
            return res.ToList();
        }
#endif

        /// <summary>
        /// Indicates whether or not the specified type is a list.
        /// </summary>
        /// <param name="type">The type to query</param>
        // <returns>True if the type is a list, otherwise false</returns>
        public static bool IsListOrArray(Type type)
        {
            if (null == type)
            {
                throw new ArgumentNullException("type");
            }
            if (typeof(System.Collections.IList).IsAssignableFrom(type))  // 数组也会通过
            {
                return true;
            }

            foreach (var it in type.GetInterfaces())
            {
                if (it.IsGenericType && typeof(IList<>) == it.GetGenericTypeDefinition())
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves the collection element type from this type
        /// </summary>
        /// <param name="type">The type to query</param>
        /// <returns>The element type of the collection or null if the type was not a collectior
        /// </returns>
        public static Type GetCollectionElementType(Type type)
        {
            if (null == type)
                throw new ArgumentNullException("type");

            // first try the generic way
            // this is easy, just query the IEnumerable<T> interface for its generic parameter
            var etype = typeof(IEnumerable<>);
            foreach (var bt in type.GetInterfaces())
            {
                if (bt.IsGenericType && bt.GetGenericTypeDefinition() == etype)
                    return bt.GetGenericArguments()[0];
            }

            // now try the non-generic way
            // if it's a dictionary we always return DictionaryEntry

            if (typeof(System.Collections.IDictionary).IsAssignableFrom(type))
                return typeof(System.Collections.DictionaryEntry);

            // if it's a list we look for an Item property with an int index parameter// where the property type is anything but object
            if (typeof(System.Collections.IList).IsAssignableFrom(type))
            {
                foreach (var prop in type.GetProperties())
                {
                    if ("Item" == prop.Name && typeof(object) != prop.PropertyType)
                    {
                        var ipa = prop.GetIndexParameters();
                        if (1 == ipa.Length && typeof(int) == ipa[0].ParameterType)
                            return prop.PropertyType;
                    }
                }

            }

            // if it's a collection, we look for an Add() method whose parameter is/ anything but object
            if (typeof(System.Collections.ICollection).IsAssignableFrom(type))
            {
                foreach (var meth in type.GetMethods())
                {
                    if ("Add" == meth.Name)
                    {
                        var pa = meth.GetParameters();
                        if (1 == pa.Length && typeof(object) != pa[0].ParameterType)
                            return pa[0].ParameterType;
                    }
                }
            }

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
                return typeof(object);

            return null;
        }

        /// <summary>
        /// 深拷贝对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T DeepClone<T>(T obj) where T : class
        {
            if(obj is ScriptableObject)
            {
                var ret = ScriptableObject.CreateInstance(typeof(T));   
                var ss = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
                Newtonsoft.Json.JsonConvert.PopulateObject(ss,ret);
                return ret as T;
            }
            else
            {
                var ss = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(ss);
            }

        }

        public static async Task<string> WebRequest(string uri, WWWForm form)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Post(uri, form))
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {Authorization.activeUser.token}");
                // Request and wait for the desired page.
                await webRequest.SendWebRequest();

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        return webRequest.downloadHandler.text;
                }
            }

            return "";
        }

        /// <summary>
        /// 用于异步方法调用，避免重复代码
        /// </summary>
        /// <param name="action"></param>
        public static void DoAsync(System.Action action)
        {
            action?.Invoke();
        }

        /// <summary>
        /// 拷贝文件夹
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="targetPath"></param>
        public static void CopyDirectory(string sourcePath, string targetPath)
        {
            // 
            Directory.CreateDirectory(targetPath);

            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }


            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        /// <summary>
        /// 结构体转byte[]
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static byte[] StructToBytes<T>(T obj)
        {
            var size = Marshal.SizeOf(obj);
            byte[] bytes = new byte[size];

            IntPtr structPtr = IntPtr.Zero;
            try
            {
                structPtr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(obj, structPtr, false);
                Marshal.Copy(structPtr, bytes, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(structPtr);
            }

            return bytes;
        }

        /// <summary>
        /// byte转结构体
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static T BytesToStruct<T>(byte[] bytes)
        {
            Type type = typeof(T);
            int size = Marshal.SizeOf(type);
            if (size > bytes.Length)
                return default;

            T obj = default(T);
            IntPtr structPtr = IntPtr.Zero;
            try
            {
                structPtr = Marshal.AllocHGlobal(size);
                Marshal.Copy(bytes, 0, structPtr, size);

               obj = (T)Marshal.PtrToStructure(structPtr, type);
            }
            finally
            {
                Marshal.FreeHGlobal(structPtr);
            }

            return obj;
        }

        /// <summary>
        /// 获取子物体相对父物体的路径，用于Find查找
        /// </summary>
        /// <param name="child"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        public static string GetRelativePath(Transform child, Transform root)
        {
            var t = child.transform;
            string path = child.name;
            while (t && t.parent != root)
            {
                path = t.parent.name + "/" + path;
                t = t.parent;
            }

            return path;
        }

        public static bool IsPrefab(GameObject obj)
        {
#if UNITY_EDITOR
            return UnityEditor.PrefabUtility.IsPartOfPrefabAsset(obj);
#else
            return false;
#endif
        }

        /// <summary>
        /// 判断鼠标是否在UI上
        /// </summary>
        /// <returns></returns>
        public static bool IsPointerOnUI()
        {
            return EventSystem.current && EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>
        /// 计算图片裁切区域，避免拉升或压缩，策略：适配短边，裁切长边
        /// </summary>
        /// <param name="imageWidth"></param>
        /// <param name="imageHeight"></param>
        /// <param name="containerWidth"></param>
        /// <param name="containerHeight"></param>
        /// <returns>归一化的矩形区域</returns>
        public static Rect AdaptiveImageRect(float imageWidth, float imageHeight, float containerWidth, float containerHeight)
        {
            var radio1 = imageWidth / imageHeight;
            var radio2 = containerWidth / containerHeight;

            if (radio1 > radio2)
            {
                var t = (radio1 / radio2 - 1.0f) * 0.5f;
                return new Rect(t, 0, 1.0f - t, 1);
            }
            else
            {
                var t = (radio2 / radio1 - 1.0f) * 0.5f;
                return new Rect(0, t, 1, 1.0f - t);
            }
        }

        /// <summary>
        /// 让image自适应图片，避免拉升的感觉
        /// </summary>
        /// <param name="image"></param>
        public static void AdaptiveResolution(this RawImage image)
        {
            var tex = image.texture;
            if (tex)
            {
                image.uvRect = AdaptiveImageRect(tex.width, tex.height, image.GetComponent<RectTransform>().sizeDelta.x, image.GetComponent<RectTransform>().sizeDelta.y);
            }
        }
    }
}

