using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Wugou
{
    /// <summary>
    /// 用于web资源
    /// </summary>
    public class WebFileSystem : LiteFileSystemBase
    {

        public WebFileSystem()
        {

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
                //var fileName = Path.GetFileName(path);
                //WebFileDesc webFileDesc = null;

                //// 先看有沒有緩存，緩存包括记录和实体文件
                //bool hasCache = filelist_.ContainsKey(path) && File.Exists($"{cachedPath_}/{filelist_[path].file}");

                //// 获取服务器文件信息，看是否需要更新
                //using (var request = UnityWebRequest.Head(path))
                //{
                //    await request.SendWebRequest();
                //    if (request.result == UnityWebRequest.Result.Success)
                //    {
                //        var modifiedStr = request.GetResponseHeader("last-modified");
                //        var modifiedTime = DateTime.Parse(modifiedStr);
                //        if (hasCache)
                //        {
                //            var oldModifiedTime = DateTime.Parse(filelist_[path].modifiedTime);
                //            if (DateTime.Compare(modifiedTime, oldModifiedTime) <= 0)   // 不需要更新，读取缓存
                //            {
                //                // 获取缓存图片
                //                var tt = await GetAssetInternal<T>($"{cachedPath_}/{filelist_[path].file}");
                //                return (tt);
                //            }
                //        }


                //        // 否则更新记录
                //        webFileDesc = new WebFileDesc()
                //        {
                //            uri = request.url,
                //            file = fileName,
                //            modifiedTime = request.GetResponseHeader("last-modified"),
                //            expiredTime = DateTime.Now.AddDays(2).ToString()
                //        };

                //    }
                //}

                //// 获取web图片
                //T asset = await GetAssetInternal<T>(path);

                //// 存储
                //if (asset)
                //{
                //    Texture2D tex = asset as Texture2D;
                //    if (tex)
                //    {
                //        var data = isJpg ? tex.EncodeToJPG() : tex.EncodeToPNG();
                //        File.WriteAllBytes($"{cachedPath_}/{fileName}", data);
                //    }
                //}
                //if (webFileDesc != null)
                //{
                //    filelist_[webFileDesc.uri] = webFileDesc;
                //}

                //return asset;

                TaskCompletionSource<object> taskSource = new TaskCompletionSource<object>();
                var dd = new HttpDownloader(path, path);
                dd.Start(async (path) =>
                {
                    var asset = await GetAssetInternal<T>(path);
                    taskSource.SetResult(asset);
                });

                return await taskSource.Task as T;
            }

            Wugou.Logger.Error($"Not support {path}");
            return null;
        }

        //public override void Unload()
        //{
        //    File.WriteAllText(cachedListFie_, JsonConvert.SerializeObject(new List<WebFileDesc>(filelist_.Values)), System.Text.Encoding.UTF8);
        //}
    }
}