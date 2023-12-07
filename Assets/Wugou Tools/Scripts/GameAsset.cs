using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 游戏资产，
    /// </summary>
    public class GameAsset
    {
        public string name;
        public AssetBundleAsset asset;
        public string icon;
        public string description;
    }

    public static class GameAssetDatabase
    {
        private static Dictionary<string, GameAsset> assets_ = new Dictionary<string, GameAsset>();

        /// <summary>
        /// 是否是本地资源
        /// </summary>
        public static bool isLocalDrive { get; set; } = true;

        /// <summary>
        /// 资产库所在路径
        /// </summary>
        public static string resourceDir => GamePlay.settings.resourcePath;

        public static async void RegisterAssets(string filePath)
        {
            var content = await FileHelper.ReadText(filePath);
            List<GameAsset> assetList = JsonConvert.DeserializeObject<List<GameAsset>>(content, new AssetBundleDescConvert());
            RegisterAssets(assetList);
        }

        public static void RegisterAsset(GameAsset asset)
        {
            Debug.Assert(asset != null);
            assets_[asset.name] = asset;
        }

        public static void RegisterAssets(List<GameAsset> asset)
        {
            foreach(var v in asset)
            {
                RegisterAsset(v);
            }
        }

        public static GameAsset GetAsset(string name)
        {
            if (!string.IsNullOrEmpty(name) && assets_.ContainsKey(name))
            {
                return assets_[name];
            }

            return null;
        }

        public static Sprite GetAssetIcon(string assetName)
        {
            var sp = Utils.LoadSpriteFromFile(GetAssetIconFullPath(assetName), new Vector2(0.5f, 0.5f));
            return sp;
        }

        public static async Task<Sprite> GetAssetIconAsync(string assetName)
        {
            var tex = await Utils.LoadTextureFromFileAsync(GetAssetIconFullPath(assetName));

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            return sprite;
        }

        public static string GetAssetIconFullPath(string name)
        {
            var fullPath = !isLocalDrive ? resourceDir : Path.GetFullPath(resourceDir);
            return $"{fullPath}/{GetAsset(name).icon}";
        }

        public static async Task<T> LoadAssetAsync<T>(string assetName) where T : UnityEngine.Object
        {
            var asset = GetAsset(assetName);
            return await LoadAssetAsync<T>(asset);
        }

        public static async Task<T> LoadAssetAsync<T>(GameAsset gameAsset) where T : UnityEngine.Object
        {
            if(gameAsset == null)
            {
                return null;
            }

            var loader = await AssetBundleAssetLoader.GetOrCreate($"{resourceDir}/{gameAsset.asset.assetbundle.path}");
            var goMem = await loader.LoadAssetAsync<T>(gameAsset.asset.asset);

            return goMem;
        }
    }

}
