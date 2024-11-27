using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Wugou
{
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

        internal static async Task<Texture2D> LoadTextureAsync(string path)
        {
            return await Utils.LoadTextureAsync($"{path}");
        }

        public override async Task<T> GetAsset<T>(string path)
        {
            if (path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".jpeg"))
            {
                path = $"{this.path}/{path}";
                if (typeof(T) == typeof(Texture2D))
                {
                    return (await LoadTextureAsync(path)) as T;
                }
            }
            else
            {
                Wugou.Logger.Error($"Not Support {typeof(T).FullName} in LocalFileSystem..");
            }

            return null;
        }
    }
}