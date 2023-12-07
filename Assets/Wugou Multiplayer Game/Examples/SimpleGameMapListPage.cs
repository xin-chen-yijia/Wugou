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
using UnityEngine.Events;
using Wugou.MapEditor;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace Wugou.Examples.UI
{
    public class SimpleGameMapListPage : UIBaseWindow
    {
        public GameObject scriptItemPrefab;
        public GameObject scriptItemContainer;

        private GameMap activeMap_;

        public GameObject newMapButton;

        // Start is called before the first frame update
        void Start()
        {
            newMapButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                GameMap map = new GameMap();
                map.Parse("{}");
                map.scene = new AssetBundleScene() { sceneName = "ZZ", assetbundle = new AssetBundleDesc() { path = "assetbundles/ZZ" } };
                map.weather.time = 0.4f;
                map.version = GameMap.kLatestVersion;
                map.createTime = DateTime.Now.ToString();
                map.name = "new map";
                MapEditorSystem.StartEditor(map);
                Hide();
            });
        }


        // Update is called once per frame
        //void Update()
        //{

        //}

        public void Refresh()
        {
            var scripts = GameMapManager.GetAllGameMapFiles();

            activeMap_ = null;
            GameObject lastSelectRow = null;
            Utils.FillContent(scriptItemContainer, scriptItemPrefab, scripts, (GameObject item, string mapName) =>
            {
                item.gameObject.SetActive(true);
                item.name = mapName;
                var map = GameMapManager.GetGameMap(mapName);
                item.transform.Find("Name").GetComponent<TMP_Text>().text = map.name;
                item.transform.Find("Time").GetComponent<TMP_Text>().text = map.createTime;
                item.transform.Find("Author").GetComponent<TMP_Text>().text = map.author;

                float clickTime = -1;
                item.transform.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (lastSelectRow)
                    {
                        lastSelectRow.SetActive(false);
                    }
                    var checkedObj = item.transform.Find("Checked").gameObject;
                    checkedObj.SetActive(true);
                    lastSelectRow = checkedObj;

                    activeMap_ = map;

                    // double click
                    if(Time.realtimeSinceStartup - clickTime < 0.2f)
                    {
                        GamePlay.loadedGameMapFile = mapName;
                        MapEditorSystem.StartEditor(map);

                        Hide();
                    }

                    clickTime = Time.realtimeSinceStartup;
                });



                item.transform.Find("Edit").GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (map != null)
                    {
                        GamePlay.loadedGameMapFile = mapName;
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
                    },null, "是", "否");
                });
            });
        }

    }
}
