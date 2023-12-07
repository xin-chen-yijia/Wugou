using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Wugou
{
    /// <summary>
    /// 加载Unity Assetbundle
    /// 每个工程打一个assetbundle包，在这个类中要处理多个工程打的多个包，从多个包中找到具体资源加载
    /// </summary>
    public class CoroutineAssetBundleAssetLoader
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
            kDescriptionError,
            kPluginSupportFileNotExist,
            kMissingPlugin
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

        private AssetBundleLauchDesc assetbundleLauchDesc_ = null; //assetbundle 描述信息

        private AssetBundleManifest mainManifest_ = null;

        //避免重复加载
        private Dictionary<string, AssetBundle> loadedBundles_ = new Dictionary<string, AssetBundle>();

        private bool isWebRequest_ = false;    // 是否用UnityWebRequestAssetBundle加载assetbunle

        public bool valid { private set; get; } = true;   //loader 是否有效

        private bool vrSupport_ = false; // 支持VR资产


        // 因为assetbundle是全局的，所以loader也是全局的
        private static Dictionary<string, CoroutineAssetBundleAssetLoader> sLoaders_ = new Dictionary<string, CoroutineAssetBundleAssetLoader>();

        private CoroutineAssetBundleAssetLoader(string path, bool vrSupport)
        {
            // 为了后续拼接方便
            assetbundlesDir_ = path.Replace('\\', '/');
            if (assetbundlesDir_.EndsWith("/"))
            {
                assetbundlesDir_ = assetbundlesDir_.Substring(0, assetbundlesDir_.Length - 1);
            }

            if (assetbundlesDir_.StartsWith("http://") || assetbundlesDir_.StartsWith("file:///"))
            {
                isWebRequest_ = true;
            }

            vrSupport_ = vrSupport;
        }

        /// <summary>
        /// 获取asset所在的assetbundle,名称推荐使用全路径
        /// </summary>
        /// <param name="assetName">推荐使用全路径</param>
        /// <returns></returns>
        public string GetAssetBundleByAssetName(string assetName)
        {
            if (!string.IsNullOrEmpty(assetName) && assetbundleLauchDesc_ != null)
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

        public T LoadAssetAndInstantiate<T>(string assetName, string instantiateName = "", Transform parent = null) where T : UnityEngine.Object
        {
            T t = LoadAsset<T>(assetName);
            if (t != null)
            {
                T it = GameObject.Instantiate<T>(t, parent);
                if (!string.IsNullOrEmpty(instantiateName))
                {
                    it.name = instantiateName;
                }

                return it;
            }

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
        /// 检查是否有效，如插件引用，VR支持
        /// </summary>
        /// <returns></returns>
        private AssetBundleLoadResult CheckValid()
        {
            if (vrSupport_ != assetbundleLauchDesc_.vrAssets)
            {
                //error = $"assetbundle's vr support not same";
                Logger.Warning($"{assetbundlesDir_} is VR Assets, but loader not support vr...");
                //return false;
            }

            return AssetBundleLoadResult.kSuccess;
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
        public static CoroutineAssetBundleAssetLoader GetOrCreate(string path, bool vrSupport = false)
        {
            if (sLoaders_.ContainsKey(path))
            {
                return sLoaders_[path];
            }

            var loader = new CoroutineAssetBundleAssetLoader(path, vrSupport);
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

            if (result == AssetBundleLoadResult.kSuccess)
            {
                yield return CheckValid();
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
        /// 加载AB包中的Asset并在场景中实例化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetName"></param>
        /// <param name="instantiateName"></param>
        /// <returns></returns>
        public void LoadAssetAndInstantiateAsync<T>(string assetName, string instantiateName = "", Action<T> onLoaded = null) where T : UnityEngine.Object
        {
            LoadAssetAsync(assetName, (T obj) =>
            {
                T newObj = UnityEngine.Object.Instantiate(obj);
                newObj.name = string.IsNullOrEmpty(instantiateName) ? assetName : instantiateName;
                onLoaded?.Invoke(newObj);
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
        System.Func<string, AsyncAssetBundleLoadResult> LoadAssetbundleFuncInternal => isWebRequest_ ? LoadAssetbundleAsync_web : LoadAssetbundleAsync_file;

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
            string configContent = string.Empty;
            if (isWebRequest_)
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
                    configContent = www.downloadHandler.text;
                }
            }
            else
            {
                string filePath = Path.Combine(assetbundlesDir_, kLauchDescFileName);
                bool valid = File.Exists(filePath);
                if (valid)
                {
                    configContent = File.ReadAllText(filePath);

                }
                else
                {
                    result = AssetBundleLoadResult.kDescriptionFileNotExist;
                    error = $"AssetBundle's description file: {assetbundlesDir_}/{filePath}  not exists...";
                    Logger.Warning(error);
                }
            }

            try
            {
                // Show results as text
                assetbundleLauchDesc_ = JsonConvert.DeserializeObject<AssetBundleLauchDesc>(configContent);
                int curVersion = 0;
                int.TryParse(Application.unityVersion.Substring(0, Application.unityVersion.IndexOf(".")), out curVersion);
                int abVersion = -1;
                int.TryParse(assetbundleLauchDesc_.version.Substring(0, assetbundleLauchDesc_.version.IndexOf(".")), out abVersion);
                if (assetbundleLauchDesc_ == null)
                {
                    result = AssetBundleLoadResult.kDescriptionFormatError;
                    error = "AssetBundle build info format error...";
                    Logger.Error(error);
                }
                else if (abVersion < curVersion)
                {
                    result = AssetBundleLoadResult.kDescriptionError;
                    error = $"AssetBundle build with old version: {assetbundleLauchDesc_.version}...";
                    Logger.Error(error);
                }
                else
                {
                    result = AssetBundleLoadResult.kSuccess;
                }

            }
            catch (Exception e)
            {
                result = AssetBundleLoadResult.kDescriptionFormatError;
                error = $"AssetBundle build info format error... {e.Message}";
                Logger.Error(error);
            }
        }
    }
}