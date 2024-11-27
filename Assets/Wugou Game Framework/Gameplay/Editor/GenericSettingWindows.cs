using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wugou.Editor
{
    /// <summary>
    /// Õ®”√…Ë÷√
    /// </summary>
    public class GenericSettingWindows : EditorWindow
    {
        [MenuItem(PluginGlobalDefine.MenuName + "/Setting...", priority = 3001)]
        public static void ShowWindow()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow wnd = GetWindow<GenericSettingWindows>();
            wnd.titleContent = new GUIContent("Settings");

            // Limit size of the window
            wnd.minSize = new Vector2(450, 200);
            wnd.maxSize = new Vector2(1920, 720);
        }

        public void CreateGUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{PluginGlobalDefine.kPluginsPath}/Editor/UI/GenericSettingWindow.uxml");
            VisualElement exportWndUXML = visualTree.Instantiate();
            rootVisualElement.Add(exportWndUXML);


            // tags
            var tagView = rootVisualElement.Q<ListView>("TagView");
            var tags = new List<string>()
            {
                "StartPosition",
                "Axis",
            };
            tagView.itemsSource = tags;

            var importTagButton = rootVisualElement.Q<Button>("TagImportButton");
            importTagButton.clicked += () =>
            {
                for (int i = 0; i < tags.Count; ++i)
                {
                    if (UnityTagLayerManager.HasTag(tags[i]))
                    {
                        return; // exists, so not import
                    }
                }

                UnityTagLayerManager.AddTags(tags.ToArray());
            };

            var layerView = rootVisualElement.Q<ListView>("LayerView");
            var layers = new List<string>()
            {
                "MapEditor",
                "MapTool",
                "MapGizmos",
            };
            layerView.itemsSource = layers;

            // import layers
            var importLayerButton = rootVisualElement.Q<Button>("LayerImportButton");
            importLayerButton.clicked += () => {
                for(int i=0; i < layers.Count; ++i)
                {
                    int layer = LayerMask.NameToLayer(layers[i]);
                    if (layer != -1) // Layer already exists, so exit.
                        return;

                    UnityTagLayerManager.CreateLayer(layers[i]);
                }
            };
        }

    }
}

