using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wugou.UI;
using TMPro;

namespace Wugou.MapEditor
{
    public class GameMapBasicInfoPage : UIBaseWindow
    {
        // Start is called before the first frame update
        public virtual void Start()
        {
            // save page
            transform.Find("SaveButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                string filename = transform.Find("NameRow/NameInput").GetComponent<TMP_InputField>().text;
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
                    MapEditorSystem.instance.loadedGameMap.description = transform.Find("Main/Description").GetComponent<TMP_InputField>().text;
                    MapEditorSystem.instance.SaveMap(filename); 

                    Hide();
                }

            });
            transform.Find("CancelButton").GetComponent<Button>().onClick.AddListener(() =>
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
