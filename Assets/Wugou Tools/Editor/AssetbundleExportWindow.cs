using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;
using System.IO;
using Wugou.Editor;

namespace Wugou.Assetbundle
{
    public class AssetbundleExportWindow : EditorWindow
    {
        public const string kPluginsPath = "Assets/Wugou tools";

        const string kHengDaoABPathStr = "HengDaoABPath";
        const string kBuildAssetBundleOptionStr = "BuildAssetBundleOption";
        const string kBuildTargetStr = "BuildTarget";
        const string kPluginsPlayerPrefStr = "Plugins";
        const string kVRPlayerPrefStr = "VRMode";
        const string kClearAssetBundlePrefStr = "ClearAB";

        [MenuItem(ConstDefines.MenuName + "/AssetBundle Export", priority = 2000)]
        public static void ShowExportEditor()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow wnd = GetWindow<AssetbundleExportWindow>();
            wnd.titleContent = new GUIContent("AssetBundleExport");

            // Limit size of the window
            wnd.minSize = new Vector2(450, 200);
            wnd.maxSize = new Vector2(1920, 720);
        }

        private class AssetBundleBuildPath
        {
            public string path;
            public bool willBuild;
        }

        public void CreateGUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(kPluginsPath + "/Editor/UI/AssetBundleExportWindow.uxml");
            VisualElement exportWndUXML = visualTree.Instantiate();
            rootVisualElement.Add(exportWndUXML);

            // folders
            List<string> excludeFolders = new List<string>()
            {
                "Editor",
                "Resources",
                "Scenes",
                "Scripts",
                "Plugins",
                "Standard Assets",
                "SteamingAssets",
                "Wugou Tools",
                "Wugou Multiplayer Game",
                "Gizmos",
                "Config"
            };

            List<AssetBundleBuildPath> pathsToBuild = new List<AssetBundleBuildPath>();

            // output direction
            var foldersView = rootVisualElement.Q<ScrollView>("ExportScrollView");
            DirectoryInfo dirInfo = new DirectoryInfo(Application.dataPath);
            foreach (var v in dirInfo.GetDirectories())
            {
                var elm = new Toggle(v.Name);
                if (excludeFolders.Contains(v.Name))
                {
                    continue;
                }

                AssetBundleBuildPath mark = new AssetBundleBuildPath();
                mark.path = v.Name;
                mark.willBuild = false;

                pathsToBuild.Add(mark);
                elm.RegisterValueChangedCallback((val) =>
                {
                    mark.willBuild = val.newValue;
                });


                foldersView.Add(elm);
            }


            // build options
            string lastBuildOption = PlayerPrefs.GetString(kBuildAssetBundleOptionStr, BuildAssetBundleOptions.None.ToString());
            string[] buildAssetBundleOps = System.Enum.GetNames(typeof(BuildAssetBundleOptions));
            DropdownField opsDropdownField = rootVisualElement.Q<DropdownField>("BuildOptions");
            opsDropdownField.choices = new List<string>(buildAssetBundleOps);
            opsDropdownField.SetValueWithoutNotify(lastBuildOption);

            // target
            string lastBuildTarget = PlayerPrefs.GetString(kBuildTargetStr, BuildTarget.StandaloneWindows.ToString());
            string[] targets = System.Enum.GetNames(typeof(BuildTarget));
            DropdownField targetDropdownField = rootVisualElement.Q<DropdownField>("BuildTarget");
            targetDropdownField.choices = new List<string>(targets);
            targetDropdownField.SetValueWithoutNotify(lastBuildTarget);

            // plugins
            TextField pluginsFiled = rootVisualElement.Q<TextField>("PluginsInput");
            string pluginsVal = PlayerPrefs.GetString(kPluginsPlayerPrefStr, string.Empty);
            pluginsFiled.value = pluginsVal;

            //output
            string cache = PlayerPrefs.GetString(kHengDaoABPathStr, string.Empty);
            TextField outputFiled = rootVisualElement.Q<TextField>("ExportPath");
            outputFiled.value = cache;

            // browser button
            Button browserBtn = rootVisualElement.Q<Button>("BrowserBtn");
            browserBtn.clicked += (() =>
            {
                string path = EditorUtility.OpenFolderPanel("Build Assetbundle", cache, "");
                outputFiled.value = path;
            });

            // vr assetbundle
            Toggle vrToggle = rootVisualElement.Q<Toggle>("VRToggle");
            vrToggle.value = PlayerPrefs.GetInt(kVRPlayerPrefStr, 0) == 1;

            var clearABToggle = rootVisualElement.Q<Button>("ClearOldBtn");
            clearABToggle.clicked += () =>
            {
                AssetbundleExport.ClearAndBuildAssetbundles(outputFiled.value);
            };

            // export Assetbundle
            var buildBtn = rootVisualElement.Q<Button>("BuildBtn");
            buildBtn.clicked +=(() =>
            {
                string outputPath = outputFiled.value;

                PlayerPrefs.SetString(kHengDaoABPathStr, outputPath);
                PlayerPrefs.SetString(kBuildAssetBundleOptionStr, opsDropdownField.value);
                PlayerPrefs.SetString(kBuildTargetStr, targetDropdownField.value);
                PlayerPrefs.SetString(kPluginsPlayerPrefStr, pluginsFiled.value);
                PlayerPrefs.SetInt(kVRPlayerPrefStr, vrToggle.value ? 1 : 0);

                // assign AB names
                Debug.Log("=====Assign assetbundle names======");
                foreach (var v in pathsToBuild)
                {
                    if (v.willBuild)
                    {
                        AssetbundleExport.AssignAssetBundleNameInFolder(v.path, v.path, AssetBundleAssetLoader.kPandaVariantName);
                    }
                }

                if (!string.IsNullOrEmpty(outputPath))
                {
                    if (!Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                    }

                    // start build
                    Debug.Log("=====start build======");
                    var tmpList = pluginsFiled.value.Split(";");
                    AssetbundleExport.plugins.Clear();
                    foreach (var v in tmpList)
                    {
                        if (!string.IsNullOrEmpty(v) && !AssetbundleExport.plugins.Contains(v))
                        {
                            AssetbundleExport.plugins.Add(v); 
                        }
                    }

                    AssetbundleExport.isBuildVRAssets = vrToggle.value; // VR
                    AssetbundleExport.ExcuteBuildAssetbundls(outputPath, System.Enum.Parse<BuildAssetBundleOptions>(opsDropdownField.value), System.Enum.Parse<BuildTarget>(targetDropdownField.value));
                    Debug.Log("=====build complet=====");

                    Close();
                }
            });

            var resetNameBtn = rootVisualElement.Q<Button>("ResetBtn");
            resetNameBtn.clicked += (() =>
            {
                AssetbundleExport.ResetAssetBundleNames();
            });

        }
    }
}
