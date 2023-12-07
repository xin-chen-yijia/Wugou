using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// AssetBundle ����
    /// </summary>
    public struct AssetBundleDesc
    {
        public string path;
    }

    /// <summary>
    /// ��assetbundle�е���������
    /// </summary>
    public struct AssetBundleAsset
    {
        public string asset;
        public string type;
        public AssetBundleDesc assetbundle;
    }

    /// <summary>
    /// ��assetbundle�еĳ�������
    /// </summary>
    public struct AssetBundleScene
    {
        public string sceneName;
        public AssetBundleDesc assetbundle;
    }

    /// <summary>
    /// ����UI�������,����UI����ʾ����ͼ��
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
