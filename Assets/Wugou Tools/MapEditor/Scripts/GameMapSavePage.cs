using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Wugou.UI;
using TMPro;

namespace Wugou.MapEditor.UI
{
    public class GameMapSavePage : UIBaseWindow
    {
        public Button saveButton;
        public Button cancelButton;

        public TMP_InputField nameInput;

        // Start is called before the first frame update
        public void Start()
        {
            // save page
            saveButton.onClick.AddListener(() =>
            {
                string filename = transform.Find("NameRow").GetComponentInChildren<TMP_InputField>().text;
                if (string.IsNullOrEmpty(filename))
                {
                    rootWindow.GetChildWindow<MakeSurePage>().ShowTips("map's name can't be empty...");
                }
                else
                {
                    if (GameMapManager.ExistsGameMap(filename))
                    {
                        rootWindow.GetChildWindow<MakeSurePage>().ShowTips("map's name exists...");
                        return;
                    }

                    MapEditorSystem.instance.loadedGameMap.name = filename;                 
                    if (MapEditorSystem.instance.SaveGameMap(filename))
                    {
                        okAction_?.Invoke();
                        okAction_ = null;

                        Hide();
                    }

                }

            });
            cancelButton.onClick.AddListener(() =>
            {
                cancelAction_?.Invoke();
                cancelAction_ = null;

                Hide();
            });
        }

        // Update is called once per frame
        //void Update()
        //{

        //}

        private System.Action okAction_;
        private System.Action cancelAction_;
        public void Show(System.Action okAction, System.Action cancelAction)
        {
            base.Show(true);

            okAction_ = okAction;
            cancelAction_ = cancelAction;
        }
    }
}
