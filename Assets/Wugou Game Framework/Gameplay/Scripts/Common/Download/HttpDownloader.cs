using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Newtonsoft.Json;

namespace Wugou
{
    /// <summary>
    /// 用于下载文件,文件存储在temp文件夹
    /// </summary>
    public class HttpDownloader : IDownloader
    {
        private static string cachedPath_ => $"{GameConsole.cachePath}/web";
        private static string cachedListFie_ => $"{cachedPath_}/.cache";

        /// <summary>
        /// 缓存信息
        /// </summary>
        public class WebFileCache
        {
            public string uri { get; }
            public string fileName { get; }
            public string modifiedTime { get; }
            public string expiredTime { get; }

            [JsonIgnore]
            public string fullPath => $"{cachedPath_}/{fileName}";

            public  WebFileCache(string uri, string fileName, string modifiedTime, string expiredTime)
            {
                this.uri = uri;
                this.fileName = fileName;
                this.modifiedTime = modifiedTime;
                this.expiredTime = expiredTime;
            }
        }

        private static Dictionary<string, WebFileCache> _fileList = null;
        /// <summary>
        /// 缓存的文件
        /// </summary>
        private static Dictionary<string, WebFileCache> fileCacheList {
            get
            {
                if(_fileList == null)
                {
                    _fileList = new Dictionary<string, WebFileCache>();

                    Directory.CreateDirectory(cachedPath_);

                    // 检查缓存
                    if (File.Exists(cachedListFie_))
                    {
                        var files = JsonConvert.DeserializeObject<List<WebFileCache>>(File.ReadAllText(cachedListFie_, System.Text.Encoding.UTF8));

                        files.RemoveAll((cache) =>
                        {
                            var t = DateTime.Parse(cache.expiredTime);
                            var valid = (DateTime.Compare(t, DateTime.Now) < 0);  
                            if(valid) // 剔除过期的
                            {
                                File.Delete(cache.fullPath);
                                return true;
                            }

                            return false;
                        });

                        for (int i = 0; i < files.Count; i++)
                        {
                            _fileList.Add(files[i].uri, files[i]);
                        }

                        //var remainList = new List<WebFileCache>();
                        //for (int i = 0; i < files.Count; i++)
                        //{
                        //    var t = DateTime.Parse(files[i].expiredTime);
                        //    if (DateTime.Compare(t, DateTime.Now) < 0)  // 剔除过期的
                        //    {
                        //        File.Delete($"{cachedPath_}/{files[i].fileName}");
                        //    }
                        //    else
                        //    {
                        //        remainList.Add(files[i]);
                        //    }
                        //}

                        //for (int i = 0; i < remainList.Count; i++)
                        //{
                        //    _fileList.Add(remainList[i].uri, remainList[i]);
                        //}
                    }

                    lastCacheCount_ = _fileList.Count;
                }

                return _fileList;
            }
        }

        private static int lastCacheCount_ = -1;  
        private static bool isWritingCacheFile_ = false; // 用于缓存记录写文件

        public string name { get; private set; }

        public string uri { get; private set; }

        private UnityWebRequest request_;
        public float progress { get; private set; }

        public bool isDone { get; private set; }

        private int _result = -1;
        //public int result => _result == (int)UnityWebRequest.Result.Success ? 0 : 1;
        public int result { get; private set; }

        public string errorMessage { get; private set; }

        public HttpDownloader(string name, string uri) 
        {
            this.name = name;
            this.uri = uri;
        }

        /// <summary>
        /// 开始下载
        /// </summary>
        /// <param name="onComplete"></param>
        public void Start(Action<string> onComplete)
        {
            CoroutineLauncher.active.StartCoroutine(DownloadContentCoroutine(uri, onComplete));
        }

        private IEnumerator DownloadContentCoroutine(string path, Action<string> onComplete)
        {
            var fileName = Path.GetFileName(path);

            // 先看有沒有緩存，緩存包括记录和实体文件
            //bool hasCache = filelist.ContainsKey(path) && File.Exists($"{cachedPath_}/{filelist[path].file}");

            // 先用Head协议获取文件信息，看是否有更新
            using (var request = UnityWebRequest.Head(path))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var modifiedStr = request.GetResponseHeader("last-modified");
                    var modifiedTime = DateTime.Parse(modifiedStr);
                    var fileCache = GetCache(path); // 查看缓存
                    if (fileCache != null)
                    {
                        var oldModifiedTime = DateTime.Parse(fileCache.modifiedTime);
                        if (DateTime.Compare(modifiedTime, oldModifiedTime) <= 0)   // 不需要更新，读取缓存
                        {
                            onComplete?.Invoke(fileCache.fullPath);
                            yield break;
                        }
                    }
                }
                else
                {
                    Logger.Error($"{request.error}");
                    yield break;
                }
            }

            using (UnityWebRequest webRequest = UnityWebRequest.Get(path))
            {
                request_ = webRequest;

                var aop = webRequest.SendWebRequest();
                while (!aop.isDone)
                {
                    progress = aop.progress;
                    yield return null;
                }
                isDone = true;

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;

                _result = (int)webRequest.result;
                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        errorMessage = webRequest.error;
                        Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        errorMessage = $"HTTP Error: {errorMessage}";
                        Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        //Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
                        //Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadedBytes);


                        var bytes = webRequest.downloadHandler.data;
                        var filePath = $"{cachedPath_}/{pages[page]}";
                        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            fs.Write(bytes, 0, bytes.Length);
                            fs.Close();

                            onComplete?.Invoke(filePath);

                            // 否则更新记录
                            var cache = new WebFileCache(webRequest.url, fileName, webRequest.GetResponseHeader("last-modified"), DateTime.Now.AddDays(2).ToString());
                            fileCacheList[cache.uri] = cache;

                            // 针对有大量下载同时进行的时候，避免短时间内大量的写文件操作
                            if (!isWritingCacheFile_)
                            {
                                isWritingCacheFile_ = true;
                                yield return new WaitForSeconds(5);
                                isWritingCacheFile_ = false;
                                WriteCache();
                            }
                        }

                        break;
                }

                request_ = null;
            }
        }

        public void Abort()
        {
            if(request_ != null)
            {
                request_.Abort();
                request_ = null;
            }
        }

        public void Cancel()
        {
            Abort();
        }

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static WebFileCache GetCache(string path)
        {
            if (fileCacheList.ContainsKey(path))
            {
                var cache = $"{cachedPath_}/{fileCacheList[path].fileName}";
                if (File.Exists(cache))
                {
                    return fileCacheList[path];
                }
            }

            return null;
        }

        /// <summary>
        /// 缓存记录写入文件
        /// </summary>
        public static void WriteCache() 
        {
            if (lastCacheCount_ != fileCacheList.Count)
            {
                lastCacheCount_ = fileCacheList.Count;
                File.WriteAllText(cachedListFie_, JsonConvert.SerializeObject(fileCacheList.Values));
            }
        }
    }
}
