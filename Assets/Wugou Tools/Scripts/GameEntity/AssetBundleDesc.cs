using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// AssetBundle 描述
    /// </summary>
    public struct AssetBundleDesc
    {
        public string path;
    }

    /// <summary>
    /// 在assetbundle中的物体描述
    /// </summary>
    public struct AssetBundleAsset
    {
        public string asset;
        public string type;
        public AssetBundleDesc assetbundle;
    }

    /// <summary>
    /// 在assetbundle中的场景描述
    /// </summary>
    public struct AssetBundleScene
    {
        public string sceneName;
        public AssetBundleDesc assetbundle;
    }

    /// <summary>
    /// 场景UI相关内容,用于UI中显示场景图标
    /// </summary>
    public struct AssetBundleSceneCard
    {
        public string name;
        public List<string> tags;
        public string icon;
        public AssetBundleScene scene;
        public string description;
    }
}
