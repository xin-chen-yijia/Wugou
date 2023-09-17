using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 插件支持，用于检查资源工程和主工程间插件是否一致
    /// </summary>
    [CreateAssetMenu(fileName = AssetBundlePluginSupports.fileName, menuName = "Wugou Tools/Plugins Supports")]
    public class AssetBundlePluginSupports : ScriptableObject
    {
        public const string fileName = "AssetBundlePluginSupports";
        public List<string> plugins = new List<string>();
    }

}
