using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
    public class AssetBundleContent
    {
        public string assetbundleName = string.Empty;
        public List<string> assets = new List<string>();
    }

    /// <summary>
    /// 自定义Assetbundle的资产描述信息，代替AssetbundleManifest使用
    /// </summary>
    public class AssetBundleDescFile
    {
        public string unityVersion = string.Empty;
        public string createTime = string.Empty;
        public List<AssetBundleContent> contents = new List<AssetBundleContent>();
    }

    #region Assetbundle Loader Implement

    /// <summary>
    /// 错误分类
    /// </summary>
    public enum AssetBundleAssetLoadResult
    {
        kSuccess = 0,
        kNetworkError,
        kDescriptionFileNotExist,
        kDescriptionFormatError,
    }

    internal abstract class AssetbundleLoaderBase
    {
        protected AssetBundleManifest mainManifest_ = null;

        //避免重复加载
        protected Dictionary<string, AssetBundle> loadedAssetbundles_ = new Dictionary<string, AssetBundle>();

        public AssetBundleDescFile assetbundleLauchDesc { get; protected set; }
        public abstract AssetBundle LoadAssetBundle(string path);
        public abstract Task<AssetBundle> LoadAssetBundleAsync(string path);
        public abstract Task<AssetBundleAssetLoadResult> LoadDescription();

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

    }

    /// <summary>
    /// web资源
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

            if (!mainManifest_)
            {
                string baseAbName = Path.GetFileName(rootPath);
                string manifestBundlePath = $"{rootPath}/{baseAbName}";
                AssetBundle manifestBundle = await LoadAssetBundleAsyncInternal(manifestBundlePath);
                mainManifest_ = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                manifestBundle.Unload(false);
            }

            string[] dps = mainManifest_.GetAllDependencies(path);
            foreach (var v in dps)
            {
                await LoadAssetBundleAsync(v);
            }

            var ab = await LoadAssetBundleAsyncInternal($"{rootPath}/{path}");
            loadedAssetbundles_.Add(path, ab);    // 加载完成

            return ab;
        }

        public override async Task<AssetBundleAssetLoadResult> LoadDescription()
        {
            Logger.Info($"load assetbundle: {rootPath}/{Path.GetFileName(rootPath)}{AssetPackageLoader.kDescFileNameSuffix}");
            UnityWebRequest www = UnityWebRequest.Get($"{rootPath}/{Path.GetFileName(rootPath)}{AssetPackageLoader.kDescFileNameSuffix}");
            await www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Logger.Error($"LoadABDescription: {www.error}");
                return AssetBundleAssetLoadResult.kNetworkError;
            }
            else
            {
                string configContent = www.downloadHandler.text;
                assetbundleLauchDesc = JsonConvert.DeserializeObject<AssetBundleDescFile>(configContent);
                if (assetbundleLauchDesc == null)
                {
                    Logger.Error("AssetBundle build info format error...");
                    return AssetBundleAssetLoadResult.kDescriptionFormatError;
                }

                Logger.DebugInfo($"{rootPath} build with:{assetbundleLauchDesc.unityVersion}");
                return AssetBundleAssetLoadResult.kSuccess;
            }
        }
    }

    /// <summary>
    /// 本地文件
    /// </summary>
    internal class LocalAssetbundleInternal : AssetbundleLoaderBase
    {
        public string rootPath { get; private set; }

        public LocalAssetbundleInternal(string path)
        {
            this.rootPath = path;
        }

        public override AssetBundle LoadAssetBundle(string path)
        {
            if (loadedAssetbundles_.ContainsKey(path))
            {
                return loadedAssetbundles_[path];
            }

            if (!mainManifest_)
            {
                string baseAbName = Path.GetFileName(rootPath);
                AssetBundle baseAb = AssetBundle.LoadFromFile($"{rootPath}/{baseAbName}");
                mainManifest_ = baseAb.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                baseAb.Unload(false);
            }

            string[] dps = mainManifest_.GetAllDependencies(path);
            foreach (var v in dps)
            {
                LoadAssetBundle(v);
            }

            var ab = AssetBundle.LoadFromFile($"{rootPath}/{path}");
            if (ab)
            {
                loadedAssetbundles_[path] = ab;
            }

            return ab;
        }

        public async Task<AssetBundle> LoadAssetBundleAsyncInternal(string path)
        {
            var aop = AssetBundle.LoadFromFileAsync(path);
            await aop;
            Debug.Assert(aop.assetBundle);

            return aop.assetBundle;
        }

        public override async Task<AssetBundle> LoadAssetBundleAsync(string path)
        {
            if (loadedAssetbundles_.ContainsKey(path))
            {
                return loadedAssetbundles_[path];
            }

            if (!mainManifest_)
            {
                string baseAbName = Path.GetFileName(rootPath);
                string manifestBundlePath = $"{rootPath}/{baseAbName}";
                AssetBundle manifestBundle = await LoadAssetBundleAsyncInternal(manifestBundlePath);
                mainManifest_ = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                manifestBundle.Unload(false);
            }

            string[] dps = mainManifest_.GetAllDependencies(path);
            foreach (var v in dps)
            {
                await LoadAssetBundleAsync(v);
            }

            var ab = await LoadAssetBundleAsyncInternal($"{rootPath}/{path}");
            loadedAssetbundles_.Add(path, ab);    // 加载完成

            return ab;
        }

        public override Task<AssetBundleAssetLoadResult> LoadDescription()
        {
            string descFile = $"{rootPath}/{Path.GetFileName(rootPath)}{AssetPackageLoader.kDescFileNameSuffix}";
            Logger.DebugInfo($"load assetbundle: {descFile}");

            if (!File.Exists(descFile))
            {
                Logger.Error($"Load description fail. {descFile} not exist.");
                return Task.FromResult(AssetBundleAssetLoadResult.kDescriptionFileNotExist);
            }
            else
            {
                string configContent = File.ReadAllText(descFile);
                assetbundleLauchDesc = JsonConvert.DeserializeObject<AssetBundleDescFile>(configContent);
                if (assetbundleLauchDesc == null)
                {
                    Logger.Error("AssetBundle build info format error...");
                    return Task.FromResult(AssetBundleAssetLoadResult.kDescriptionFormatError);
                }

                Logger.DebugInfo($"{rootPath} build with:{assetbundleLauchDesc.unityVersion}");
                return Task.FromResult(AssetBundleAssetLoadResult.kSuccess);
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

            MemoryStream stream = new MemoryStream(content);
            return AssetBundle.LoadFromStream(stream);
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

            MemoryStream stream = new MemoryStream(content);
            var aop = AssetBundle.LoadFromStreamAsync(stream);
            await aop;

            return aop.assetBundle;
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

        public override Task<AssetBundleAssetLoadResult> LoadDescription()
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

                            assetbundleLauchDesc = JsonConvert.DeserializeObject<AssetBundleDescFile>(Encoding.GetEncoding("utf-8").GetString(content));
                            return Task.FromResult(AssetBundleAssetLoadResult.kSuccess);
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

        private AssetBundleDescFile assetbundleLauchDesc => loaderImp_.assetbundleLauchDesc; //assetbundle 描述信息

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
            var bundle = await LoadAssetBundleAsync(bundleName);
            if (!bundle || !bundle.Contains(assetName))
            {
                Logger.Error($"Assetbundle '{assetbundleDir}' not contain's {assetName}");
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
        public async Task<AssetBundle> LoadAssetBundleAsync(string path)
        {
            Logger.DebugInfo($"Start load assetbundle {path}");
            if (string.IsNullOrEmpty(path))
            {
                Logger.Error("LoadAssetBundleAsync: empty bundle name");
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
        private async Task<AssetBundleAssetLoadResult> LoadDescription()
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
                if (res != AssetBundleAssetLoadResult.kSuccess)
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
            return sLoaders_.Values.ToList();
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