using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace Wugou
{

    public class AssetBundleContent
    {
        public string assetbundleName = string.Empty;
        public List<string> assets = new List<string>();
    }

    /// <summary>
    /// Assetbundle 描述信息
    /// </summary>
    public class AssetBundleLauchDesc
    {
        public string version = string.Empty;
        public bool vrAssets = false;
        public List<AssetBundleContent> contents = new List<AssetBundleContent>();
        public List<string> plugins = new List<string>();
    }

    /// <summary>
    /// 加载Unity Assetbundle
    /// 每个工程打一个assetbundle包，在这个类中要处理多个工程打的多个包，从多个包中找到具体资源加载
    /// </summary>
    public class AssetBundleAssetLoader
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

        //
        /// <summary>
        /// AB包存放路径
        /// </summary>
        public string assetbundleDir { get; private set; }

        private AssetBundleLauchDesc assetbundleLauchDesc_ = null; //assetbundle 描述信息

        private AssetBundleManifest mainManifest_ = null;

        // 记录正在加载的AssetBundle
        private Dictionary<string,bool> loadingAssetBundles_ = new Dictionary<string,bool>();
        //避免重复加载
        private Dictionary<string, AssetBundle> loadedBundles_ = new Dictionary<string, AssetBundle>();

        private static bool useWebRequest { get; set; } = true;    // 是否用UnityWebRequestAssetBundle加载assetbunle

        public bool valid { private set; get; } = true;   //loader 是否有效

        public static bool vrSupport { get; set; } = false; // 支持VR资产

        /// <summary>
        /// 初始化状态
        /// </summary>
        private enum InitiazlieStatus
        {
            kUninitialized = 0,
            kInitializing,
            kInitializeComplete,
        }
        private InitiazlieStatus initializeStatus_ = InitiazlieStatus.kUninitialized;


        // 因为assetbundle是全局的，所以loader也是全局的
        private static Dictionary<string,AssetBundleAssetLoader> sLoaders_ = new Dictionary<string,AssetBundleAssetLoader>();

        private AssetBundleAssetLoader(string path)
        {
            // 为了后续拼接方便
            assetbundleDir = path.Replace('\\', '/');
            if (assetbundleDir.EndsWith("/"))
            {
                assetbundleDir = assetbundleDir.Substring(0, assetbundleDir.Length - 1);
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

            if(assetbundleLauchDesc_ == null)
            {
                if(initializeStatus_ == InitiazlieStatus.kUninitialized || initializeStatus_ == InitiazlieStatus.kInitializing)
                {
                    Debug.LogError($"AssetBundle with path: {assetbundleDir} not complete initialized...");
                }
                return "";
            }

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

        private string CombinePath(params string[] paths)
        {
            string res = "";
            for(int i=0;i<paths.Length-1; i++)
            {
                res += paths[i] + "/";
            }
            if(paths.Length > 0)
            {
                res += paths[paths.Length - 1];
            }

            return res;
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
                string baseAbName = Path.GetFileName(assetbundleDir);
                AssetBundle baseAb = AssetBundle.LoadFromFile(CombinePath(assetbundleDir, baseAbName));
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

                loadedBundles_[bundleName] = AssetBundle.LoadFromFile(CombinePath(assetbundleDir, bundleName));
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
        /// 检查 AB包是否有效
        /// </summary>
        /// <returns></returns>
        private AssetBundleLoadResult CheckValid()
        {
            if (assetbundleLauchDesc_.vrAssets && !vrSupport)
            {
                //error = $"assetbundle's vr support not same";
                Logger.Warning($"{assetbundleDir} is VR Assets, but loader not support vr...");
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

        // 使用C#的async和await的异步编程

        /// <summary>
        /// 只支持绝对路径
        /// </summary>
        /// <param name="path">assetbundle的路径</param>
        public static async Task<AssetBundleAssetLoader> GetOrCreate(string path)
        {
            // 这里只对本地路径有效，对http无效
            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(path);
            }

            path = path.Replace('\\','/').TrimEnd('/');  // 统一路径
            string lowerPath = path.ToLower();
            if (!sLoaders_.ContainsKey(lowerPath))
            {
                var newLoader = new AssetBundleAssetLoader(path);
                sLoaders_.Add(lowerPath, newLoader);
            }

            var loader = sLoaders_[lowerPath];
            var res = await loader.Init();
            if (res != AssetBundleLoadResult.kSuccess)
            {
                Logger.Error($"AssetBundle with path:{path} init failed. error:{loader.error}");
                return null;
            }

            return loader;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        private async Task<AssetBundleLoadResult> Init()
        {
            switch (initializeStatus_)
            {
                case InitiazlieStatus.kUninitialized:
                    initializeStatus_ = InitiazlieStatus.kInitializing;

                    result = await LoadAssetBundleLaunchConfig();
                    result = result == AssetBundleLoadResult.kSuccess ? CheckValid() : result;                   
                    break;
                case InitiazlieStatus.kInitializing:
                    while (initializeStatus_ == InitiazlieStatus.kInitializing)
                    {
                        await new YieldInstructionAwaiter(new WaitForSeconds(1.0f));
                    }
                    break;
                default:
                    break;
            }

            initializeStatus_ = InitiazlieStatus.kInitializeComplete;
            return result;
        }

        /// <summary>
        /// 加载AB包中的某个asset到内存中
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public async Task<T> LoadAssetAsync<T>(string assetName) where T : UnityEngine.Object
        {
            string bundleName = GetAssetBundleByAssetName(assetName);
            var bundle = await LoadAssetBundleAsync(bundleName);
            if (!bundle || !bundle.Contains(assetName))
            {
                Logger.Error($"Assetbundle '{assetbundleDir}' not contain's {assetName}");
                return null;
            }
            return bundle.LoadAsset<T>(assetName);
        }

        private async Task<AssetBundle> LoadAssetbundleAsync_web(string path)
        {
            var request = UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle(path, 0);
            await request.SendWebRequest();
            AssetBundle bundle = UnityEngine.Networking.DownloadHandlerAssetBundle.GetContent(request);
            if (bundle)
            {
                Logger.Info("Loaded assetbundle:" + path);
            }
            else
            {
                Logger.Error("load assetbundle:" + path + " failed..");
            }

            return bundle;
        }

        private async Task<AssetBundle> LoadAssetbundleAsync_file(string path)
        {
            var requestOp = AssetBundle.LoadFromFileAsync(path);
            await requestOp;
            AssetBundle bundle = requestOp.assetBundle;
            if (bundle)
            {
                Logger.Info("Loaded assetbundle:" + path);
            }
            else
            {
                Logger.Error("load assetbundle:" + path + " failed..");
            }

            return bundle;
        }

        /// <summary>
        /// 异步加载Assetbundle
        /// </summary>
        /// <param name="bundleName"></param>
        /// <returns></returns>
        public async Task<AssetBundle> LoadAssetBundleAsync(string bundleName)
        {
            Logger.DebugInfo($"Start load assetbundle {bundleName}");
            if (string.IsNullOrEmpty(bundleName))
            {
                Logger.Error("LoadAssetBundleAsync: empty bundle name");
                return null;
            }

            System.Func<string, Task<AssetBundle>> LoadAssetbundleFunc = useWebRequest ? LoadAssetbundleAsync_web : LoadAssetbundleAsync_file;

            if (!mainManifest_)
            {
                string baseAbName = Path.GetFileName(assetbundleDir);
                string manifestBundlePath = CombinePath(assetbundleDir, baseAbName);

                AssetBundle manifestBundle = await LoadAssetbundleFunc(manifestBundlePath);
                mainManifest_ = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                manifestBundle.Unload(false);
            }
            if (!loadedBundles_.ContainsKey(bundleName))
            {
                if (!loadingAssetBundles_.ContainsKey(bundleName)) // bundle未加载
                {
                    loadingAssetBundles_.Add(bundleName, true);

                    string[] dps = mainManifest_.GetAllDependencies(bundleName);
                    foreach (var v in dps)
                    {
                        await LoadAssetBundleAsync(v);
                    }

                    string bundlePath = CombinePath(assetbundleDir, bundleName);

                    loadedBundles_[bundleName] = await LoadAssetbundleFunc(bundlePath);

                    loadingAssetBundles_[bundleName] = false;    // 加载完成
                }
                else        // 加载中
                {
                    while (loadingAssetBundles_.ContainsKey(bundleName) && loadingAssetBundles_[bundleName])
                    {
                        //await Task.Delay(100);    // webgl 平台不行
                        await new YieldInstructionAwaiter(new WaitForSeconds(0.1f));
                    }
                }

            }

            Logger.DebugInfo($"Load assetbundle {bundleName} Complete");
            return loadedBundles_.ContainsKey(bundleName) ? loadedBundles_[bundleName] : null;
        }

        /// <summary>
        /// 获取 assetbundle 的描述文件
        /// </summary>
        /// <returns></returns>
        private async Task<AssetBundleLoadResult> LoadAssetBundleLaunchConfig()
        {
            string configContent = string.Empty;
            if (useWebRequest)
            {
                Logger.Info("load web assetbundle: " + assetbundleDir + "/" + kLauchDescFileName);
                UnityWebRequest www = UnityWebRequest.Get(assetbundleDir + "/" + kLauchDescFileName);
                await www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Logger.Error(www.error);
                    error = www.error;
                    return  AssetBundleLoadResult.kNetworkError;
                }
                else
                {
                    configContent = www.downloadHandler.text;
                }
            }
            else
            {
                //System.Func<Task<object>> tAwaiter = async () =>
                //{
                //    string filePath = CombinePath(assetbundlesDir_, AssetBundlePresets.kLauchDescFileName);
                //    if (File.Exists(filePath))
                //    {
                //        configContent = File.ReadAllText(filePath);

                //    }
                //    else
                //    {
                //        Logger.Warning(filePath + " not exists...");
                //    }

                //    return new object();
                //};
                //await tAwaiter();

                bool valid = true;
                string filePath = CombinePath(assetbundleDir, kLauchDescFileName);
                if (File.Exists(filePath))
                {
                    configContent = File.ReadAllText(filePath);

                }
                else
                {
                    valid = false;
                }

                // 废弃，Task是基于线程池的，也就是非常吃当前任务安排情况，所以await并非等的是当前任务的执行时间，而是整个系统的任务调度时间
                //var task = Task.Run(() =>
                //{
                //    if (File.Exists(filePath))
                //    {
                //        Debug.Log("D read file:" + filePath);
                //        configContent = File.ReadAllText(filePath);
                //        Debug.Log("D read file end:" + filePath);

                //    }
                //    else
                //    {
                //        valid = false;
                //    }
                //});

                //await task;
                if (!valid)
                {
                    error = $"AssetBundle's description file: {filePath}  not exists...";
                    Logger.Error(error);
                    return AssetBundleLoadResult.kDescriptionFileNotExist;
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
                    error = "AssetBundle build info format error...";
                    Logger.Error(error);
                    return AssetBundleLoadResult.kDescriptionFormatError;
                }
                else if (abVersion < curVersion)
                {
                    error = $"AssetBundle build with old version: {assetbundleLauchDesc_.version}...";
                    Logger.Error(error);
                    return AssetBundleLoadResult.kDescriptionError;
                }
                else
                {
                    return AssetBundleLoadResult.kSuccess;
                }

            }
            catch (Exception e)
            {
                error = $"AssetBundle build info format error... {e.Message}";
                Logger.Error(error);
                return AssetBundleLoadResult.kDescriptionFormatError;
            }
        }
    }
}