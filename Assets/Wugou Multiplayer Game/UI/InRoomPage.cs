using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wugou;
using Wugou.UI;
using Wugou.Multiplayer;
using TMPro;
using System.IO;
using static Wugou.Authorization;
using System;

namespace Wugou.UI
{
    public class InRoomPage : UIBaseWindow
    {
        private GameMap map_;

        public GameObject playerRowContainer;
        public GameObject playerRowPrefab;

        protected Dictionary<MultiplayerGameRoomPlayer, GameObject> playerRowDictionary_ = new Dictionary<MultiplayerGameRoomPlayer, GameObject>();

        /// <summary>
        /// ��ɫ��ѡ��
        /// </summary>
        public Dictionary<string,int> roleOptions { get; protected set; }

        private void Awake()
        {
            playerRowPrefab.SetActive(false);
        }

        // Start is called before the first frame update
        void Start()
        {
            transform.Find("Buttons/Start").GetComponent<Button>().onClick.AddListener(() =>
            {
                MultiplayerGameManager.instance.StartGameplay(map_);

                Hide();
            });

            transform.Find("Buttons/Quit").GetComponent<Button>().onClick.AddListener(() =>
            {
                MultiplayerGameManager.instance.ExitRoom();
                Hide();
            });
        }

        //// Update is called once per frame
        //void Update()
        //{

        //}

        public const string roleStr = "Role";
        /// <summary>
        /// ���ý�ɫ����ѡ��,ע�⣬��Ҫ��SetPlayerCount֮ǰ����
        /// </summary>
        /// <param name="options"></param>
        public void SetRoleOptions(Dictionary<string,int> options)
        {
            roleOptions = options;

            var ops = new List<TMP_Dropdown.OptionData>();
            foreach(var v in options.Keys)
            {
                ops.Add(new TMP_Dropdown.OptionData(v));
            }

            // ��ɫ��Ϣ
            var dropdown = playerRowPrefab.transform.Find(roleStr).GetComponent<TMP_Dropdown>();
            dropdown.options = ops;
            dropdown.SetValueWithoutNotify(0);

            foreach (Transform v in playerRowContainer.transform)
            {
                v.Find(roleStr).GetComponent<TMP_Dropdown>().options = ops;
            }
        }

        public void SetAsRoomOwner(bool isOwner)
        {
            transform.Find("Buttons/Start").gameObject.SetActive(isOwner);
        }

        public void SetPlayerCount(int count)
        {
            List<int> list = new List<int>();
            for (int i = 0; i < count; i++) list.Add(i);

            Wugou.Utils.FillContent<int>(playerRowContainer, playerRowPrefab, list, (go, i) =>
            {
                go.transform.Find("Name").GetComponent<TMP_Text>().text = "�򿪵�";
            });
        }

        public void SetGameMap(GameMap map)
        {
            map_ = map;

            // fill content
            string iconName = GameMapManager.GetSceneIcon(map_.scene.sceneName);
            transform.Find("Right/Icon").GetComponent<Image>().sprite = Wugou.Utils.LoadSpriteFromFile(System.IO.Path.Combine(GameMapManager.resourceDir, iconName), new Vector2(0.5f, 0.5f));
            transform.Find("Right/Details/Name").GetComponent<TMP_Text>().text = $"�ű����ƣ�{map_.name}";
            transform.Find("Right/Details/Author").GetComponent<TMP_Text>().text = $"���ߣ�{map_.author}";
            transform.Find("Right/Details/CreateTime").GetComponent<TMP_Text>().text = $"����ʱ�䣺{map_.createTime}";
            transform.Find("Right/Details/Description").GetComponent<TMP_Text>().text = $"{map_.description}";
        }

        public virtual void AddPlayer(MultiplayerGameRoomPlayer player)
        {
            Wugou.Logger.Info("Add:" + player.playerId + ":" + player.playerName);

            Transform go = playerRowContainer.transform.GetChild(player.playerId);
            playerRowDictionary_.Add(player, go.gameObject);

            // name
            go.Find("Name").GetComponent<TMP_Text>().text = player.playerName;

            // role
            var dropdown = go.Find(roleStr).GetComponent<TMP_Dropdown>();
            dropdown.SetValueWithoutNotify(player.playerRole);

            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener((index) =>
            {
                player.CmdSetPlayerRole(index);
            });
            dropdown.interactable = player.isOwned;
        }

        public virtual void RemovePlayer(MultiplayerGameRoomPlayer player)
        {
            if (playerRowDictionary_.ContainsKey(player) && playerRowDictionary_[player])
            {
                Wugou.Logger.Info("remove:" + player.playerId + ":" + player.playerName);
                playerRowDictionary_[player].transform.Find("Name").GetComponent<TMP_Text>().text = "�򿪵�";
                playerRowDictionary_.Remove(player);
            }
        }

        public virtual void UpdatePlayerName(MultiplayerGameRoomPlayer player)
        {
            if (!playerRowDictionary_.ContainsKey(player))
            {
                Logger.Info($"No {player.playerName} in InRoomPage");
                return;
            }

            playerRowDictionary_[player].transform.Find("Name").GetComponent<TMP_Text>().text = player.playerName;
        }

        public virtual void UpdatePlayerRole(MultiplayerGameRoomPlayer player)
        {
            if (!playerRowDictionary_.ContainsKey(player))
            {
                Logger.Info($"No {player.playerName} in InRoomPage");
                return;
            }
            var dropdown = playerRowDictionary_[player].transform.Find(roleStr).GetComponent<TMP_Dropdown>();
            dropdown.SetValueWithoutNotify(player.playerRole);
        }
    }
}
