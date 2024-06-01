using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Wugou.UI;
using TMPro;

namespace Wugou.Editor.UI
{
    public class GameMapBasicInfoPage : UIBaseWindow
    {
        public Button saveButton;
        public Button cancelButton;

        /// <summary>
        /// 保存后要执行的操作，比如退出编辑器
        /// </summary>
        public UnityEvent onSaveGameMap { get; private set; } = new UnityEvent();

        // Start is called before the first frame update
        public void Start()
        {
            // save page
            saveButton.onClick.AddListener(() =>
            {
                string filename = transform.Find("NameRow").GetComponentInChildren<TMP_InputField>().text;
                if (string.IsNullOrEmpty(filename))
                {
                    DaemonUI.makeSurePage.Tips("map's name can't be empty...");
                }
                else
                {
                    if (Gameplay.gameMapManager.Exists(filename))
                    {
                        DaemonUI.makeSurePage.Tips("map's name exists...");
                        return;
                    }

                    GameMapEditor.instance.loadedGameMap.name = filename;
                    GameMapEditor.instance.loadedGameMap.description = transform.Find("Main/Description").GetComponentInChildren<TMP_InputField>().text;
                   
                    if (GameMapEditor.instance.Save())
                    {
                        // 单次触发
                        onSaveGameMap.Invoke();

                        Hide();
                    }

                }

            });
            cancelButton.onClick.AddListener(() =>
            {
                Hide();
            });
        }

        // Update is called once per frame
        //void Update()
        //{

        //}
    }
}
