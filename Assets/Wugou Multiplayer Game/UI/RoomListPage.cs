using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wugou;
using Wugou.Multiplayer;
using System.Linq;
using TMPro;
using UnityEngine.Events;
using Newtonsoft.Json.Linq;

namespace Wugou.UI
{
    public class RoomListPage : UIBaseWindow
    {
        public GameObject roomPrfab;
        public GameObject roomRowContainer;

        private MultiplayerServerResponse activeResponse_;

        private Dictionary<long, GameObject> cachedItems_ = new Dictionary<long, GameObject>();
        private Dictionary<long, GameObject> cachedUnusedItems_ = new Dictionary<long, GameObject>();

        // Start is called before the first frame update
        void Start()
        {
            roomPrfab.SetActive(false);
            transform.Find("Rooms/Buttons/CreateButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                var page = rootWindow.GetChildWindow<CreateRoomPage>();
                page.SetGameMaps(GameMapManager.GetAllGameMaps());
                page.Show();

                Hide();
            });

            transform.Find("Rooms/Buttons/JoinButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                if (activeResponse_.uri != null)
                {
                    MultiplayerGameManager.instance.EnterRoom(activeResponse_.uri);
                    var page = rootWindow.GetChildWindow<InRoomPage>();
                    page.SetAsRoomOwner(false);
                    page.SetPlayerCount(activeResponse_.maxPlayerCount);
                    page.Show();
                    Hide();
                }

            });

            Show();
        }

        // Update is called once per frame
        void Update()
        {
        }

        private void OnEnable()
        {
            StartCoroutine(ProcessTrainingRoomsInfo());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        public override void Hide()
        {
            base.Hide();
            MultiplayerGameManager.instance.StopDiscoveryServer();
        }

        IEnumerator ProcessTrainingRoomsInfo()
        {
            while (!MultiplayerGameManager.instance)
            {
                yield return null;
            }

            if (!MultiplayerGameManager.instance.isDiscoveringServer)
            {
                MultiplayerGameManager.instance.StartDiscoveryServer();
            }

            while (MultiplayerGameManager.instance.isDiscoveringServer)
            {
                yield return new WaitForSeconds(1.0f);

                foreach (var v in cachedItems_)
                {
                    v.Value.SetActive(false);
                }


                // 按钮点击事件
                GameObject lastChecked = null;
                UnityAction<GameObject, MultiplayerServerResponse> clickHandle = (item, response) =>
                {
                    lastChecked?.SetActive(false);

                    lastChecked = item.transform.Find("RoomButton/Checked").gameObject;
                    lastChecked.SetActive(true);

                    activeResponse_ = response;
                };

                List<MultiplayerServerResponse> newRooms = new List<MultiplayerServerResponse>();
                var rooms = MultiplayerGameManager.instance.GetAllServers();
                foreach (var v in rooms)
                {
                    if (cachedItems_.ContainsKey(v.serverId))
                    {
                        cachedItems_[v.serverId].SetActive(true);
                    }
                    else if (cachedUnusedItems_.Count > 0)
                    {
                        MultiplayerServerResponse response = v;
                        var u = cachedUnusedItems_.First();
                        u.Value.transform.Find("RoomButton/Name").GetComponent<TMP_Text>().text = $"{response.gameMap}";
                        u.Value.transform.Find("RoomButton/Author").GetComponent<TMP_Text>().text = $"{response.playerName}";
                        u.Value.transform.Find("RoomButton/Count").GetComponent<TMP_Text>().text = $"{response.maxPlayerCount}";
                        Button btn = u.Value.GetComponentInChildren<Button>();
                        btn.onClick.RemoveAllListeners();   // attention 
                        btn.onClick.AddListener(() =>
                        {
                            clickHandle(u.Value, response);
                        });

                        cachedItems_.Add(response.serverId, u.Value);
                        cachedUnusedItems_.Remove(response.serverId);
                    }
                    else
                    {
                        newRooms.Add(v);
                    }
                }

                // 填充列表
                Utils.FillContent<MultiplayerServerResponse>(roomRowContainer, roomPrfab, newRooms, (GameObject item, MultiplayerServerResponse response) =>
                {
                    cachedItems_.Add(response.serverId, item);

                    item.transform.Find("RoomButton/Name").GetComponent<TMP_Text>().text = $"{response.gameMap}";
                    item.transform.Find("RoomButton/Author").GetComponent<TMP_Text>().text = $"{response.playerName}";
                    item.transform.Find("RoomButton/Count").GetComponent<TMP_Text>().text = $"{response.maxPlayerCount}";
                    item.GetComponentInChildren<Button>().onClick.AddListener(() =>
                    {
                        clickHandle(item, response);
                    });
                }, false);
            }



        }
    }

}
