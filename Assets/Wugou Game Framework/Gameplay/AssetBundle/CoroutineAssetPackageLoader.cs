using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace Wugou
{
    /// <summary>
    /// 加载Unity Assetbundle
    /// 每个工程打一个assetbundle包，在这个类中要处理多个工程打的多个包，从多个包中找到具体资源加载
    /// </summary>
    public class CoroutineAssetPackageLoader
    {
        public const string kPandaVariantName = "panda";
        public const string kLauchDescFileName = "assetbundle_lauch.json";

        /// <summary>
        /// 错误分类
        /// </summary>
        public enum AssetBundleLoadResult
        {
            kSuccess = 0,
            kNetworkError,
            kConfigNotExists,
            kDescriptionFileNotExist,
            kDescriptionFormatError,
        }

        /// <summary>
        /// 错误代码
        /// </summary>
        public AssetBundleLoadResult result { get; private set; } = AssetBundleLoadResult.kSuccess;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string error { get; private set; } = "status ok!";

        // AB包存放路径
        private string assetbundlesDir_ = string.Empty;
        public string assetbundleDir
        {
            get
            {
                return assetbundlesDir_;
            }
        }

        private AssetPackageDescFile assetbundleLauchDesc_ = null; //assetbundle 描述信息

        private AssetBundleManifest mainManifest_ = null;

        //避免重复加载
        private Dictionary<string, AssetBundle> loadedBundles_ = new Dictionary<string, AssetBundle>();

        private static bool useWebRequest { get; set; } = true;    // 是否用UnityWebRequestAssetBundle加载assetbunle

        public bool valid { private set; get; } = true;   //loader 是否有效

        // 因为assetbundle是全局的，所以loader也是全局的
        private static Dictionary<string, CoroutineAssetPackageLoader> sLoaders_ = new Dictionary<string, CoroutineAssetPackageLoader>();

        public static bool vrSupport { get; set; } = false; // 支持VR资产

        private CoroutineAssetPackageLoader(string path, bool vrSupport)
        {
            // 为了后续拼接方便
            assetbundlesDir_ = path.Replace('\\', '/');
            if (assetbundlesDir_.EndsWith("/"))
            {
                assetbundlesDir_ = assetbundlesDir_.Substring(0, assetbundlesDir_.Length - 1);
            }
        }

        /// <summary>
        /// 获取asset所在的assetbundle,名称推荐使用全路径
        /// </summary>
        /// <param name="assetName">推荐使用全路径</param>
        /// <returns></returns>
        public string GetAssetBundleByAssetName(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                Debug.LogError($"assetName is null or empty");
                return "";
            }

            if (assetbundleLauchDesc_ != null)
            {
                foreach (var v in assetbundleLauchDesc_.contents)
                {
                    foreach (var name in v.assets)
                    {
                        if (name.Contains(assetName))
                        {
                            return v.assetbundleName;
                        }
                    }
                }
            }

            return "";
        }

        public T LoadAsset<T>(string assetName) where T : UnityEngine.Object
        {
            if (useWebRequest)
            {
                Logger.Error(string.Format("LoadAsset only valid in web native assetbundle files"));
                return null;
            }
            string bundleName = GetAssetBundleByAssetName(assetName);
            AssetBundle ab = LoadAssetBundleFromFile(bundleName);
            if (ab != null)
            {
                T t = ab.LoadAsset<T>(assetName);
                return t;
            }

            Logger.Error(string.Format("LoadAsset Error.bundleName:{0},assetName:{1}", bundleName, assetName));
            return null;
        }

        /// <summary>
        /// 从本地文件加载assetbundle
        /// </summary>
        /// <param name="bundleName"></param>
        /// <returns></returns>
        public AssetBundle LoadAssetBundleFromFile(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                Logger.Warning($"Load assetbundle with empty name..");
                return null;
            }
            if (!mainManifest_)
            {
                string baseAbName = Path.GetFileName(assetbundlesDir_);
                AssetBundle baseAb = AssetBundle.LoadFromFile(Path.Combine(assetbundlesDir_, baseAbName));
                mainManifest_ = baseAb.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                baseAb.Unload(false);
            }
            if (!loadedBundles_.ContainsKey(bundleName))
            {
                string[] dps = mainManifest_.GetAllDependencies(bundleName);
                foreach (var v in dps)
                {
                    LoadAssetBundleFromFile(v);
                }

                loadedBundles_[bundleName] = AssetBundle.LoadFromFile(System.IO.Path.Combine(assetbundlesDir_, bundleName));
            }

            return loadedBundles_[bundleName];
        }

        /// <summary>
        /// unload assetbundle
        /// </summary>
        /// <param name="unloadAllLoadedObjects">Determines whether the current instances of objects loaded from the AssetBundle will also be unloaded.</param>
        public void UnloadAssetBundle(bool unloadAllLoadedObjects)
        {
            foreach (var v in loadedBundles_)
            {
                v.Value.Unload(unloadAllLoadedObjects);
            }

            loadedBundles_.Clear();
        }

        /// <summary>
        /// unload assetbundle async
        /// </summary>
        /// <param name="unloadAllLoadedObjects">Determines whether the current instances of objects loaded from the AssetBundle will also be unloaded.</param>
        public void UnloadAssetBundleAsync(bool unloadAllLoadedObjects)
        {
            foreach (var v in loadedBundles_)
            {
                v.Value.Unload(unloadAllLoadedObjects);
            }

            loadedBundles_.Clear();
        }

        /// <summary>
        /// Unload all assetbundle
        /// </summary>
        public static void UnloadAllAssetBundle()
        {
            foreach (var v in sLoaders_)
            {
                v.Value.UnloadAssetBundle(true);
            }
            sLoaders_.Clear();
        }

        /// <summary>
        /// 异步Unload
        /// </summary>
        public static void UnloadAllAsync()
        {
            foreach (var v in sLoaders_)
            {
                v.Value.UnloadAssetBundleAsync(true);
            }
            sLoaders_.Clear();
        }

        /// <summary>
        /// 获取所有assets
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllConetents()
        {
            List<string> result = new List<string>();
            foreach (var v in assetbundleLauchDesc_.contents)
            {
                result.AddRange(v.assets);
            }
            return result;
        }

        /// <summary>
        /// http或file开头的路径使用WebRequest，其它的路径当做本地文件处理
        /// </summary>
        /// <param name="path"></param>
        /// <param name="vrSupport"></param>
        public static CoroutineAssetPackageLoader GetOrCreate(string path, bool vrSupport = false)
        {
            if (sLoaders_.ContainsKey(path))
            {
                return sLoaders_[path];
            }

            var loader = new CoroutineAssetPackageLoader(path, vrSupport);
            sLoaders_.Add(path, loader);
            return loader;
        }

        /// <summary>
        ///  初始化
        /// </summary>
        /// <param name="onInitialized"></param>
        public void Init(Action onInitialized)
        {
            CoroutineLauncher.active.StartCoroutine(InitInternal(onInitialized));
        }

        private IEnumerator InitInternal(Action onInitialized)
        {
            if (result == AssetBundleLoadResult.kSuccess)
            {
                yield return LoadAssetBundleLaunchConfig();
            }

            onInitialized?.Invoke();
        }

        /// <summary>
        /// 加载AB包中的某个asset到内存中(注意：非实例化）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public void LoadAssetAsync<T>(string assetName, Action<T> onLoaded) where T : UnityEngine.Object
        {
            string bundleName = GetAssetBundleByAssetName(assetName);
            LoadAssetBundleAsync(bundleName, (AssetBundle bundle) =>
            {
                if (bundle == null)
                {
                    Logger.Error($"Load '{bundleName} failed..'");
                }
                else
                {
                    if (!bundle.Contains(assetName))
                    {
                        Logger.Warning($"Assetbundle '{bundleName}' not contain's {assetName}");
                    }
                    else
                    {
                        T obj = bundle.LoadAsset<T>(assetName);
                        onLoaded?.Invoke(obj);
                    }

                }
            });
        }

        /// <summary>
        /// 记录异步加载assetbundle信息
        /// </summary>
        private class AsyncAssetBundleLoadResult
        {
            public AsyncOperation asyncOp;
            public Func<AssetBundle> getAssetBundle;
        }

        private AsyncAssetBundleLoadResult LoadAssetbundleAsync_web(string path)
        {
            var request = UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle(path, 0);
            return new AsyncAssetBundleLoadResult()
            {
                asyncOp = request.SendWebRequest(),
                getAssetBundle = () =>
                {
                    return UnityEngine.Networking.DownloadHandlerAssetBundle.GetContent(request);
                }
            };
        }

        private AsyncAssetBundleLoadResult LoadAssetbundleAsync_file(string path)
        {
            var requestOp = AssetBundle.LoadFromFileAsync(path);
            return new AsyncAssetBundleLoadResult()
            {
                asyncOp = requestOp,
                getAssetBundle = () =>
                {
                    return requestOp.assetBundle;
                }
            };
        }

        // 区分是否使用WebRequest
        System.Func<string, AsyncAssetBundleLoadResult> LoadAssetbundleFuncInternal => useWebRequest ? LoadAssetbundleAsync_web : LoadAssetbundleAsync_file;

        /// <summary>
        /// 异步加载Assetbundle
        /// </summary>
        /// <param name="bundleName"></param>
        public void LoadAssetBundleAsync(string bundleName, Action<AssetBundle> onLoaded)
        {
            CoroutineLauncher.active.StartCoroutine(LoadAssetBundleAsyncInternal(bundleName, onLoaded));
        }

        /// <summary>
        /// 异步加载Assetbundle
        /// </summary>
        /// <param name="bundleName"></param>
        /// <returns></returns>
        private IEnumerator LoadAssetBundleAsyncInternal(string bundleName, Action<AssetBundle> onLoaded = null)
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                Logger.Error("LoadAssetBundleAsync: empty bundle name");
                yield return null;
            }

            if (!mainManifest_)
            {
                string baseAbName = Path.GetFileName(assetbundlesDir_);
                string manifestBundlePath = Path.Combine(assetbundlesDir_, baseAbName);

                var res = LoadAssetbundleFuncInternal(manifestBundlePath);
                yield return res.asyncOp;

                AssetBundle manifestBundle = res.getAssetBundle();
                mainManifest_ = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                manifestBundle.Unload(false);
            }
            if (!loadedBundles_.ContainsKey(bundleName))
            {
                string[] dps = mainManifest_.GetAllDependencies(bundleName);
                foreach (var v in dps)
                {
                    var it = LoadAssetBundleAsyncInternal(v);
                    yield return it;
                }

                string bundlePath = System.IO.Path.Combine(assetbundlesDir_, bundleName);
                var res = LoadAssetbundleFuncInternal(bundlePath);
                yield return res.asyncOp;
                loadedBundles_[bundleName] = res.getAssetBundle();

                onLoaded?.Invoke(loadedBundles_[bundleName]);
            }
        }

        /// <summary>
        /// 获取 assetbundle 的描述文件
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadAssetBundleLaunchConfig()
        {
            string path = assetbundlesDir_ + "/" + kLauchDescFileName;
            Logger.Info("load web assetbundle: " + path);
            UnityWebRequest www = UnityWebRequest.Get(path);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Logger.Error(www.error);
                error = www.error;
                result = AssetBundleLoadResult.kNetworkError;
            }
            else
            {
                string configContent = www.downloadHandler.text;
                // Show results as text
                assetbundleLauchDesc_ = JsonConvert.DeserializeObject<AssetPackageDescFile>(configContent);
                if (assetbundleLauchDesc_ == null)
                {
                    result = AssetBundleLoadResult.kDescriptionFormatError;
                    error = "AssetBundle build info format error...";
                    Logger.Error(error);
                }
            }
        }
    }
}