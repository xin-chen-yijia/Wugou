using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 游戏资产，包括Assetbundle和jpg等图片文件
    /// </summary>
    public class GameAsset
    {
        public string name;
        public string type;
        public string drive;    // 用于标识是ab包还是本地文件

        public GameAsset(string name, string type, string drive) 
        {
            this.name = name;
            this.type = type;
            this.drive = drive;
        }

        //public AssetBundleAsset asset;
        //public string icon;
        //public string description;
    }

}
