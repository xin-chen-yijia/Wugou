using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wugou;
using Wugou.UI;
using Wugou.Multiplayer;
using TMPro;

namespace Wugou.Examples.UI
{
    public class SimpleCreateRoomPage : UIBaseWindow
    {
        public GameObject rowContainer;
        public GameObject rowPrefab;
        public Button createBtn;
        public Button lastBtn;

        private GameMap selectedGameMap_ = null;


        // Start is called before the first frame update
        public void Start()
        {

            createBtn.onClick.AddListener(() =>
            {
                if (selectedGameMap_ == null)
                {
                    rootWindow.GetChildWindow<MakeSurePage>().ShowTips("请先选择一个脚本。");
                    return;
                }

                CreateRoom();

                Hide();
            });

            lastBtn.onClick.AddListener(() =>
            {
                rootWindow.GetChildWindow<SimpleRoomListPage>().Show();

                Hide();
            });

        }

        private void CreateRoom()
        {
            var page = rootWindow.GetChildWindow<SimpleInRoomPage>();
            page.Show();
            page.SetPlayerCount(selectedGameMap_.maxPlayerCount);
            page.SetGameMap(selectedGameMap_);
            page.SetAsRoomOwner(true);

            MultiplayerGameManager.instance.CreateRoom(selectedGameMap_);
        }

        // Update is called once per frame
        //void Update()
        //{

        //}

        public void SetGameMaps(List<GameMap> gameMaps)
        {
            selectedGameMap_ = null;

            GameObject lastChecked = null;
            float lastClickTime = -1;
            Utils.FillContent(rowContainer, rowPrefab, gameMaps, (item, map) =>
            {
                item.GetComponent<Button>().onClick.AddListener(() =>
                {
                    lastChecked?.SetActive(false);

                    lastChecked = item.transform.Find("Checked").gameObject;
                    lastChecked.SetActive(true);

                    selectedGameMap_ = map;
                    FillMapContent(selectedGameMap_);

                    if (Time.realtimeSinceStartup - lastClickTime < Utils.doubleClickMaxInterval)
                    {
                        CreateRoom();
                        Hide();
                    }
                    lastClickTime = Time.realtimeSinceStartup;
                });


                item.name = map.name;
                item.transform.Find("Name").GetComponent<TMP_Text>().text = map.name;
            });

            // 选择第一个
            if (rowContainer.transform.childCount > 0)
            {
                rowContainer.transform.GetChild(0).GetComponent<Button>().onClick.Invoke();
            }

        }

        private void FillMapContent(GameMap map)
        {
            string iconName = GameMapManager.GetAssetbundleSceneIcon(map.scene.sceneName);
            Utils.LoadSpriteFromFileWithWebRequest(System.IO.Path.GetFullPath($"{GamePlay.settings.resourcePath}/{iconName}"), new Vector2(0.5f, 0.5f), (sprite) =>
            {
                transform.Find("Main/Right/Icon").GetComponent<Image>().sprite = sprite;
            });
            transform.Find("Main/Right/Details/Name").GetComponent<TMP_Text>().text = $"脚本名称：{map.name}";
            transform.Find("Main/Right/Details/Author").GetComponent<TMP_Text>().text = $"作者：{map.author}";
            transform.Find("Main/Right/Details/CreateTime").GetComponent<TMP_Text>().text = $"创建时间：{map.createTime}";
            transform.Find("Main/Right/Details/Description").GetComponent<TMP_Text>().text = $"{map.description}";
        }
    }
}
