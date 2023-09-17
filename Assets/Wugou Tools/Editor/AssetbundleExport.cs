using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;

namespace Wugou.Assetbundle
{

    public class AssetbundleExport
    {

        const string sceneSuffixStr_ = ".unity";

        public static List<string> plugins { get; set; } = new List<string>();

        public static bool isBuildVRAssets =  false;


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
        public static void ResetAssetBundleNames()
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

        private static void WriteLaunchInfoToFile(string path)
        {
            AssetBundleLauchDesc buildInfo = new AssetBundleLauchDesc();
            buildInfo.version = Application.unityVersion;
            buildInfo.vrAssets = isBuildVRAssets;

            string[] assetBundleNames = AssetDatabase.GetAllAssetBundleNames();
            for (int i = 0; i < assetBundleNames.Length; i++)
            {
                AssetBundleContent abContent = new AssetBundleContent();
                abContent.assetbundleName = assetBundleNames[i];

                string[] aFiles = AssetDatabase.GetAssetPathsFromAssetBundle(abContent.assetbundleName);
                abContent.assets = new List<string>(aFiles);

                buildInfo.contents.Add(abContent);
            }

            // 插件
            buildInfo.plugins = plugins;

            string jsonString = JsonConvert.SerializeObject(buildInfo);
            File.WriteAllText(Path.Combine(path, AssetBundleAssetLoader.kLauchDescFileName), jsonString);
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
            WriteLaunchInfoToFile(path);
        }

        private static string[] GetAllABScenesName()
        {
            List<string> scenes = new List<string>();
            foreach (var v in AssetDatabase.GetAllAssetBundleNames())
            {
                foreach (var s in AssetDatabase.GetAssetPathsFromAssetBundle(v))
                {
                    if (s.EndsWith("unity"))
                    {
                        string temp = s.Substring(s.LastIndexOf('/') + 1);
                        temp = temp.Substring(0, temp.LastIndexOf('.'));
                        scenes.Add(temp);
                    }
                }
            }

            return scenes.ToArray();
        }
    }
}
