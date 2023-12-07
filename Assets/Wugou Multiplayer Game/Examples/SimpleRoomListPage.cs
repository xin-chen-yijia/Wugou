using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wugou;
using Wugou.UI;
using Wugou.Multiplayer;
using TMPro;
using UnityEngine.Events;

namespace Wugou.Examples.UI
{
    public class SimpleRoomListPage : UIBaseWindow
    {
        public GameObject roomPrfab;
        public GameObject roomRowContainer;

        private MultiplayerServerResponse activeResponse_;

        private Dictionary<long, ServerRecord> cachedItems_ = new Dictionary<long, ServerRecord>();
        private Stack<GameObject> cachedUnusedItems_ = new Stack<GameObject>();

        private class ServerRecord
        {
            public long serverID;
            public float validTime;
            public GameObject rowObj;
        }

        // Start is called before the first frame update
        public virtual void Start()
        {
            MultiplayerGameManager.instance.GetComponent<MultiplayerNetworkDiscovery>().OnServerFound.AddListener(OnDiscoveredServer);

            roomPrfab.SetActive(false);
            transform.Find("Rooms/Buttons/CreateButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                var page = rootWindow.GetChildWindow<SimpleCreateRoomPage>();
                page.Show();
                page.SetGameMaps(GameMapManager.GetAllGameMaps());

                Hide();
            });

            transform.Find("Rooms/Buttons/JoinButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                JoinRoom();
            });
        }

        private void JoinRoom()
        {
            if (activeResponse_.uri != null)
            {
                MultiplayerGameManager.instance.EnterRoom(activeResponse_.uri);
                var page = rootWindow.GetChildWindow<SimpleInRoomPage>();
                page.SetAsRoomOwner(false);
                page.SetPlayerCount(activeResponse_.maxPlayerCount);
                page.Show();
                Hide();
            }
        }

        // Update is called once per frame
        //void Update()
        //{
        //}

        private async void OnEnable()
        {
            await new YieldInstructionAwaiter(null);
            StartCoroutine(ProcessTrainingRoomsInfo());

            MultiplayerGameManager.instance.StartServerDiscovery();
        }

        private void OnDisable()
        {
            if(MultiplayerGameManager.instance != null)
            {
                MultiplayerGameManager.instance.GetComponent<MultiplayerNetworkDiscovery>().OnServerFound.RemoveListener(OnDiscoveredServer);
                MultiplayerGameManager.instance.StopServerDiscovery();
            }
        }

        private GameObject lastChecked = null;
        private float lastClickTime = -1.0f;
        private void OnDiscoveredServer(MultiplayerServerResponse response)
        {
            if (!enabled)
            {
                return;
            }

            if (cachedItems_.ContainsKey(response.serverId))
            {
                cachedItems_[response.serverId].validTime = Time.realtimeSinceStartup + 3.0f;
            }
            else
            {
                GameObject go = null;
                if (!cachedUnusedItems_.TryPop(out go))
                {
                    go = GameObject.Instantiate<GameObject>(roomPrfab, roomRowContainer.transform);
                }
                go.SetActive(true);
                Utils.ResizeContainerHeight(roomRowContainer);

                go.transform.Find("RoomButton/Name").GetComponent<TMP_Text>().text = $"{response.gameMap}";
                go.transform.Find("RoomButton/Author").GetComponent<TMP_Text>().text = $"{response.playerName}";
                go.transform.Find("RoomButton/Count").GetComponent<TMP_Text>().text = $"{response.maxPlayerCount}";
                var button = go.GetComponentInChildren<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    lastChecked?.SetActive(false);

                    lastChecked = go.transform.Find("RoomButton/Checked").gameObject;
                    lastChecked.SetActive(true);

                    activeResponse_ = response;

                    // Ë«»÷¼ÓÈë
                    if (Time.realtimeSinceStartup - lastClickTime < Utils.doubleClickMaxInterval)
                    {
                        JoinRoom();
                    }
                    lastClickTime = Time.realtimeSinceStartup;
                });

                cachedItems_[response.serverId] = new ServerRecord()
                {
                    serverID = response.serverId,
                    validTime = Time.realtimeSinceStartup + 3.0f,
                    rowObj = go
                };
            }
        }

        public override void Hide()
        {
            base.Hide();
            MultiplayerGameManager.instance.StopServerDiscovery();
        }

        private IEnumerator ProcessTrainingRoomsInfo()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(0.1f);

                List<long> willRemoved = new List<long>();
                foreach (var v in cachedItems_)
                {
                    if (Time.realtimeSinceStartup > v.Value.validTime)
                    {
                        v.Value.rowObj.SetActive(false);
                        cachedUnusedItems_.Push(v.Value.rowObj);
                        willRemoved.Add(v.Key);
                    }
                }

                foreach (var v in willRemoved)
                {
                    cachedItems_.Remove(v);
                }
            }
        }

    }


}
