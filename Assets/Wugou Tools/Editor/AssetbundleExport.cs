using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using System;

namespace Wugou.Assetbundle
{

    public class AssetbundleExport
    {

        const string sceneSuffixStr_ = ".unity";

        /// <summary>
        /// assign assetbundle name to asset
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="assetBundleName"></param>
        /// <param name="variantName"></param>
        public static void AssignAssetBunleName(string assetPath, string assetBundleName, string variantName)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }
            if (assetPath.EndsWith(".cs"))  // 脚本不能导出
            {
                return;
            }
            AssetImporter ai = AssetImporter.GetAtPath(assetPath);
            if(ai != null)
            {
                string tmpName = assetBundleName;
                if (assetPath.EndsWith(sceneSuffixStr_))    //scene can't pack with assets
                {
                    tmpName = assetBundleName + "_scene";
                }
                ai.SetAssetBundleNameAndVariant(tmpName, variantName);
            }

        }

        /// <summary>
        /// name assetbundles in specified folder
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="assetBundleName"></param>
        /// <param name="variantName"></param>
        public static void AssignAssetBundleNameInFolder(string relativePath, string assetBundleName, string variantName)
        {
            DirectoryInfo folderInfo = new DirectoryInfo(Path.Combine(Application.dataPath, relativePath));
            foreach (var v in folderInfo.GetFiles())
            {
                if (v.Name.EndsWith(".meta"))
                {
                    continue;
                }

                AssetImporter ai = AssetImporter.GetAtPath("Assets/" + relativePath + "/" + v.Name);
                string tmpName = assetBundleName;
                if (v.Name.EndsWith(sceneSuffixStr_))    //scene can't pack with assets
                {
                    tmpName = assetBundleName + "_scene";
                }
                ai.SetAssetBundleNameAndVariant(tmpName, variantName);
            }

            foreach (var v in folderInfo.GetDirectories())
            {
                AssignAssetBundleNameInFolder(relativePath + "/" + v.Name, assetBundleName, variantName);
            }
        }

        /// <summary>
        /// 重置指定路径的assetbundle命名
        /// </summary>
        public static void ResetAllAssetBundleNames()
        {
            string[] assetBundleNames = AssetDatabase.GetAllAssetBundleNames();
            for (int i = 0; i < assetBundleNames.Length; i++)
            {
                string assetBundleName = assetBundleNames[i];
                string[] aFiles = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
                for (int j = 0; j < aFiles.Length; ++j)
                {
                    AssetImporter ai = AssetImporter.GetAtPath(aFiles[j]);
                    ai.SetAssetBundleNameAndVariant("", "");
                }
            }

            // clean assetbundle names
            AssetDatabase.RemoveUnusedAssetBundleNames();
        }

        private static void WriteDescriptionToFile(string path)
        {
            AssetBundleDescFile buildInfo = new AssetBundleDescFile();
            buildInfo.unityVersion = Application.unityVersion;
            buildInfo.wugouVersion = Wugou.PackageInformation.latestVersion;
            buildInfo.createTime = string.Format("{0}", DateTime.Now.ToLocalTime());

            string[] assetBundleNames = AssetDatabase.GetAllAssetBundleNames();
            for (int i = 0; i < assetBundleNames.Length; i++)
            {
                AssetBundleContent abContent = new AssetBundleContent();
                abContent.assetbundleName = assetBundleNames[i];

                string[] aFiles = AssetDatabase.GetAssetPathsFromAssetBundle(abContent.assetbundleName);
                abContent.assets = new List<string>(aFiles);
                buildInfo.contents.Add(abContent);
            }

            string jsonString = JsonConvert.SerializeObject(buildInfo);
            File.WriteAllText($"{path}/{Path.GetFileName(path)}{AssetPackageLoader.kDescFileNameSuffix}", jsonString);
        }

        /// <summary>
        /// 清理已构建的assetbundle
        /// </summary>
        public static void ClearAndBuildAssetbundles(string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                dir.Delete(true);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }

        /// <summary>
        /// 执行打包
        /// </summary>
        /// <param name="path"></param>
        public static void ExcuteBuildAssetbundls(string path, BuildAssetBundleOptions options = BuildAssetBundleOptions.None, BuildTarget target = BuildTarget.StandaloneWindows)
        {
            BuildPipeline.BuildAssetBundles(path, options, target);
            WriteDescriptionToFile(path);
        }
    }
}
