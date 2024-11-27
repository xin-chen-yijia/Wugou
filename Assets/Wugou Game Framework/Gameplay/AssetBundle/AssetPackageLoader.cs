using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Wugou
{
    /// <summary>
    /// Assetbundle描述文件中队assetbundle的描述
    /// </summary>
    public class AssetPackageContent
    {
        public string assetbundleName = string.Empty;
        public List<string> assets = new List<string>();
    }

    /// <summary>
    /// 自定义Assetbundle的资产描述信息，代替AssetbundleManifest使用
    /// </summary>
    public class AssetPackageDescFile
    {
        public string unityVersion = string.Empty;
        public string wugouVersion = string.Empty;
        public string createTime = string.Empty;
        public bool isEncrypt = false;
        public List<AssetPackageContent> contents = new List<AssetPackageContent>();
    }

    #region Assetbundle Loader Implement

    /// <summary>
    /// 错误分类
    /// </summary>
    public enum AssetPackageAssetLoadResult
    {
        kSuccess = 0,
        kNetworkError,
        kDescriptionFileNotExist,
        kDescriptionFormatError,
    }

    /// <summary>
    /// AB包加载基类
    /// </summary>
    internal abstract class AssetbundleLoaderBase
    {
        public AssetBundleManifest mainManifest { get; protected set; }

        //避免重复加载
        protected Dictionary<string, AssetBundle> loadedAssetbundles_ = new Dictionary<string, AssetBundle>();

        public AssetPackageDescFile assetbundleLauchDesc { get; protected set; }
        public abstract AssetBundle LoadAssetBundle(string path);
        public abstract Task<AssetBundle> LoadAssetBundleAsync(string path);
        public abstract Task<AssetPackageAssetLoadResult> LoadDescription();

        /// <summary>
        /// unload assetbundle
        /// </summary>
        /// <param name="unloadAllLoadedObjects">Determines whether the current instances of objects loaded from the AssetBundle will also be unloaded.</param>
        public void Unload(bool unloadAllLoadedObjects)
        {
            foreach (var v in loadedAssetbundles_)
            {
                v.Value.Unload(unloadAllLoadedObjects);
            }

            loadedAssetbundles_.Clear();
        }

        /// <summary>
        /// unload assetbundle async
        /// </summary>
        /// <param name="unloadAllLoadedObjects">Determines whether the current instances of objects loaded from the AssetBundle will also be unloaded.</param>
        public void UnloadAsync(bool unloadAllLoadedObjects)
        {
            foreach (var v in loadedAssetbundles_)
            {
                v.Value.UnloadAsync(unloadAllLoadedObjects);
            }

            loadedAssetbundles_.Clear();
        }

        /// <summary>
        /// 检查是否存在循环引用
        /// </summary>
        /// <returns></returns>
        public bool HasLoopRef()
        {
            bool hasLoopRef = false;
            foreach (var v in mainManifest.GetAllAssetBundles())
            {
                HashSet<string> deps = new HashSet<string>();
                //Debug.LogError($"开始检查 {v} 的引用！");
                if (!LoopRefCheckImpl(mainManifest, v, deps))
                {
                    hasLoopRef = true;
                    break;
                }
            }

            return hasLoopRef;
        }

        private bool LoopRefCheckImpl(AssetBundleManifest manifest, string bundleName, HashSet<string> deps)
        {
            if (deps.Contains(bundleName))
            {
                Debug.LogError($"{bundleName} 出现了循环引用！");
                foreach (var dep in deps)
                {
                    Debug.LogError($"{dep}");
                }
                return false;
            }

            deps.Add(bundleName);

            foreach (var v in manifest.GetAllDependencies(bundleName))
            {
                if (!LoopRefCheckImpl(manifest, v, deps))
                {
                    return false;
                }
            }

            deps.Remove(bundleName);
            return true;
        }

    }

    /// <summary>
    /// web AssetBundle资源加载
    /// 1. UnityWebRequestAssetBundle.GetAssetBundle的方式下载AB包；
    /// 2. 不支持加密；
    /// </summary>
    internal class WebAssetbundleLoaderInternal : AssetbundleLoaderBase
    {
        public string rootPath { get; private set; }

        public WebAssetbundleLoaderInternal(string path)
        {
            this.rootPath = path;
        }

        public override AssetBundle LoadAssetBundle(string path)
        {
            throw new Exception("Can't use sync function in web assetbundle loader...");
        }

        private async Task<AssetBundle> LoadAssetBundleAsyncInternal(string path)
        {
            var request = UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle($"{path}", 0);
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

        public override async Task<AssetBundle> LoadAssetBundleAsync(string path)
        {
            if (loadedAssetbundles_.ContainsKey(path))
            {
                return loadedAssetbundles_[path];
            }

            if (!mainManifest)
            {
                string baseAbName = Path.GetFileName(rootPath);
                string manifestBundlePath = $"{rootPath}/{baseAbName}";
                AssetBundle manifestBundle = await LoadAssetBundleAsyncInternal(manifestBundlePath);
                mainManifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                manifestBundle.Unload(false);
            }

            string[] dps = mainManifest.GetAllDependencies(path);
            foreach (var v in dps)
            {
                await LoadAssetBundleAsync(v);
            }

            var ab = await LoadAssetBundleAsyncInternal($"{rootPath}/{path}");
            loadedAssetbundles_.Add(path, ab);    // 加载完成

            return ab;
        }

        public override async Task<AssetPackageAssetLoadResult> LoadDescription()
        {
            Logger.Info($"load assetbundle: {rootPath}/{Path.GetFileName(rootPath)}{AssetPackageLoader.kDescFileNameSuffix}");
            UnityWebRequest www = UnityWebRequest.Get($"{rootPath}/{Path.GetFileName(rootPath)}{AssetPackageLoader.kDescFileNameSuffix}");
            await www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Logger.Error($"LoadABDescription: {www.error}");
                return AssetPackageAssetLoadResult.kNetworkError;
            }
            else
            {
                string configContent = www.downloadHandler.text;
                assetbundleLauchDesc = JsonConvert.DeserializeObject<AssetPackageDescFile>(configContent);
                if (assetbundleLauchDesc == null)
                {
                    Logger.Error("AssetBundle build info format error...");
                    return AssetPackageAssetLoadResult.kDescriptionFormatError;
                }

                Logger.DebugInfo($"{rootPath} build with:{assetbundleLauchDesc.unityVersion}");
                Logger.DebugInfo($"{rootPath}' version: {assetbundleLauchDesc.wugouVersion}");
                if(assetbundleLauchDesc.wugouVersion != PackageInformation.latestVersion)
                {
                    Logger.Warning($"{rootPath}' build with: {assetbundleLauchDesc.wugouVersion}, May not be compatible with {PackageInformation.latestVersion}...");
                }

                return AssetPackageAssetLoadResult.kSuccess;
            }
        }
    }

    /// <summary>
    /// 本地文件Assetbundle加载
    /// 1. 普通模式使用LoadFromFile
    /// 2. 加密模式使用LoadFromStream;
    /// </summary>
    internal class LocalAssetbundleInternal : AssetbundleLoaderBase
    {
        public string rootPath { get; private set; }

        public LocalAssetbundleInternal(string path)
        {
            this.rootPath = path;
        }

        public AssetBundle LoadAssetBundleInternal(string path)
        {
            if (assetbundleLauchDesc.isEncrypt)
            {
                using (var fileStream = new XORFileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, 1024 * 4, false))
                {
                    return AssetBundle.LoadFromStream(fileStream);
                }
            }


            return AssetBundle.LoadFromFile(path);
        }

        public override AssetBundle LoadAssetBundle(string path)
        {
            if (loadedAssetbundles_.ContainsKey(path))
            {
                return loadedAssetbundles_[path];
            }

            if (!mainManifest)
            {
                string baseAbName = Path.GetFileName(rootPath);
                AssetBundle baseAb = LoadAssetBundleInternal($"{rootPath}/{baseAbName}");
                mainManifest = baseAb.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                baseAb.Unload(false);
            }

            string[] dps = mainManifest.GetAllDependencies(path);
            foreach (var v in dps)
            {
                LoadAssetBundle(v);
            }

            var ab = LoadAssetBundleInternal($"{rootPath}/{path}");
            if (ab)
            {
                loadedAssetbundles_[path] = ab;
            }

            return ab;
        }

        public async Task<AssetBundle> LoadAssetBundleAsyncInternal(string path)
        {
            if (assetbundleLauchDesc.isEncrypt)
            {
                using (var fileStream = new XORFileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 4, false))
                {
                    var streamOp = AssetBundle.LoadFromStreamAsync(fileStream);
                    await streamOp;
                    Debug.Assert(streamOp.assetBundle);

                    return streamOp.assetBundle;
                }
            }

            var aop = AssetBundle.LoadFromFileAsync(path);
            await aop;
            Debug.Assert(aop.assetBundle);

            return aop.assetBundle;
        }

        /// <summary>
        /// 缓存正在加载的assetbundle，因为Unity不让加载同一个ab包两次
        /// </summary>
        Dictionary<string, Task<AssetBundle>> loadingRequests = new Dictionary<string, Task<AssetBundle>>();

        private Task<AssetBundle> manifestAbTask_ = null;

        public override async Task<AssetBundle> LoadAssetBundleAsync(string path)
        {
            if (loadedAssetbundles_.ContainsKey(path))
            {
                return loadedAssetbundles_[path];
            }

            if (!mainManifest)
            {
                if (manifestAbTask_ == null)
                {
                    string baseAbName = Path.GetFileName(rootPath);
                    string manifestBundlePath = $"{rootPath}/{baseAbName}";
                    manifestAbTask_ = LoadAssetBundleAsyncInternal(manifestBundlePath);
                }

                AssetBundle manifestBundle = await manifestAbTask_;
                if (!mainManifest)
                {
                    mainManifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                    manifestBundle.Unload(false);
                }
            }

            string[] dps = mainManifest.GetAllDependencies(path);
            foreach (var v in dps)
            {
                await LoadAssetBundleAsync(v);
            }

            if (loadingRequests.ContainsKey(path))
            {
                return await loadingRequests[path];
            }

            var task = LoadAssetBundleAsyncInternal($"{rootPath}/{path}");
            loadingRequests.Add(path, task);
            var ab = await task;
            loadingRequests.Remove(path);

            loadedAssetbundles_.Add(path, ab);    // 加载完成

            return ab;
        }

        public override Task<AssetPackageAssetLoadResult> LoadDescription()
        {
            string descFile = $"{rootPath}/{Path.GetFileName(rootPath)}{AssetPackageLoader.kDescFileNameSuffix}";
            Logger.DebugInfo($"load assetbundle: {descFile}");

            if (!File.Exists(descFile))
            {
                Logger.Error($"Load description fail. {descFile} not exist.");
                return Task.FromResult(AssetPackageAssetLoadResult.kDescriptionFileNotExist);
            }
            else
            {
                string configContent = File.ReadAllText(descFile);
                assetbundleLauchDesc = JsonConvert.DeserializeObject<AssetPackageDescFile>(configContent);
                if (assetbundleLauchDesc == null)
                {
                    Logger.Error("AssetBundle build info format error...");
                    return Task.FromResult(AssetPackageAssetLoadResult.kDescriptionFormatError);
                }

                Logger.DebugInfo($"{rootPath} build with:{assetbundleLauchDesc.unityVersion}");
                return Task.FromResult(AssetPackageAssetLoadResult.kSuccess);
            }
        }
    }

    /// <summary>
    /// zip 文件Assetbundle
    /// 第一种中方案：简单点，采用先解压到磁盘，再加载
    /// 另一种是读取zip文件中的指定内容，然后使用AssetBundle.LoadFromStream加载
    /// </summary>
    internal class ZipAssetbundleLoader : AssetbundleLoaderBase
    {
        public string filePath { get; private set; }

        private Dictionary<string, AssetBundle> loadedAssetBundles_ = new Dictionary<string, AssetBundle>();

        public ZipAssetbundleLoader(string path)
        {
            this.filePath = path;
        }

        ~ZipAssetbundleLoader()
        {

        }

        byte[] ReadContent(ZipArchiveEntry entry)
        {
            var content = new byte[entry.Length];
            entry.Open().Read(content, 0, content.Length);

            return content;
        }
        AssetBundle LoadFromEntry(ZipArchiveEntry entry)
        {
            if(entry == null)
            {
                return null;
            }

            var content = new byte[entry.Length];
            entry.Open().Read(content, 0, content.Length);

            if (assetbundleLauchDesc.isEncrypt)
            {

                XORMemoryStream stream = new XORMemoryStream(content);
                return AssetBundle.LoadFromStream(stream);
            }
            else
            {
                MemoryStream stream = new MemoryStream(content);
                return AssetBundle.LoadFromStream(stream);
            }

        }


        public override AssetBundle LoadAssetBundle(string path)
        {
            if (loadedAssetBundles_.ContainsKey(path))
            {
                return loadedAssetBundles_[path];
            }

            using (var zipToOpen = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read, true, Encoding.GetEncoding("gbk")))
                {
                    var count = archive.Entries.Count;
                    if (count == 0)
                    {
                        return null;
                    }
                    string root = archive.Entries[0].FullName;
                    root = root.Replace(@"\", "/");
                    root = root.Substring(0, root.IndexOf('/'));

                    Dictionary<string, ZipArchiveEntry> entriesDict = new Dictionary<string, ZipArchiveEntry>();
                    for (int i = 0; i < count; i++)
                    {
                        var entry = archive.Entries[i];
                        if (!entry.FullName.EndsWith("/") && !entry.FullName.EndsWith(".manifest") && !entry.FullName.EndsWith(AssetPackageLoader.kDescFileNameSuffix))
                        {
                            string tPath = entry.FullName.Replace($"{root}/", "");
                            entriesDict.Add(tPath, entry);
                        }

                    }

                    if (entriesDict.ContainsKey(root))
                    {
                        // manifrest
                        AssetBundle manifestBundle = LoadFromEntry(entriesDict[root]);
                        var mainManifest = manifestBundle?.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                        manifestBundle.Unload(false);

                        string[] dps = mainManifest.GetAllDependencies(path);
                        foreach (var v in dps)
                        {
                            LoadAssetBundle(v);
                        }

                        if(entriesDict.ContainsKey(path))
                        {
                            var ab = LoadFromEntry(entriesDict[path]);
                            loadedAssetBundles_.Add(path, ab);    // 加载完成

                            return ab;
                        }
                    }

                    return null;
                }
            }
        }

        async Task<AssetBundle> LoadFromEntryAsync(ZipArchiveEntry entry)
        {
            var content = new byte[entry.Length];
            entry.Open().Read(content, 0, content.Length);

            if (assetbundleLauchDesc.isEncrypt)
            {
                XORMemoryStream stream = new XORMemoryStream(content);
                var aop = AssetBundle.LoadFromStreamAsync(stream);
                await aop;

                return aop.assetBundle;
            }
            else
            {
                MemoryStream stream = new MemoryStream(content);
                var aop = AssetBundle.LoadFromStreamAsync(stream);
                await aop;

                return aop.assetBundle;
            }

        }

        public override async Task<AssetBundle> LoadAssetBundleAsync(string path)
        {
            using (var zipToOpen = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read, true, Encoding.GetEncoding("gbk")))
                {
                    var count = archive.Entries.Count;
                    if (count == 0)
                    {
                        return null;
                    }
                    string root = archive.Entries[0].FullName;
                    root = root.Replace(@"\", "/");
                    root = root.Substring(0, root.IndexOf('/'));

                    Dictionary<string, ZipArchiveEntry> entriesDict = new Dictionary<string, ZipArchiveEntry>();
                    for (int i = 0; i < count; i++)
                    {
                        var entry = archive.Entries[i];
                        if (!entry.FullName.EndsWith("/") && !entry.FullName.EndsWith(".manifest") && !entry.FullName.EndsWith(AssetPackageLoader.kDescFileNameSuffix))
                        {
                            string tPath = entry.FullName.Replace($"{root}/", "");
                            entriesDict.Add(tPath, entry);
                        }

                    }

                    if(entriesDict.ContainsKey(root))
                    {
                        // manifrest
                        AssetBundle manifestBundle = await LoadFromEntryAsync(entriesDict[root]);
                        var mainManifest = manifestBundle?.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                        manifestBundle.Unload(false);

                        string[] dps = mainManifest.GetAllDependencies(path);
                        foreach (var v in dps)
                        {
                            LoadAssetBundle(v);
                        }

                        if (entriesDict.ContainsKey(path))
                        {
                            var ab = await LoadFromEntryAsync(entriesDict[path]);
                            loadedAssetBundles_.Add(path, ab);    // 加载完成

                            return ab;
                        }
                    }

                    return null;
                }
            }
        }

        public override Task<AssetPackageAssetLoadResult> LoadDescription()
        {
            using (var zipToOpen = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read, true, Encoding.GetEncoding("gbk")))
                {
                    if (archive.Entries.Count == 0)
                    {
                        return null;
                    }

                    var root = archive.Entries[0].FullName;
                    root = root.Replace(@"\", "/");
                    root = root.Substring(0, root.IndexOf('/'));

                    var annotName = $"{root}{AssetPackageLoader.kDescFileNameSuffix}";
                    for (int i = 0; i < archive.Entries.Count; i++)
                    {
                        var entry = archive.Entries[i];
                        if(entry.Name == annotName)
                        {
                            var content = new byte[entry.Length];
                            entry.Open().Read(content, 0, content.Length);

                            assetbundleLauchDesc = JsonConvert.DeserializeObject<AssetPackageDescFile>(Encoding.GetEncoding("utf-8").GetString(content));
                            return Task.FromResult(AssetPackageAssetLoadResult.kSuccess);
                        }

                    }

                    return null;
                }
            }
        }
    }

    #endregion

    /// <summary>
    /// 加载Unity Assetbundle
    /// 每个工程打一个assetbundle包，在这个类中要处理多个工程打的多个包，从多个包中找到具体资源加载
    /// </summary>
    public class AssetPackageLoader
    {
        public const string kPandaVariantName = "panda";
        public const string kDescFileNameSuffix = ".annot";

        /// <summary>
        /// 错误信息
        /// </summary>
        public string error { get; private set; } = "status ok!";

        //
        /// <summary>
        /// AB包存放路径
        /// </summary>
        public string assetbundleDir { get; private set; }

        private AssetPackageDescFile assetbundleLauchDesc => loaderImp_.assetbundleLauchDesc; //assetbundle 描述信息

        // 因为assetbundle是全局的，所以loader也是全局的
        private static Dictionary<string,AssetPackageLoader> sLoaders_ = new Dictionary<string,AssetPackageLoader>();

        private AssetbundleLoaderBase loaderImp_ = null;

        private AssetPackageLoader(string path)
        {
            // 为了后续拼接方便
            assetbundleDir = path;

            if(path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                loaderImp_ = new WebAssetbundleLoaderInternal(path);
            }
            else if(path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                loaderImp_ = new ZipAssetbundleLoader(path);
            }
            else
            {
                loaderImp_ = new LocalAssetbundleInternal(path);
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

            if(assetbundleLauchDesc == null)
            {
                Debug.LogError($"AssetBundle with path: {assetbundleDir} not complete initialized...");
                return "";
            }

            foreach (var v in assetbundleLauchDesc.contents)
            {
                foreach (var name in v.assets)
                {
                    if (name.EndsWith($"/{assetName}") || name == assetName)
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
            AssetBundle ab = LoadAssetbundle(bundleName);
            if (ab != null)
            {
                T t = ab.LoadAsset<T>(assetName);
                return t;
            }

            Logger.Error(string.Format("LoadAsset Error.bundleName:{0},assetName:{1}", bundleName, assetName));
            return null;
        }

        public AssetBundle LoadAssetbundle(string path)
        {
            Logger.DebugInfo($"Start load assetbundle {path}");
            if (string.IsNullOrEmpty(path))
            {
                Logger.Error("LoadAssetBundleAsync: empty bundle name");
                return null;
            }

            var ab = loaderImp_.LoadAssetBundle($"{path}");
            if (ab)
            {
                Logger.DebugInfo($"Load assetbundle {path} Complete");
            }
            else
            {
                Logger.Error($"Load assetbundle {path} fail...");
            }

            return ab;
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
            var bundle = await LoadAssetbundleAsync(bundleName);
            if (!bundle || !bundle.Contains(assetName))
            {
                Logger.Error($"Assetbundle '{assetbundleDir}' not contain's {assetName}. find assetbundle:{bundleName}");
                return null;
            }
            var loadOp = bundle.LoadAssetAsync<T>(assetName);
            await loadOp;
            return loadOp.asset as T;
        }

        /// <summary>
        /// 异步加载Assetbundle
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<AssetBundle> LoadAssetbundleAsync(string path)
        {
            Logger.DebugInfo($"Start load assetbundle {path}");
            if (string.IsNullOrEmpty(path))
            {
                Logger.Error("LoadAssetbundleAsync: empty bundle name");
                return null;
            }

            var ab = await loaderImp_.LoadAssetBundleAsync($"{path}");

            if (ab)
            {
                Logger.DebugInfo($"Load assetbundle {path} Complete");
            }
            else
            {
                Logger.Error($"Load assetbundle {path} fail...");
            }

            return ab;
        }

        /// <summary>
        /// 获取 assetbundle 的描述文件
        /// </summary>
        /// <returns></returns>
        private async Task<AssetPackageAssetLoadResult> LoadDescription()
        {
            return await loaderImp_.LoadDescription();
        }

        public string GetDescFileMD5()
        {
            var content = JsonConvert.SerializeObject(assetbundleLauchDesc);
            MD5CryptoServiceProvider md5Provider = new MD5CryptoServiceProvider();
            var buffer = md5Provider.ComputeHash(Encoding.UTF8.GetBytes(content));

            return BitConverter.ToString(buffer).Replace("-","");
        }

        /// <summary>
        /// unload assetbundle
        /// </summary>
        /// <param name="unloadAllLoadedObjects">Determines whether the current instances of objects loaded from the AssetBundle will also be unloaded.</param>
        public void Unload(bool unloadAllLoadedObjects)
        {
            loaderImp_.Unload(unloadAllLoadedObjects);
        }

        /// <summary>
        /// unload assetbundle async
        /// </summary>
        /// <param name="unloadAllLoadedObjects">Determines whether the current instances of objects loaded from the AssetBundle will also be unloaded.</param>
        public void UnloadAsync(bool unloadAllLoadedObjects)
        {
            loaderImp_.UnloadAsync(unloadAllLoadedObjects);
        }

        /// <summary>
        /// 获取所有assets
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllConetents()
        {
            List<string> result = new List<string>();
            foreach (var v in assetbundleLauchDesc.contents)
            {
                result.AddRange(v.assets);
            }
            return result;
        }

        public bool HasLoopRef() => loaderImp_.HasLoopRef();

        /// <summary>
        /// 创建一个AB包的Loader，只支持绝对路径
        /// </summary>
        /// <param name="path">assetbundle的路径</param>
        public static async Task<AssetPackageLoader> GetOrCreate(string path)
        {
            try
            {
                if (!sLoaders_.ContainsKey(path))
                {
                    var newLoader = new AssetPackageLoader(path);
                    sLoaders_.Add(path, newLoader);
                }

                var loader = sLoaders_[path];
                var res = await loader.LoadDescription();
                if (res != AssetPackageAssetLoadResult.kSuccess)
                {
                    Logger.Error($"AssetBundle with path:{path} init failed. error:{loader.error}");
                    return null;
                }

                return loader;
            }
            catch (Exception e)
            {
                Logger.LogExcpetion(e);
                return null;
            }
        }

        public static List<AssetPackageLoader> GetAllLoaders()
        {
            return new List<AssetPackageLoader>(sLoaders_.Values);
        }

        /// <summary>
        /// 清理所有的loader缓存
        /// </summary>
        public static void Release()
        {
            foreach (var v in sLoaders_)
            {
                v.Value.UnloadAsync(true);
            }

            sLoaders_.Clear();
        }
    }
}