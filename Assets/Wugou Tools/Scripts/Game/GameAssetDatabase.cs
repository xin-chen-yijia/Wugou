using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;


namespace Wugou
{
    #region LiteFileSystem
    /// <summary>
    /// 轻量化的一个文件系统，用于统一AB包、本地资源管理
    /// 借鉴Linux和UE文件挂载的概念
    /// </summary>
    public abstract class LiteFileSystemBase
    {
        public abstract Task<T> GetAsset<T>(string path) where T : UnityEngine.Object;

        public virtual Task<bool> Load() { return Task.FromResult(true); }

        public virtual void Unload() { }
    }

    /// <summary>
    /// 一个Assetbundle包当做一个文件系统
    /// </summary>
    public class AssetBundleFileSystem : LiteFileSystemBase
    {
        public AssetPackageLoader assetLoader { get; private set; }
        public string path { get; private set; }

        public List<GameAsset> assets { get; private set; } = new List<GameAsset>();

        /// <summary>
        /// AB包文件夹
        /// </summary>
        /// <param name="path"></param>
        public AssetBundleFileSystem(string path)
        {
            this.path = path;
        }

        public override async Task<bool> Load()
        {
            assetLoader = await AssetPackageLoader.GetOrCreate(path);
            if (assetLoader == null)
            {
                Logger.Error($"{path} not exist..");
                return false;
            }

            foreach (var v in assetLoader.GetAllConetents())
            {
                assets.Add(new GameAsset(v, "", "assetbundle"));
            }

            return true;
        }

        public override void Unload()
        {
            if (assetLoader != null)
            {
                assetLoader.Unload(false);
                assetLoader = null;
            }
        }

        /// <summary>
        /// 加载sceneName所在的assetbundle
        /// </summary>
        /// <param name="sceneName"></param>
        public async Task<bool> LoadScene(string sceneName)
        {
            if (assetLoader != null)
            {
                var abName = assetLoader.GetAssetBundleByAssetName(sceneName);
                var ab = await assetLoader.LoadAssetBundleAsync(abName);

                return ab != null;
            }

            Logger.Error("GameAssetDatabase didn't loaded...");
            return false;
        }

        /// <summary>
        /// 加载assetbundle并获取assetbundle中的asset
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public override async Task<T> GetAsset<T>(string path)
        {
            if (assetLoader != null)
            {
                return await assetLoader.LoadAssetAsync<T>(path);
            }

            Logger.Error("GameAssetDatabase didn't loaded...");
            return null;
        }
    }

    /// <summary>
    /// 用于本地文件
    /// </summary>
    public class LocalFileSystem : LiteFileSystemBase
    {
        public string path { get; private set; }
        public LocalFileSystem(string path)
        {
            this.path = path;
        }

        internal static async Task<Texture2D> LoadTextureAsync(string path)
        {
            return await Utils.LoadTextureAsync($"{path}");
        }

        public override async Task<T> GetAsset<T>(string path)
        {
            if(path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".jpeg"))
            {
                path = $"{this.path}/{path}";
                if(typeof(T) == typeof(Texture2D))
                {
                    return (await LoadTextureAsync(path)) as T;
                }
            }
            else
            {
                Wugou.Logger.Error($"Not Support {typeof(T).FullName} in LocalFileSystem..");
            }

            return null;
        }
    }

    /// <summary>
    /// 用于web资源
    /// </summary>
    public class WebFileSystem : LiteFileSystemBase
    {
        private string cachedPath_ => $"{Application.persistentDataPath}/.webcache";
        private string cachedListFie_ => $"{cachedPath_}/.cache";

        private class WebFileDesc
        {
            public string uri;
            public string file;
            public string modifiedTime;
            public string expiredTime;
        }

        Dictionary<string,WebFileDesc> filelist_ = new Dictionary<string, WebFileDesc>();
        public WebFileSystem()
        {
            Directory.CreateDirectory(cachedPath_);

            // 检查缓存
            if (File.Exists(cachedListFie_))
            {
                var files = JsonConvert.DeserializeObject<List<WebFileDesc>>(File.ReadAllText(cachedListFie_, System.Text.Encoding.UTF8));

                var remainList = new List<WebFileDesc>();
                for(int i=0;i<files.Count; i++)
                {
                    var t = DateTime.Parse(files[i].expiredTime);
                    if (DateTime.Compare(t, DateTime.Now) < 0)
                    {
                        File.Delete($"{cachedPath_}/{files[i].file}");
                    }
                    else
                    {
                        remainList.Add(files[i]);
                    }
                }

                for(int i = 0; i < remainList.Count; i++)
                {
                    filelist_.Add(remainList[i].uri, remainList[i]);
                }
            }
        }

        public async Task<T> GetAssetInternal<T>(string path) where T : UnityEngine.Object
        {
            T asset = null;
            if (typeof(T) == typeof(Texture2D))
            {
                asset = (await LocalFileSystem.LoadTextureAsync(path)) as T;
            }
            else
            {
                Wugou.Logger.Error($"load {path} with wrong type....");
            }

            return asset;
        }

        public override async Task<T> GetAsset<T>(string path)
        {
            bool isPng = path.EndsWith(".png");
            bool isJpg = path.EndsWith(".jpg") || path.EndsWith("jpeg");
            if (isPng || isJpg)
            {
                var fileName = Path.GetFileName(path);
                WebFileDesc webFileDesc = null;

                // 先看有沒有緩存，緩存包括记录和实体文件
                bool hasCache = filelist_.ContainsKey(path) && File.Exists($"{cachedPath_}/{filelist_[path].file}");

                // 获取服务器文件信息，看是否需要更新
                using (var request = UnityWebRequest.Head(path))
                {
                    await request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var modifiedStr = request.GetResponseHeader("last-modified");
                        var modifiedTime = DateTime.Parse(modifiedStr);
                        if (hasCache)
                        {
                            var oldModifiedTime = DateTime.Parse(filelist_[path].modifiedTime);
                            if (DateTime.Compare(modifiedTime, oldModifiedTime) <= 0)   // 不需要更新，读取缓存
                            {
                                // 获取缓存图片
                                var tt = await GetAssetInternal<T>($"{cachedPath_}/{filelist_[path].file}");
                                return (tt);
                            }
                        }


                        // 否则更新记录
                        webFileDesc = new WebFileDesc()
                        {
                            uri = request.url,
                            file = fileName,
                            modifiedTime = request.GetResponseHeader("last-modified"),
                            expiredTime = DateTime.Now.AddDays(2).ToString()
                        };

                    }
                }

                // 获取web图片
                T asset = await GetAssetInternal<T>(path);

                // 存储
                if (asset)
                {
                    Texture2D tex = asset as Texture2D;
                    if (tex)
                    {
                        var data = isJpg ? tex.EncodeToJPG() : tex.EncodeToPNG();
                        File.WriteAllBytes($"{cachedPath_}/{fileName}", data);
                    }
                }
                if (webFileDesc != null)
                {
                    filelist_[webFileDesc.uri] = webFileDesc;
                }

                return asset;
            }

            Wugou.Logger.Error($"Not support {path}");
            return null;
        }

        public override void Unload()
        {
            File.WriteAllText(cachedListFie_, JsonConvert.SerializeObject(new List<WebFileDesc>(filelist_.Values)), System.Text.Encoding.UTF8);
        }
    }

    /// <summary>
    /// 用于统一Resource文件夹下的资源加载
    /// 注意，因为Resource是Unity管理的，且文件夹名称指定为了"Resources"
    /// </summary>
    public class ResourceFileSystem : LiteFileSystemBase
    {
        public override async Task<T> GetAsset<T>(string path)
        {
            var op = Resources.LoadAsync<T>(path);
            await op;

            return op.asset as T;
        }
    }

    public class PathInfo
    {
        public string root;
        public string name;
    }

    #endregion


    /// <summary>
    /// 用于系统管理所有资源
    /// 1. AB包；
    /// 2. 本地文件;
    /// 3. Resources文件；
    /// 4. web资源文件；
    /// </summary>
    public static class GameAssetDatabase
    {
        public const string kWebMountPoint = "/web";
        public const string kBuiltInMountPoint = "/BuiltIn";
        public const string kResourcesMountPoint = "/Resources";

        private static Dictionary<string, LiteFileSystemBase> mountedFileSystems_ = new Dictionary<string, LiteFileSystemBase>()
        {
            {kWebMountPoint, new WebFileSystem() },
            {kResourcesMountPoint, new ResourceFileSystem() },
        };

        /// <summary>
        /// 挂载一个Assetbundle
        /// </summary>
        /// <param name="mountPoint"></param>
        /// <param name="path"></param>
        /// <param name="overwrite"></param>
        public static async Task<bool> MountAssetBundle(string mountPoint, string path, bool overwrite = false)
        {
            if(!overwrite && mountedFileSystems_.ContainsKey(mountPoint))
            {
                return true;
            }

            var abFS = new AssetBundleFileSystem(path);
            var ret = await abFS.Load();

            if (ret)
            {
                mountedFileSystems_[mountPoint] = abFS;
            }

            return ret;
        }

        public static void MountDirectory(string mountPoint, string path)
        {
            var localFS = new LocalFileSystem(path);
            mountedFileSystems_[mountPoint] = localFS;

            localFS.Load();
        }

        public static void Unmount(string mountPoint)
        {
            if (mountedFileSystems_.ContainsKey(mountPoint))
            {
                var fs = mountedFileSystems_[mountPoint];
                fs.Unload();

                mountedFileSystems_.Remove(mountPoint);
            }
        }

        public static void UnmountAll()
        {
            foreach(var v in mountedFileSystems_)
            {
                v.Value.Unload();
            }
            mountedFileSystems_.Clear();
        }

        /// <summary>
        /// 判断是否是可加载的AB包
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsAssetbundle(string path)
        {
            return File.Exists($"{path}/{Path.GetFileName(path)}{AssetPackageLoader.kDescFileNameSuffix}");
        }

        /// <summary>
        /// 解析路径信息
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static PathInfo GetPathInfo(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Logger.Error($"assetPath can't be empty..");
                return null;
            }
            if (path[0] != '/')
            {
                Logger.Error($"{path} is not valid in GameAssetDatabase..");
                return null;
            }

            int pos = path.IndexOf("/", 1);
            if (pos == -1)
            {
                Logger.Error($"{path} is not valid in GameAssetDatabase..");
                return null;
            }

            return new PathInfo() { root = path.Substring(0, pos), name = path.Substring(pos + 1) };
        }

        // 缓存
        private static Dictionary<string, UnityEngine.Object> cachedAssets_ = new Dictionary<string, UnityEngine.Object>();
        // 正在加载的缓存
        private static Dictionary<string, Task> cachedLoadingAssets_ = new Dictionary<string, Task>();

        /// <summary>
        /// 获取游戏资产
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetPath">1. "http"开头的资产代表web资源；2. BuiltIn目录开头的资产代表Unity Resources文件夹中的资产</param>
        /// <returns></returns>
        public static async Task<T> GetAssetAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                Logger.DebugInfo($"GameAssetDatabase.GetAssetAsync with null or empty string...");
                return null;
            }

            if (cachedAssets_.ContainsKey(assetPath))
            {
                return cachedAssets_[assetPath] as T;
            }

            // 正在加载中的
            if (cachedLoadingAssets_.ContainsKey(assetPath))
            {
                var task = cachedLoadingAssets_[assetPath] as Task<T>;
                return await task;
            }


            if (assetPath.StartsWith("http",StringComparison.OrdinalIgnoreCase))
            {
                return await mountedFileSystems_[kWebMountPoint].GetAsset<T>(assetPath);
            }

            var pathInfo = GetPathInfo(assetPath);
            var fs = GetFileSystem(pathInfo.root);
            if (fs != null)
            {
                var task = fs.GetAsset<T>(pathInfo.name);
                cachedLoadingAssets_.Add(assetPath, task);
                var asset = await task;
                cachedAssets_.Add(assetPath, asset);
                cachedLoadingAssets_.Remove(assetPath);

                return asset;
            }

            return null;
        }

        /// <summary>
        /// 获取指定的文件系统
        /// </summary>
        /// <param name="mountPoint"></param>
        /// <returns></returns>
        public static LiteFileSystemBase GetFileSystem(string mountPoint)
        {
            if (!mountedFileSystems_.ContainsKey(mountPoint))
            {
                Logger.Error($"No mounted point: {mountPoint} in GameAssetDatabase..");
                return null;
            }

            return mountedFileSystems_[mountPoint];
        }

        /// <summary>
        /// 获取原始的资产名称
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public static string GetRawAssetName(string path)
        {
            return path.Substring(path.IndexOf('/', 1) + 1);
        }

        public static async Task<bool> LoadScene(string scene)
        {
            // 避免其它文件后缀的影响
            if (!scene.EndsWith(".unity"))
            {
                scene = scene + ".unity";
            }

            var pathInfo = GetPathInfo(scene);
            if (pathInfo.root.StartsWith(kBuiltInMountPoint)){
                for(int i=0;i< UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; ++i)
                {
                    var scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                    if (scenePath.Contains(pathInfo.name))
                    {
                        return true;
                    }
                }
                return false;
            }


            if (!mountedFileSystems_.ContainsKey(pathInfo.root))
            {
                Logger.Error($"No {scene} in GameAssetDatabase..");
                return false;
            }

            var abSys = mountedFileSystems_[pathInfo.root] as AssetBundleFileSystem;
            if( abSys == null)
            {
                Logger.Error($"No {pathInfo.root} is not a AssetBundleFileSystem..");
                return false;
            }

            //TODO: unload assetbundle
            return await abSys.LoadScene(pathInfo.name);
        }

        /// <summary>
        /// 获取Assetbundle中的资产内容
        /// </summary>
        /// <param name="mountPoint"></param>
        /// <returns></returns>
        public static List<GameAsset> GetAssetbundleAssets(string mountPoint)
        {
            if(mountedFileSystems_.ContainsKey(mountPoint))
            {
                var abSys = mountedFileSystems_[mountPoint] as AssetBundleFileSystem;
                return abSys.assets;
            }

            return new List<GameAsset>();
        }

        //public static void LoadAssetWithCoroutine<T>(GameAsset gameAsset, System.Action<GameObject> onLoaded = null) where T : UnityEngine.Object
        //{
        //    var loader = CoroutineAssetBundleAssetLoader.GetOrCreate(FindAssetBundle(gameAsset.location));
        //    loader.LoadAssetAsync<GameObject>(gameAsset.name, onLoaded);
        //}


    }
}

