using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Wugou.Editor;

namespace Wugou.Assetbundle{
    /// <summary>
    /// AssetBundle 菜单栏
    /// </summary>
    public class AssetBundleMenus
    {
        [MenuItem(PluginGlobalDefine.MenuName + "/AssetBundle Export", priority = 2000)]
        public static void ShowExportEditor()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow wnd = EditorWindow.GetWindow<AssetbundleExportWindow>();
            wnd.titleContent = new GUIContent("AssetBundleExport");

            // Limit size of the window
            wnd.minSize = new Vector2(480, 360);
            wnd.maxSize = new Vector2(1920, 1920);
            wnd.autoRepaintOnSceneChange = true;
        }


        [MenuItem(PluginGlobalDefine.MenuName + "/AssetBundle Ref Check", priority = 2001)]
        public static async void AssetbundleRefCheck()
        {
            string path = EditorUtility.OpenFolderPanel("Check Assetbundle", ".", "");
            //string folder = Path.GetFileName(path);
            //var ab = AssetBundle.LoadFromFile($"{path}/{folder}");
            //AssetBundleManifest manifest = ab.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            //foreach (var v in manifest.GetAllAssetBundles())
            //{
            //    HashSet<string> deps = new HashSet<string>();
            //    //Debug.LogError($"开始检查 {v} 的引用！");
            //    if (!AssetbundleRefCheckImpl(manifest, v, deps))
            //    {
            //        break;
            //    }
            //}

            //ab.Unload(false);

            AssetPackageLoader.Release();
            var loader = await AssetPackageLoader.GetOrCreate(path);
            try
            {
                loader.LoadAssetbundle("Test"); // just for load MainManifest
            }
            catch
            {
                // do nothing
            }

            if (!loader.HasLoopRef())
            {
                // do nothing
                Debug.Log($"<b><color=green>{path} no loop ref!</color></b>");
            }
        }

    }
}

