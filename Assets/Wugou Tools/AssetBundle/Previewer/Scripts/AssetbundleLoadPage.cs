#if !UNITY_WEBGL
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

using System.Runtime.InteropServices;
using System;
using Ookii.Dialogs;
using System.Windows.Forms;
using System.IO;

using Wugou.UI;
using Wugou;

namespace Wugou.AssetbundlePreviewer
{
    public class AssetbundleLoadPage : UIBaseWindow
    {


        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        public class WindowWrapper : IWin32Window
        {
            private IntPtr _hwnd;
            public WindowWrapper(IntPtr handle) { _hwnd = handle; }
            public IntPtr Handle { get { return _hwnd; } }
        }

        private static string GetDirectoryPath(string directory)
        {
            var directoryPath = Path.GetFullPath(directory);
            if (!directoryPath.EndsWith("\\"))
            {
                directoryPath += "\\";
            }
            if (Path.GetPathRoot(directoryPath) == directoryPath)
            {
                return directory;
            }
            return Path.GetDirectoryName(directoryPath) + Path.DirectorySeparatorChar;
        }

        private List<Toggle> assetItems_ = new List<Toggle>();

        // Start is called before the first frame update
        void Start()
        {
            InputField pathInput = transform.Find("Main/Path/PathInput").GetComponent<InputField>();

            transform.Find("Main/Path/BrowserBtn").GetComponent<Button>().onClick.AddListener(() =>
            {
                // old style too ugly
                //FolderBrowserDialog dialog = new FolderBrowserDialog();
                //dialog.Description = "请选择Assetbundle所在文件夹";
                //dialog.SelectedPath = "./";
                //if (dialog.ShowDialog(new AssetbundleLoadPage.WindowWrapper(GetActiveWindow())) == DialogResult.OK)
                //{
                //    print(dialog.SelectedPath);
                //    transform.Find("Main/PathInput").GetComponent<TMP_InputField>().text = dialog.SelectedPath;
                //}

                //dialog.Dispose();

                // new style
                var fd = new VistaFolderBrowserDialog();
                fd.Description = "请选择Assetbundle所在文件夹";

                const string abPathPrefName = "AssetbundlePath";
                string dir = PlayerPrefs.GetString(abPathPrefName);
                fd.SelectedPath = dir ?? ".";
                var res = fd.ShowDialog(new WindowWrapper(GetActiveWindow()));
                if(res == DialogResult.OK)
                {
                    pathInput.GetComponent<InputField>().text = fd.SelectedPath;
                    fd.Dispose();

                    PlayerPrefs.SetString(abPathPrefName, fd.SelectedPath);

                    OnSummitInputPath(fd.SelectedPath);
                }

            });


            pathInput.GetComponent<InputField>().onSubmit.AddListener((string content) =>
            {

                OnSummitInputPath(content);

            });

            transform.Find("Main/LoadBtn").GetComponent<Button>().onClick.AddListener(() =>
            {
                List<string> resPaths = new List<string>();
                ScrollRect view = transform.GetComponentInChildren<ScrollRect>();
                foreach (var asset in view.content.GetComponentsInChildren<Toggle>())
                {
                    if (asset.GetComponent<Toggle>().isOn)
                    {
                        var path = asset.GetComponentInChildren<Text>().text;
                        resPaths.Add(path);
                    }
                }

                AssetbundlePreviewer.instance.LoadAssets(resPaths);

                Hide();
            });

            transform.Find("Main/CancelBtn").GetComponent<Button>().onClick.AddListener(() =>
            {
                Hide();
            });

            transform.Find("Details/All").GetComponent<Button>().onClick.AddListener(() =>
            {
                foreach (var v in assetItems_)
                {
                    v.isOn = true;
                }
            });

            transform.Find("Details/None").GetComponent<Button>().onClick.AddListener(() =>
            {
                foreach (var v in assetItems_)
                {
                    v.isOn = false;
                }
            });

        }

        /// <summary>
        /// 更新资源列表
        /// </summary>
        /// <param name="assets"></param>
        private async void OnSummitInputPath(string path)
        {
            Text tips = transform.Find("InputTips").GetComponent<Text>();
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                LogWindow.instance.Log($"{path} Assetbundle path empty or not exists....");
                tips.text = "Assetbundle path empty or not exists....";
                tips.color = Color.red;
                tips.gameObject.SetActive(true);
                return;
            }
            else
            {
                tips.gameObject.SetActive(false);
            }

            var loader = await AssetbundlePreviewer.instance.LoadAssetbundle(path);
            if(loader == null)
            {
                tips.text = "assetbundle not valid...";
                tips.color = Color.red;
                tips.gameObject.SetActive(true);
                return;
            }

            GameObject rowPrefab = transform.Find("Details/RowPrefab").gameObject;
            ScrollRect view = transform.GetComponentInChildren<ScrollRect>();
            // 清理 
            foreach(Transform v in view.content)
            {
                Destroy(v.gameObject);
            }
            assetItems_.Clear();

            foreach (var asset in AssetbundlePreviewer.instance.assetbundleLoader.GetAllConetents())
            {
                // 只加载场景和gameobject
                if (asset.EndsWith(".unity") || asset.EndsWith(".prefab"))
                {
                    GameObject item = Instantiate<GameObject>(rowPrefab, view.content);
                    item.GetComponentInChildren<Text>().text = asset;
                    item.SetActive(true);

                    item.GetComponent<Toggle>().isOn = false;
                    assetItems_.Add(item.GetComponent<Toggle>());
                }
            }

            // 根据内容多少扩展content高
            var contentTrans = view.content.GetComponent<RectTransform>();
            contentTrans.sizeDelta = new Vector2(contentTrans.sizeDelta.x, assetItems_.Count * 27);

            rowPrefab.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
#endif
