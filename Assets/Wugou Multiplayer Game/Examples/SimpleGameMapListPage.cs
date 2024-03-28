using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wugou.Editor;
using Wugou.UI;

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
                GameMap map = GameMap.Create();
                map.Parse("{}");
                map.scene = "/ZZ/ZZ.unity";
                map.weather.time = 0.4f;
                map.version = GameMap.kLatestVersion;
                map.createTime = DateTime.Now.ToString();
                map.name = "new map";

                var proj = new GameMapProj($"{Gameplay.gameMapProjsPath}/test");
                proj.gameMap = map;
                GameMapEditor.StartEditor(proj);

                Hide();
            });
        }


        // Update is called once per frame
        //void Update()
        //{

        //}

        public void Refresh()
        {
            var projs = Gameplay.gameMapProjManager.GetAllNames();

            activeMap_ = null;
            GameObject lastSelectRow = null;
            Utils.FillContent(scriptItemContainer, scriptItemPrefab, projs, (GameObject item, string mapProjName) =>
            {
                item.gameObject.SetActive(true);
                item.name = mapProjName;
                var mapProj = Gameplay.gameMapProjManager.Get(mapProjName);
                item.transform.Find("Name").GetComponent<TMP_Text>().text = mapProj.name;
                item.transform.Find("Time").GetComponent<TMP_Text>().text = mapProj.gameMap.createTime;
                item.transform.Find("Author").GetComponent<TMP_Text>().text = mapProj.gameMap.author;

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

                    activeMap_ = mapProj.gameMap;

                    // double click
                    if(Time.realtimeSinceStartup - clickTime < 0.2f)
                    {
                        Gameplay.loadedGameMapFile = mapProjName;
                        Editor.GameMapEditor.StartEditor(mapProj);

                        Hide();
                    }

                    clickTime = Time.realtimeSinceStartup;
                });



                item.transform.Find("Edit").GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (mapProj != null)
                    {
                        Gameplay.loadedGameMapFile = mapProjName;
                        Editor.GameMapEditor.StartEditor(mapProj);
                        Hide();
                    }

                });

                int tmpId = item.GetInstanceID();
                item.transform.Find("Delete").GetComponent<Button>().onClick.AddListener(() =>
                {
                    DaemonUI.makeSurePage.ShowOptions($"确定删除{mapProj.name}?", () =>
                    {
                        if (lastSelectRow && lastSelectRow.transform.parent.GetInstanceID() == tmpId)
                        {
                            lastSelectRow = null;
                            activeMap_ = null;
                        }

                        //删除脚本和记录
                        SimpleNonGamingSystem.gameMapManager.Remove(mapProj.name);

                        //ui delete
                        GameObject.Destroy(item.gameObject);
                    },null, "是", "否");
                });
            });
        }

    }
}
