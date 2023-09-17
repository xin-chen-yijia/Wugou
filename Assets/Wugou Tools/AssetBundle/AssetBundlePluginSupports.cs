using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// ���֧�֣����ڼ����Դ���̺������̼����Ƿ�һ��
    /// </summary>
    [CreateAssetMenu(fileName = AssetBundlePluginSupports.fileName, menuName = "Wugou Tools/Plugins Supports")]
    public class AssetBundlePluginSupports : ScriptableObject
    {
        public const string fileName = "AssetBundlePluginSupports";
        public List<string> plugins = new List<string>();
    }

}
