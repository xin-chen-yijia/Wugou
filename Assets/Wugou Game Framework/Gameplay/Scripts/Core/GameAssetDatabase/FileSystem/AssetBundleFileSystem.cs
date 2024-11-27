using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Wugou
{
    /// <summary>
    /// 一个Assetbundle包当做一个文件系统
    /// </summary>
    public class AssetBundleFileSystem : LiteFileSystemBase
    {
        public AssetPackageLoader assetLoader { get; private set; }
        public string path { get; private set; }

        /// <summary>
        /// AB包文件夹
        /// </summary>
        /// <param name="path"></param>
        public AssetBundleFileSystem(string path)
        {
            this.path = path;
        }

        public override async Task<bool> Load()
        {
            assetLoader = await AssetPackageLoader.GetOrCreate(path);
            if (assetLoader == null)
            {
                Logger.Error($"{path} not exist..");
                return false;
            }

            return true;
        }

        public override void Unload()
        {
            assetLoader.Unload(true);
            assetLoader = null;
        }

        /// <summary>
        /// 加载sceneName所在的assetbundle
        /// </summary>
        /// <param name="sceneName"></param>
        public async Task<bool> LoadScene(string sceneName)
        {
            if (assetLoader != null)
            {
                var abName = assetLoader.GetAssetBundleByAssetName(sceneName);
                var ab = await assetLoader.LoadAssetbundleAsync(abName);

                return ab != null;
            }

            Logger.Error("GameAssetDatabase didn't loaded...");
            return false;
        }

        /// <summary>
        /// 加载assetbundle并获取assetbundle中的asset
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public override async Task<T> GetAsset<T>(string path)
        {
            if (assetLoader != null)
            {
                return await assetLoader.LoadAssetAsync<T>(path);
            }

            Logger.Error("GameAssetDatabase didn't loaded...");
            return null;
        }
    }

}