using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Wugou
{
    /// <summary>
    /// 用于统一Resource文件夹下的资源加载
    /// 注意，因为Resource是Unity管理的，且文件夹名称指定为了"Resources"
    /// </summary>
    public class ResourceFileSystem : LiteFileSystemBase
    {
        public override async Task<T> GetAsset<T>(string path)
        {
            Debug.Assert(path.Contains("."));

            path = path.Substring(0, path.IndexOf("."));

            var op = Resources.LoadAsync<T>(path);
            await op;

            return op.asset as T;
        }
    }
}