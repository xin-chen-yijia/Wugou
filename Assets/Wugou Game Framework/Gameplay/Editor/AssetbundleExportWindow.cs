using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO;
using System;

namespace Wugou.Editor
{
    public class AssetbundleExportWindow : EditorWindow
    {
        const string kOutputPathStr = "ABOutputPath";
        const string kBuildAssetBundleOptionStr = "BuildAssetBundleOption";
        const string kBuildTargetStr = "BuildTarget";
        const string kVRPlayerPrefStr = "VRMode";
        const string kClearAssetBundlePrefStr = "ClearAB";

        private class AssetItemInfo
        {
            public string path;
            public int level;   // 层级，用于折叠
            public bool folded;
            public bool isToggleOn;
            public AssetItemElement element;
        }
        private List<AssetItemInfo> allAssetItems_;

        private List<AssetItemInfo> activeItems_ = new List<AssetItemInfo>();

        public void CreateGUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PluginGlobalDefine.kPluginsPath + "/Editor/UI/AssetBundleExportWindow.uxml");
            VisualElement exportWndUXML = visualTree.Instantiate();

            rootVisualElement.Add(exportWndUXML);

            // export directory
            var assetsScrollView = rootVisualElement.Q<VisualElement>("ExportScrollView");

            ListView assetsTree = null;

            // The ListView calls this to add visible items to the scroller.
            Func<VisualElement> makeItem = () =>
            {
                var elm = new AssetItemElement();
                var foldBtn = elm.Q<Image>("foldoutBtn");
                foldBtn.RegisterCallback<ClickEvent>(evt => {
                    var i = (int)foldBtn.userData;
                    var assetItem = activeItems_[i];

                    assetItem.folded = !assetItem.folded;
                    foldBtn.style.backgroundImage = assetItem.folded ? new StyleBackground(AssetItemElement.foldoutSprite) : new StyleBackground(AssetItemElement.foldoutOnSprite);

                    // foldout
                    //for (int j = i + 1; j < activeItems_.Count; ++j)
                    //{
                    //    var tmp = activeItems_[j];
                    //    if (tmp.level <= assetItem.level)
                    //    {
                    //        break;
                    //    }

                    //    if (tmp.element != null)
                    //    {
                    //        tmp.element.style.display = new StyleEnum<DisplayStyle>(!assetItem.folded ? DisplayStyle.Flex : DisplayStyle.None);

                    //        // TODO：记录每一级的折叠情况
                    //        // 这里简单的展开所有
                    //        if (!assetItem.folded)
                    //        {
                    //            tmp.element.Q<Image>("foldoutBtn").style.backgroundImage = new StyleBackground(AssetItemElement.foldoutOnSprite);
                    //        }
                    //    }

                    //}

                    // update listview source
                    activeItems_.Clear();
                    int foldLevel = int.MaxValue;
                    for (int k = 0; k < allAssetItems_.Count; ++k)
                    {
                        if (allAssetItems_[k].level > foldLevel)    // folded
                        {
                            continue;
                        }
                        else
                        {
                            foldLevel = int.MaxValue;   // new folder
                        }

                        if (allAssetItems_[k].folded)
                        {
                            foldLevel = allAssetItems_[k].level;
                        }

                        activeItems_.Add(allAssetItems_[k]);    // add to list
                    }
                    assetsTree.itemsSource = activeItems_;
                    assetsTree.Rebuild();
                });

                var toggle = elm.Q<Toggle>();
                toggle.RegisterValueChangedCallback(evt =>
                {
                    var i = (int)foldBtn.userData;
                    var assetItem = activeItems_[i];
                    assetItem.isToggleOn = toggle.value;

                    // toggle
                    for (int j = i+1; j < activeItems_.Count; ++j)
                    {
                        var tmp = activeItems_[j];
                        if (tmp.level <= assetItem.level)
                        {
                            break;
                        }

                        tmp.isToggleOn = toggle.value;
                        if(tmp.element != null)
                        {
                            tmp.element.Q<Toggle>().SetValueWithoutNotify(toggle.value);
                        }

                    }
                    
                    // apply to allAssetItems
                    for(int j=0;j<allAssetItems_.Count; ++j)
                    {
                        if (allAssetItems_[j].path == assetItem.path)
                        {
                            for (int k = j+1; k < allAssetItems_.Count; ++k)
                            {
                                var tmp = allAssetItems_[k];
                                if (tmp.level <= assetItem.level)
                                {
                                    break;  // new folder
                                }

                                tmp.isToggleOn = toggle.value;
                            }

                            break;
                        }
                    }
                });
                return elm;
            };

            // The ListView calls this if a new item becomes visible when the item first appears on the screen, 
            // when a user scrolls, or when the dimensions of the scroller are changed.
            Action<VisualElement, int> bindItem = (e, i) =>
            {
                var asset = activeItems_[i];
                bool isDir = Directory.Exists(asset.path);

                // foldout
                var elm = e as AssetItemElement;
                var foldBtn = elm.Q<Image>("foldoutBtn");
                foldBtn.userData = i;
                foldBtn.style.backgroundImage = asset.folded ? new StyleBackground(AssetItemElement.foldoutSprite) : new StyleBackground(AssetItemElement.foldoutOnSprite);
                foldBtn.style.display = new StyleEnum<DisplayStyle>(isDir ? DisplayStyle.Flex : DisplayStyle.None);

                // 
                asset.element = elm;

                // text
                var label = elm.Q<Label>();
                var filePath = Path.GetRelativePath(new DirectoryInfo(Application.dataPath).Parent.FullName, asset.path);
                filePath = filePath.Replace('\\', '/');
                label.text = filePath;

                // icon
                var iconElm = elm.Q<Image>("assetIcon");
                if (isDir)
                {
                    iconElm.sprite = AssetItemElement.folderIcon;
                }
                else
                {
                    var tmpAssetPath = Path.GetRelativePath(Directory.GetParent(Application.dataPath).FullName, asset.path);
                    var tex = AssetDatabase.GetCachedIcon(tmpAssetPath) as Texture2D;
                    if(tex != null)
                    {
                        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        iconElm.sprite = sprite;
                    }
                    else
                    {
                        Debug.LogError($"{tmpAssetPath} not have cached icon..");
                    }

                }

                // toggle
                var toggle = elm.Q<Toggle>();
                toggle.SetValueWithoutNotify(asset.isToggleOn);

                // indent
                elm.style.left = asset.level * 20;
            };

            // 所有资产
            allAssetItems_ = TraverseDirectory(new DirectoryInfo("./Assets"), 0);
            activeItems_.Clear();
            for (int i=0;i<allAssetItems_.Count;i++)
            {
                activeItems_.Add(allAssetItems_[i]);
            }
            assetsTree = new ListView(activeItems_, 20,makeItem,bindItem);
            //assetsPanel.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            //assetsPanel.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column);
            assetsScrollView.Add(assetsTree);



            // 按类型查找
            //var guids = AssetDatabase.FindAssets("t:" + typeof(Object).Name, new[] { "Assets" });
            //foreach (var guid in guids)
            //{
            //    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            //    Debug.Log(assetPath);
            //}

            // build options
            string lastBuildOption = EditorPrefs.GetString(kBuildAssetBundleOptionStr, BuildAssetBundleOptions.None.ToString());
            string[] buildAssetBundleOps = System.Enum.GetNames(typeof(BuildAssetBundleOptions));
            DropdownField opsDropdownField = rootVisualElement.Q<DropdownField>("BuildOptions");
            opsDropdownField.choices = new List<string>(buildAssetBundleOps);
            opsDropdownField.SetValueWithoutNotify(lastBuildOption);

            // target
            string lastBuildTarget = EditorPrefs.GetString(kBuildTargetStr, BuildTarget.StandaloneWindows.ToString());
            string[] targets = System.Enum.GetNames(typeof(BuildTarget));
            DropdownField targetDropdownField = rootVisualElement.Q<DropdownField>("BuildTarget");
            targetDropdownField.choices = new List<string>(targets);
            targetDropdownField.SetValueWithoutNotify(lastBuildTarget);

            //output
            string cache = EditorPrefs.GetString(kOutputPathStr, Application.dataPath);
            TextField outputFiled = rootVisualElement.Q<TextField>("ExportPath");
            outputFiled.value = cache;

            // browser button
            Button browserBtn = rootVisualElement.Q<Button>("BrowserBtn");
            browserBtn.clicked += (() =>
            {
                string path = EditorUtility.OpenFolderPanel("Build Assetbundle", outputFiled.value, "");
                outputFiled.value = path;
            });

            // Encrypt
            Toggle encryptToggle = rootVisualElement.Q<Toggle>("EncryptToggle");

            var clearABToggle = rootVisualElement.Q<Button>("ClearOldBtn");
            clearABToggle.clicked += () =>
            {
                AssetbundleExport.ClearAssetbundles(outputFiled.value);
            };

            // export Assetbundle
            var buildBtn = rootVisualElement.Q<Button>("BuildBtn");
            buildBtn.clicked +=(() =>
            {
                string outputPath = outputFiled.value;

                EditorPrefs.SetString(kOutputPathStr, outputPath);
                EditorPrefs.SetString(kBuildAssetBundleOptionStr, opsDropdownField.value);
                EditorPrefs.SetString(kBuildTargetStr, targetDropdownField.value);

                // assign assetbundle name
                Debug.Log("=====Assign assetbundle names======");
                AssetbundleExport.ResetAllAssetBundleNames();
                foreach (var asset in allAssetItems_)
                {
                    if (asset.isToggleOn)
                    {
                        if (!Directory.Exists($"{asset.path}"))  // 非目录
                        {
                            var assetPath = Path.GetRelativePath(Directory.GetParent(Application.dataPath).FullName, asset.path);
                            assetPath = assetPath.Replace('\\', '/');
                            var dir = Path.GetDirectoryName(assetPath);
                            AssetbundleExport.AssignAssetBunleName(assetPath, dir, AssetPackageLoader.kPandaVariantName);
                        }

                    }
                }

                if (!string.IsNullOrEmpty(outputPath))
                {
                    if (!Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                    }

                    // start build
                    Debug.Log($"<color=green>=====Start building. Encrypt:{encryptToggle.value}======</color>");
                    AssetbundleExport.ExcuteBuildAssetbundls(outputPath, System.Enum.Parse<BuildAssetBundleOptions>(opsDropdownField.value), System.Enum.Parse<BuildTarget>(targetDropdownField.value), encryptToggle.value);

                    Debug.Log("<color=green>=====Build Assetbundle Complete====</color>");

                    Close();
                }
                else
                {
                    Debug.Log("<color=green>=====Output path can't be empty..=====</color>");
                }
            });

            var resetNameBtn = rootVisualElement.Q<Button>("ResetBtn");
            resetNameBtn.clicked += (() =>
            {
                AssetbundleExport.ResetAllAssetBundleNames();
            });

        }

        private class AssetItemElement : VisualElement
        {
            public static Sprite folderIcon;
            public static Sprite foldoutSprite = null;
            public static Sprite foldoutOnSprite = null;

            public AssetItemElement()
            {
                var root = new VisualElement();

                // The code below to style the ListView is for demo purpose. It's better to use a USS file
                // to style a visual element. 
                root.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                root.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

                if (!foldoutSprite)
                {
                    //var folderPath = Path.GetRelativePath(Directory.GetParent(Application.dataPath).FullName, info.FullName);
                    //var folderTex = AssetPreview.GetMiniThumbnail(AssetDatabase.LoadMainAssetAtPath(folderPath));
                    var folderTex = EditorGUIUtility.FindTexture("Folder Icon");
                    if (folderTex)
                    {
                        folderIcon = Sprite.Create(folderTex, new Rect(0, 0, folderTex.width, folderTex.height), new Vector2(0.5f, 0.5f));
                    }


                    var foldoutTex = EditorGUIUtility.IconContent("d_IN_foldout_act").image as Texture2D;
                    if (foldoutTex != null)
                    {
                        foldoutSprite = Sprite.Create(foldoutTex, new Rect(0, 0, foldoutTex.width, foldoutTex.height), new Vector2(0.5f, 0.5f));
                    }

                    var folderOutOnTex = EditorGUIUtility.IconContent("d_IN_foldout_act_on").image as Texture2D;
                    if (folderOutOnTex != null)
                    {
                        foldoutOnSprite = Sprite.Create(folderOutOnTex, new Rect(0, 0, folderOutOnTex.width, folderOutOnTex.height), new Vector2(0.5f, 0.5f));
                    }
                }

                // folder
                var foldoutBtn = new Image();
                foldoutBtn.name = "foldoutBtn";
                foldoutBtn.style.backgroundImage = new StyleBackground(foldoutOnSprite);
                foldoutBtn.style.width = 15;
                foldoutBtn.style.height = 15;
                root.Add(foldoutBtn);
                bool isOn = false;
                foldoutBtn.RegisterCallback<ClickEvent>((e) =>
                {
                    isOn = !isOn;
                    foldoutBtn.style.backgroundImage = isOn ? new StyleBackground(foldoutOnSprite) : new StyleBackground(foldoutSprite);
                });

                //
                var assetToggle = new Toggle();
                root.Add(assetToggle);

                var image = new Image();
                image.name = "assetIcon";
                image.sprite = folderIcon;
                image.style.width = 15;
                image.style.height = 15;
                root.Add(image);

                //
                var lable = new Label();
                root.Add(lable);

                Add(root);
            }
        }

        private List<AssetItemInfo> TraverseDirectory(DirectoryInfo dir, int level) 
        {
            if (!dir.Exists)
            {
                return new List<AssetItemInfo>(); 
            }

            List<AssetItemInfo> items = new List<AssetItemInfo>();
            items.Add(new AssetItemInfo()
            {
                path = dir.FullName,
                level = level,
            });


            var files = dir.GetFileSystemInfos();
            foreach(var v in files)
            {
                if(v is DirectoryInfo)
                {
                    if (v.Name.EndsWith("~"))
                    {
                        continue;
                    }
                    items.AddRange(TraverseDirectory(v as DirectoryInfo, level+1));
                }
                else
                {
                    if (v.Name.EndsWith(".meta") || v.Name.EndsWith(".cs"))  // cs script can't be build in ab
                    {
                        continue;
                    }

                    items.Add(new AssetItemInfo()
                    {
                        path = v.FullName,
                        level=level+1,
                    });
                }
            }

            return items;

        }

        //private static bool AssetbundleRefCheckImpl(AssetBundleManifest manifest, string bundleName, HashSet<string> deps)
        //{
        //    if (deps.Contains(bundleName))
        //    {
        //        Debug.LogError($"{bundleName} 出现了循环引用！");
        //        foreach(var dep in deps)
        //        {
        //            Debug.LogError($"{dep}");
        //        }
        //        return false;
        //    }

        //    deps.Add(bundleName);

        //    foreach (var v in manifest.GetAllDependencies(bundleName))
        //    {
        //        if(!AssetbundleRefCheckImpl(manifest, v, deps))
        //        {
        //            return false;
        //        }
        //    }

        //    deps.Remove(bundleName);
        //    return true;
        //}

        //List<AssetItemInfo> CreateAssetTree(VisualElement parent, DirectoryInfo dir)
        //{
        //    //VisualElement folderPanel = prefab.Instantiate();
        //    //Foldout foldout = folderPanel.Q<Foldout>();
        //    //foldout.text = dir.Name;
        //    //folderPanel.styleSheets.Add(style);
        //    //parent.Add(folderPanel);

        //    // 文件夹的勾选
        //    //var assetToggle = folderPanel.Q<Toggle>();



        //    //var folder = new AssetNode(dir);
        //    //parent.Add(folder);


        //    //var dirPath = Path.GetRelativePath(Directory.GetParent(Application.dataPath).FullName, dir.FullName);
        //    var dirPath = Path.GetRelativePath(Application.dataPath, dir.FullName);
        //    dirPath = dirPath.Replace('\\', '/');
        //    List<AssetItemInfo> items = new List<AssetItemInfo>();
        //    items.Add(new AssetItemInfo()
        //    {
        //        path = dirPath,
        //        //toggle = assetToggle
        //    });

        //    var ss = dir.GetFileSystemInfos();
        //    foreach (var v in ss)
        //    {
        //        if (v is DirectoryInfo)
        //        {

        //            var list = CreateAssetTree(folder, v as DirectoryInfo);
        //            items.AddRange(list);
        //        }
        //        else
        //        {
        //            if (v.Name.EndsWith(".meta"))
        //            {
        //                continue;
        //            }

        //            var panel = new VisualElement();
        //            panel.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        //            panel.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
        //            //panel.style.position = new StyleEnum<Position>(Position.Relative);
        //            //panel.style.left = 20;

        //            var toggle = new Toggle();
        //            panel.Add(toggle);

        //            var preImg = new Image();
        //            var filePath = Path.GetRelativePath(Application.dataPath, v.FullName);
        //            filePath = filePath.Replace('\\', '/');
        //            var assetPath = Path.GetRelativePath(Directory.GetParent(Application.dataPath).FullName, v.FullName);
        //            var tex = AssetDatabase.GetCachedIcon(assetPath) as Texture2D;
        //            if (tex != null)
        //            {
        //                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        //                preImg.sprite = sprite;
        //                preImg.style.width = 15;
        //                preImg.style.height = 15;
        //                panel.Add(preImg);
        //            }


        //            var label = new Label(v.Name);
        //            label.style.backgroundImage = new StyleBackground();
        //            panel.Add(label);

        //            //foldout.Add(panel);

        //            items.Add(new AssetItemInfo()
        //            {
        //                path = filePath,
        //                toggle = toggle
        //            });
        //        }
        //    }

        //    //assetToggle.RegisterCallback<ChangeEvent<bool>>((val) =>
        //    //{
        //    //    foreach (var v in items)
        //    //    {
        //    //        Debug.Log(v.path);
        //    //        v.toggle.value = (val.newValue);
        //    //    }
        //    //});
        //    return items;
        //}
    }
}
