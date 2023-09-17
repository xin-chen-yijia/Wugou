using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wugou;
using Wugou.UI;
using TMPro;
using Wugou.Multiplayer;
using System;
using System.IO.Compression;

public class CreateRoomPage : UIBaseWindow
{
    public GameObject rowContainer;
    public GameObject rowPrefab;

    private GameMap selectedGameMap_ = null;

    Button createBtn => transform.Find("Buttons/Create").GetComponent<Button>();

    // Start is called before the first frame update
    void Start()
    {

        createBtn.onClick.AddListener(() =>
        {
            var page = rootWindow.GetChildWindow<InRoomPage>();
            page.Show();
            page.SetPlayerCount(selectedGameMap_.maxPlayerCount);
            page.SetGameMap(selectedGameMap_);
            page.SetAsRoomOwner(true);

            MultiplayerGameManager.instance.CreateRoom(selectedGameMap_);

            Hide();
        });

        transform.Find("Buttons/Last").GetComponent<Button>().onClick.AddListener(() =>
        {
            rootWindow.GetChildWindow<RoomListPage>().Show();

            Hide();
        });

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetGameMaps(List<GameMap> gameMaps)
    {
        selectedGameMap_ = null;

        GameObject lastChecked = null;
        Utils.FillContent(rowContainer, rowPrefab, gameMaps, (item, map) =>
        {
            item.GetComponent<Button>().onClick.AddListener(() =>
            {
                lastChecked?.SetActive(false);

                lastChecked = item.transform.Find("Checked").gameObject;
                lastChecked.SetActive(true);

                selectedGameMap_ = map;
                FillMapContent(selectedGameMap_);
            });


            item.name = map.name;
            item.transform.Find("Name").GetComponent<TMP_Text>().text = map.name;
        });

        // 选择第一个
        rowContainer.transform.GetChild(0).GetComponent<Button>().onClick.Invoke();

    }

    public void FillMapContent(GameMap map)
    {
        string icon = GameMapManager.GetSceneIcon(map.scene.sceneName);
        transform.Find("Right/Icon").GetComponent<Image>().sprite = Wugou.Utils.LoadSpriteFromFile(System.IO.Path.Combine(GameMapManager.resourceDir, icon), new Vector2(0.5f, 0.5f));
        transform.Find("Right/Details/Name").GetComponent<TMP_Text>().text = $"脚本名称：{map.name}";
        transform.Find("Right/Details/Author").GetComponent<TMP_Text>().text = $"作者：{map.author}";
        transform.Find("Right/Details/CreateTime").GetComponent<TMP_Text>().text = $"创建时间：{map.createTime}";
        transform.Find("Right/Details/Description").GetComponent<TMP_Text>().text = $"{map.description}";
    }
}
