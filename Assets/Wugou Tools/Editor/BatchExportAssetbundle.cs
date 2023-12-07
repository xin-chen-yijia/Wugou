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
        /// ����AB��
        /// </summary>
        public static void Export()
        {
            string tt = "";
            string abFolder = GetParameter("-assetbundleFolder");
            tt += abFolder + " ";
            string exFolder = GetParameter("-exportFolder");
            tt += exFolder + "  ";
            //File.WriteAllText("d:/a.txt", tt);

            // ָ��Ҫ��������Դ·��
            AssetbundleExport.AssignAssetBundleNameInFolder(abFolder, abFolder, AssetBundleAssetLoader.kPandaVariantName);

            // ����AB��
            AssetbundleExport.ExcuteBuildAssetbundls(exFolder, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
            Debug.Log("=====build complet=====");
        }
    }
}
