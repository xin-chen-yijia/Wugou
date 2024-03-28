using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Wugou.Editor;
using Wugou.UI;

namespace Wugou.Examples.AssetbundlePreviewer
{

    public class MenuBarPage : UIBaseWindow
    {
        public UnityEngine.UI.Button openButton;
        // Start is called before the first frame update
        void Start()
        {
            openButton.onClick.AddListener(() =>
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


                const string abPathPrefName = "AssetbundlePath";
                string dir = PlayerPrefs.GetString(abPathPrefName);
                string path = FolderBrowserDialog.Open(dir, "请选择Assetbundle所在文件夹");
                if(!string.IsNullOrEmpty(path))
                {
                    PlayerPrefs.SetString(abPathPrefName, path);

                    AssetbundlePreviewer.instance.LoadAssetBundle(path);
                }
            });
        }

        // Update is called once per frame
        //void Update()
        //{

        //}
    }
}

