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
    public class SimpleInRoomPage : UIBaseWindow
    {
        private GameMap map_;

        public GameObject playerRowContainer;
        public GameObject playerRowPrefab;

        public Button startButton;
        public Button quitButton;

        public Image mapIconImage;
        public TMP_Text nameLabel;
        public TMP_Text authorLabel;
        public TMP_Text createTimeLabel;
        public TMP_Text descriptionLabel;

        private Dictionary<MultiplayerGameRoomPlayer, GameObject> playerRowDictionary_ = new Dictionary<MultiplayerGameRoomPlayer, GameObject>();

        /// <summary>
        /// 角色可选项
        /// </summary>
        public Dictionary<string,int> roleOptions { get; private set; }

        private void Awake()
        {
            playerRowPrefab.SetActive(false);
        }

        // Start is called before the first frame update
        public void Start()
        {
            startButton.onClick.AddListener(() =>
            {
                MultiplayerGameManager.instance.StartGameplay(map_);

                Hide();
            });

            quitButton.onClick.AddListener(() =>
            {
                MultiplayerGameManager.instance.ExitRoom();
                Hide();
            });
        }

        //// Update is called once per frame
        //void Update()
        //{

        //}

        public const string kRoleStr = "Role";
        /// <summary>
        /// 设置角色下拉选项,注意，需要在SetPlayerCount之前调用
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

            // 角色信息
            var dropdown = playerRowPrefab.transform.Find(kRoleStr).GetComponent<TMP_Dropdown>();
            dropdown.options = ops;
            dropdown.SetValueWithoutNotify(0);

            foreach (Transform v in playerRowContainer.transform)
            {
                v.Find(kRoleStr).GetComponent<TMP_Dropdown>().options = ops;
            }
        }

        public virtual void SetAsRoomOwner(bool isOwner)
        {
            startButton.gameObject.SetActive(isOwner);
        }

        public void SetPlayerCount(int count)
        {
            List<int> list = new List<int>();
            for (int i = 0; i < count; i++) list.Add(i);

            Wugou.Utils.FillContent<int>(playerRowContainer, playerRowPrefab, list, (go, i) =>
            {
                go.transform.Find("Name").GetComponent<TMP_Text>().text = "打开的";
            });
        }

        public void SetGameMap(GameMap map)
        {
            map_ = map;

            // fill content
            string iconName = GameMapManager.GetAssetbundleSceneIcon(map_.scene.sceneName);
            Utils.LoadSpriteFromFileWithWebRequest(System.IO.Path.GetFullPath($"{GamePlay.settings.resourcePath}/{iconName}"), new Vector2(0.5f, 0.5f), (sprite) =>
            {
                mapIconImage.sprite = sprite;
            });
            nameLabel.text = $"脚本名称：{map_.name}";
            authorLabel.GetComponent<TMP_Text>().text = $"作者：{map_.author}";
            createTimeLabel.GetComponent<TMP_Text>().text = $"创建时间：{map_.createTime}";
            descriptionLabel.GetComponent<TMP_Text>().text = $"{map_.description}";
        }

        public void AddPlayer(MultiplayerGameRoomPlayer player)
        {
            Wugou.Logger.Info("Add:" + player.playerId + ":" + player.playerName);

            // 加入了一个满员的房间，自动退回房间列表界面
            if(player.playerId >= playerRowContainer.transform.childCount)
            {
                // 客户端退出，服务器不处理，继续
                if (MultiplayerGameManager.instance.mode == Mirror.NetworkManagerMode.ClientOnly)
                {
                    MultiplayerGameManager.instance.ExitRoom();
                    Hide();
                    rootWindow.GetChildWindow<SimpleRoomListPage>().Show();
                }
                return;
            }

            Transform go = playerRowContainer.transform.GetChild(player.playerId);
            playerRowDictionary_.Add(player, go.gameObject);

            // name
            go.Find("Name").GetComponent<TMP_Text>().text = player.playerName;

            // role
            var dropdown = go.Find(kRoleStr).GetComponent<TMP_Dropdown>();
            dropdown.SetValueWithoutNotify(player.playerRole);

            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener((index) =>
            {
                player.CmdSetPlayerRole(index);
            });
            dropdown.interactable = player.isOwned;
        }

        public void RemovePlayer(MultiplayerGameRoomPlayer player)
        {
            if (playerRowDictionary_.ContainsKey(player) && playerRowDictionary_[player])
            {
                Wugou.Logger.Info("remove:" + player.playerId + ":" + player.playerName);
                playerRowDictionary_[player].transform.Find("Name").GetComponent<TMP_Text>().text = "打开的";
                playerRowDictionary_.Remove(player);
            }
        }

        public void UpdatePlayerName(MultiplayerGameRoomPlayer player)
        {
            if (!playerRowDictionary_.ContainsKey(player))
            {
                Logger.Warning($"No {player.playerName} in InRoomPage. Maybe room's player is full. ");
                return;
            }

            playerRowDictionary_[player].transform.Find("Name").GetComponent<TMP_Text>().text = player.playerName;
        }

        public void UpdatePlayerRole(MultiplayerGameRoomPlayer player)
        {
            if (!playerRowDictionary_.ContainsKey(player))
            {
                Logger.Warning($"No {player.playerName} in InRoomPage");
                return;
            }
            var dropdown = playerRowDictionary_[player].transform.Find(kRoleStr).GetComponent<TMP_Dropdown>();
            dropdown.SetValueWithoutNotify(player.playerRole);
        }
    }
}
