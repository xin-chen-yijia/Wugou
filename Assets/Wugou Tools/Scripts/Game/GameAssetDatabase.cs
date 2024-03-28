using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;


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

        private static Dictionary<string, Texture2D> cachedTextures_ = new Dictionary<string, Texture2D>();
        private static Dictionary<string, Task<Texture2D>> cachedLoadingTextures_ = new Dictionary<string,Task<Texture2D>>();

        internal static async Task<Texture2D> LoadTextureAsync(string path)
        {
            if (cachedTextures_.ContainsKey(path) && cachedTextures_[path])
            {
                return cachedTextures_[path];
            }

            if (cachedLoadingTextures_.ContainsKey(path))
            {
                return await cachedLoadingTextures_[path];
            }

            var task = Utils.LoadTextureAsync($"{path}");
            cachedLoadingTextures_[path] = task;

            var tex = await task;
            cachedTextures_[path] = tex;
            cachedLoadingTextures_.Remove(path);

            return tex;
        }

        private static Dictionary<string, Sprite> cachedSprites_ = new Dictionary<string, Sprite>();

        internal static async Task<Sprite> LoadSpriteAsync(string path)
        {
            if (cachedSprites_.ContainsKey(path) && cachedSprites_[path])
            {
                return cachedSprites_[path];
            }

            var tex = await LoadTextureAsync($"{path}");
            if (!tex)
            {
                return null;
            }

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            cachedSprites_[path] = sprite;
            return sprite;

        }

        public override async Task<T> GetAsset<T>(string path)
        {
            path = $"{this.path}/{path}";
            if(path.EndsWith(".png") || path.EndsWith(".jpg"))
            {
                if(typeof(T) == typeof(Sprite))
                {
                    return (await LoadSpriteAsync(path)) as T;
                }
                else if(typeof(T) == typeof(Texture2D))
                {
                    return (await LoadTextureAsync(path)) as T;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// 用于web资源
    /// </summary>
    public class WebFileSystem : LiteFileSystemBase
    {
        public override async Task<T> GetAsset<T>(string path)
        {
            if (path.EndsWith(".png") || path.EndsWith(".jpg"))
            {
                if (typeof(T) == typeof(Sprite))
                {
                    return (await LocalFileSystem.LoadSpriteAsync(path)) as T;
                }
                else if (typeof(T) == typeof(Texture2D))
                {
                    return (await LocalFileSystem.LoadTextureAsync(path)) as T;
                }
            }

            return null;
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

        private static Dictionary<string, LiteFileSystemBase> mountedFileSystems_ = new Dictionary<string, LiteFileSystemBase>()
        {
            {kWebMountPoint, new WebFileSystem() },
            {kBuiltInMountPoint, new ResourceFileSystem() },
        };

        /// <summary>
        /// 挂载一个Assetbundle
        /// </summary>
        /// <param name="path"></param>
        public static async Task<bool> MountAssetBundle(string mountPoint, string path)
        {
            var abFS = new AssetBundleFileSystem(path);
            mountedFileSystems_[mountPoint] = abFS;

            return await abFS.Load();
           
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

            if (assetPath.StartsWith("http",StringComparison.OrdinalIgnoreCase))
            {
                return await mountedFileSystems_[kWebMountPoint].GetAsset<T>(assetPath);
            }

            var pathInfo = GetPathInfo(assetPath);
            var fs = GetFileSystem(pathInfo.root);
            if (fs != null)
            {
                return await fs.GetAsset<T>(pathInfo.name);
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

