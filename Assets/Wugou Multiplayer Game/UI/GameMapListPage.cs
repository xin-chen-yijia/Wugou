using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.UI;
using TMPro;
using UnityEngine.UI;
using Wugou;
using System.IO;
using Newtonsoft.Json;
using System;
using Wugou.Multiplayer;
using UnityEngine.Events;
using Wugou.MapEditor;
using UnityEngine.SceneManagement;

namespace Wugou.UI
{
    public class GameMapListPage : UIBaseWindow
    {
        public GameObject scriptItemPrefab;
        public GameObject scriptItemContainer;

        private GameMap activeMap_;

        // Start is called before the first frame update
        void Start()
        {
            transform.Find("NewScriptButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                rootWindow.GetChildWindow<GameMapTemplatePage>().SetContent(GameMapManager.GetAllGameMapTemplates());
                rootWindow.GetChildWindow<GameMapTemplatePage>().Show();
                Hide();
            });

            //transform.Find("LoadScriptButton").GetComponent<Button>().onClick.AddListener(() =>
            //{
            //    if (!string.IsNullOrEmpty(activeScriptName_))
            //    {
            //        RocketLauncherMonitorSystem.instance.StartTraining(activeScriptName_);
            //        Hide();
            //    }

            //});


        }


        // Update is called once per frame
        //void Update()
        //{

        //}

        public override void Show(bool asTop = false)
        {
            base.Show(asTop);
            Refresh();
        }

        public void Refresh()
        {
            var scripts = GameMapManager.GetAllGameMapFiles();

            activeMap_ = null;
            GameObject lastSelectRow = null;
            Utils.FillContent(scriptItemContainer, scriptItemPrefab, scripts, (GameObject item, string fileName) =>
            {
                item.gameObject.SetActive(true);
                item.name = fileName;
                var map = GameMapManager.DeserializeGameMapFromFile(fileName);
                item.transform.Find("Name").GetComponent<TMP_Text>().text = map.name;
                item.transform.Find("Time").GetComponent<TMP_Text>().text = map.createTime;
                item.transform.Find("Author").GetComponent<TMP_Text>().text = map.author;

                item.GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (lastSelectRow)
                    {
                        lastSelectRow.SetActive(false);
                    }
                    var checkedObj = item.transform.Find("Checked").gameObject;
                    checkedObj.SetActive(true);
                    lastSelectRow = checkedObj;

                    activeMap_ = map;
                });

                item.transform.Find("Edit").GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (map != null)
                    {
                        GamePlay.loadedGameMapFile = fileName;
                        MapEditorSystem.StartEditor(map);
                        Hide();
                    }

                });

                int tmpId = item.GetInstanceID();
                item.transform.Find("Delete").GetComponent<Button>().onClick.AddListener(() =>
                {
                    rootWindow.GetChildWindow<MakeSurePage>().ShowOptions($"确定删除{map.name}?", () =>
                    {
                        if (lastSelectRow && lastSelectRow.transform.parent.GetInstanceID() == tmpId)
                        {
                            lastSelectRow = null;
                            activeMap_ = null;
                        }

                        //删除脚本和记录
                        GameMapManager.RemoveGameMap(map.name);

                        //ui delete
                        GameObject.Destroy(item.gameObject);
                    });
                });

                // 选择第一个
                if (activeMap_ == null)
                {
                    scriptItemContainer.transform.GetChild(0).GetComponent<Button>().onClick.Invoke();
                }
            });
        }

    }
}
