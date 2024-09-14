using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wugou.Verify
{
    public class DlfGenerateWindow : EditorWindow
    {
        [MenuItem("Window/Wugou Tools/Verify/Dlf Generate", priority = 2000)]
        public static void ShowDlfEditor()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow wnd = GetWindow<DlfGenerateWindow>();
            wnd.titleContent = new GUIContent("Dlf Generate");

            // Limit size of the window
            wnd.minSize = new Vector2(450, 200);
            wnd.maxSize = new Vector2(1920, 720);
        }

        public void CreateGUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Wugou Dongle/Editor/UI/DlfGenerateWindow.uxml");
            VisualElement exportWndUXML = visualTree.Instantiate();
            rootVisualElement.Add(exportWndUXML);

            // version
            var versionInput = rootVisualElement.Q<TextField>("VersionInput");

            // validity
            var validityInput = rootVisualElement.Q<TextField>("ValidTimeInput");

            // mac
            var macInput = rootVisualElement.Q<TextField>("MACInput");

            // outPut
            var outputInput = rootVisualElement.Q<TextField>("OutputInput");

            // browser
            var browserButton = rootVisualElement.Q<Button>("BrowserButton");
            browserButton.clicked += () =>
            {
                outputInput.value = EditorUtility.OpenFolderPanel("dlf 生成目录", ".", "");
            };

            // generate
            rootVisualElement.Q<Button>("GenButton").clicked += () =>
            {
                //
                JObject jo = new JObject();
                int version = 0;
                if(!int.TryParse(versionInput.value, out version))
                {
                    EditorUtility.DisplayDialog("提示", "版本输入错误！", "确定");
                    return;
                }
                jo["version"] = versionInput.value;
                int validity = 0;
                if(!int.TryParse(validityInput.value,out validity))
                {
                    EditorUtility.DisplayDialog("提示", "有效期输入错误！","确定");
                    return;
                }
                var validDate = DateTime.Now;
                validDate = validDate.AddMonths(validity);   // 计算截止时间
                jo["validity"] = validDate.ToString();
                jo["mac"] = macInput.value;

                string encrptStr = RSA.Encrypt(jo.ToString(), $"./Config/verify/RSA.pub");
                var fileName = "dongle.dlf";
                var output = outputInput.value;
                if(string.IsNullOrEmpty(output))
                {
                    EditorUtility.DisplayDialog("提示", "输出路径不能为空！", "确定");
                    return;
                }
                File.WriteAllText($"{outputInput.value}/{fileName}", encrptStr);

                EditorUtility.DisplayDialog("提示", $"生成成功！文件存放在：{outputInput.value}/{fileName}", "确定");
                Close();
            };
        }
    }
}
