using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Wugou
{
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
        /// 挂载类型
        /// </summary>
        public enum MountContentType
        {
            kAssetbundle=0,
            kFileSystem
        }

        public static async Task<bool> Mount(string mountPoint, string path, MountContentType contentType = MountContentType.kAssetbundle, bool overwrite = false)
        {
            switch (contentType)
            {
                case MountContentType.kAssetbundle:
                    return await MountAssetBundle(mountPoint, path, overwrite);
                case MountContentType.kFileSystem:
                    MountDirectory(mountPoint, path, overwrite);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 挂载一个Assetbundle
        /// </summary>
        /// <param name="mountPoint"></param>
        /// <param name="path"></param>
        /// <param name="overwrite"></param>
        private static async Task<bool> MountAssetBundle(string mountPoint, string path, bool overwrite = false)
        {
            Debug.Assert(!string.IsNullOrEmpty(mountPoint));
            Debug.Assert(!string.IsNullOrEmpty(path));

            if (!overwrite && mountedFileSystems_.ContainsKey(mountPoint))
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
        /// <summary>
        /// 挂载一个目录
        /// </summary>
        /// <param name="mountPoint"></param>
        /// <param name="path"></param>
        /// <param name="overwrite"></param>
        private static void MountDirectory(string mountPoint, string path, bool overwrite = false)
        {
            Debug.Assert(!string.IsNullOrEmpty(mountPoint));
            Debug.Assert(!string.IsNullOrEmpty(path));

            if (mountedFileSystems_.ContainsKey(mountPoint) && !overwrite)
            {
                return;
            }

            var localFS = new LocalFileSystem(path);
            mountedFileSystems_[mountPoint] = localFS;

            localFS.Load();
        }

        /// <summary>
        /// 卸载某个挂载点
        /// </summary>
        /// <param name="mountPoint"></param>
        public static void Unmount(string mountPoint)
        {
            foreach(var v in cachedAssets_)
            {
                if (v.Key.StartsWith(mountPoint))
                {
                    cachedAssets_.Remove(v.Key);
                }
            }

            if (mountedFileSystems_.ContainsKey(mountPoint))
            {
                var fs = mountedFileSystems_[mountPoint];
                fs.Unload();

                mountedFileSystems_.Remove(mountPoint);
            }
        }

        public static void UnmountAll()
        {
            ClearAllCaches();

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

        private class PathInfo
        {
            public string root;
            public string name;
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

        // 缓存已加载的
        private static Dictionary<string, UnityEngine.Object> cachedAssets_ = new Dictionary<string, UnityEngine.Object>();
        // 缓存正在加载的
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
                Logger.DebugInfo($"Except '{assetPath}'. GameAssetDatabase.GetAssetAsync with null or empty string...");
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

            var pathInfo = GetPathInfo(assetPath);
            var fs = GetFileSystem(pathInfo.root);
            if (fs != null)
            {
                var task = fs.GetAsset<T>(pathInfo.name);
                cachedLoadingAssets_.Add(assetPath, task);
                var asset = await task;

                Debug.Assert(!cachedAssets_.ContainsKey(assetPath));
                Debug.Assert(cachedLoadingAssets_.ContainsKey(assetPath));

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

        public static bool HasScene(string scene)
        {
            // 避免其它文件后缀的影响
            if (!scene.EndsWith(".unity"))
            {
                scene = scene + ".unity";
            }

            var pathInfo = GetPathInfo(scene);
            if (pathInfo.root.StartsWith(kBuiltInMountPoint))
            {
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; ++i)
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
                Logger.Error($"No {pathInfo.root} in GameAssetDatabase..");
                return false;
            }

            var abSys = GetFileSystem(pathInfo.root) as AssetBundleFileSystem;
            if (abSys == null)
            {
                Logger.Error($"{pathInfo.root} is not a AssetBundleFileSystem..");
                return false;
            }

            return true;
        }

        public static async Task<bool> LoadScene(string scene)
        {
            // 避免其它文件后缀的影响
            if (!scene.EndsWith(".unity"))
            {
                scene = scene + ".unity";
            }

            if (!HasScene(scene))
            {
                return false;
            }

            var pathInfo = GetPathInfo(scene);
            var abSys = GetFileSystem(pathInfo.root) as AssetBundleFileSystem;

            Debug.Assert(abSys != null);

            //TODO: unload assetbundle
            Debug.Log($"{scene} {pathInfo.name}");
            return await abSys.LoadScene(pathInfo.name);
        }

        public enum LitePathMount
        {
            kBuiltIn=0,
            kResource,
            kHTTP,
        }

        /// <summary>
        /// 封装http路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetWrapMountPath(LitePathMount mount,  string path)
        {
            switch (mount)
            {
                case LitePathMount.kBuiltIn:
                    return $"{kBuiltInMountPoint}/{path}";
                case LitePathMount.kResource:
                    return $"{kResourcesMountPoint}/{path}";
                case LitePathMount.kHTTP:
                    return $"{kWebMountPoint}/{path}";
                default:
                    break;
            }

            return path;
        }

        /// <summary>
        /// 注册一个动态生成资产
        /// </summary>
        /// <param name="path"></param>
        /// <param name="asset"></param>
        public static void RegisterAsset(string  path, GameObject asset)
        {
            cachedAssets_[path] = asset;
        }

        public static void UnregisterAsset(string path)
        {
            cachedAssets_.Remove(path);
        }

        /// <summary>
        /// 清理所有缓存，避免引用一直存在，导致资源不能卸载
        /// </summary>
        public static void ClearAllCaches()
        {
            cachedAssets_.Clear();
            Resources.UnloadUnusedAssets();
        }
    }
}

