using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Wugou.Assetbundle;

namespace Wugou.Editor
{
    public class BatchExportAssetbundle
    {
        private static string GetParameter(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for(int i=0; i < args.Length; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return "";
        }

        /// <summary>
        /// 构建AB包
        /// </summary>
        public static void Export()
        {
            string tt = "";
            string abFolder = GetParameter("-assetbundleFolder");
            tt += abFolder + " ";
            string exFolder = GetParameter("-exportFolder");
            tt += exFolder + "  ";
            //File.WriteAllText("d:/a.txt", tt);

            // 指定要导出的资源路径
            AssetbundleExport.AssignAssetBundleNameInFolder(abFolder, abFolder, AssetBundleAssetLoader.kPandaVariantName);

            // 构建AB包
            AssetbundleExport.ExcuteBuildAssetbundls(exFolder, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
            Debug.Log("=====build complet=====");
        }
    }
}
