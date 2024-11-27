using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Wugou.Editor
{
    [InitializeOnLoad]
    public static class PackageLoad
    {
        static PackageLoad()
        {
            //AssetDatabase.importPackageCompleted += packageName =>
            //{
            //    if(packageName.StartsWith("Wugou Tools"))
            //    {
            //        // create streaming asset
            //        if (!Directory.Exists(Application.streamingAssetsPath))
            //        {
            //            Directory.CreateDirectory(Application.streamingAssetsPath);
            //        }

            //        if (!File.Exists($"{Application.streamingAssetsPath}/config.json"))
            //        {
            //            File.Copy($"{Application.dataPath}/Wugou Tools/Config/config-template.json", $"{Application.streamingAssetsPath}/config.json");
            //        }
            //    }

            //};
        }
    }
}
