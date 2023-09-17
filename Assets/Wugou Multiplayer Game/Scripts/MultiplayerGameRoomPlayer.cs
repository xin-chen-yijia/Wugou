using Wugou;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.MapEditor;
using Wugou.UI;

using Mirror;
using Wugou.Multiplayer;
using static Wugou.Authorization;

namespace Wugou.Multiplayer {
    public class MultiplayerGameRoomPlayer : NetworkRoomPlayer
    {
        //// syncvar 是按顺序同步的，所以playerid 写在第一个，用于房间列表定位
        /// 用SyncVar 而不是 rpc，是因为后登录的玩家无法同步之前的rpc
   
        /// <summary>
        /// player id 是指在本次游戏中的id，从0开始
        /// </summary>
        [SyncVar(hook = nameof(SetPlayerID))]
        public int playerId = -1;

        [SyncVar(hook = nameof(SetPlayerName))]
        public string playerName;

        [SyncVar(hook = nameof(SetPlayerRole))]
        public int playerRole = 0;  // 角色信息

        #region sync hooks

        protected virtual void SetPlayerID(int oldId, int newId)
        {
            Debug.Log(nameof(SetPlayerID));
            // 玩家列表更新
            GameObject.FindObjectOfType<InRoomPage>().AddPlayer(this);
        }

        protected virtual void SetPlayerName(string oldName, string newName)
        {
            gameObject.name = newName;

            // 报告自己的存在
            MultiplayerGameManager.instance.ReportRoomPlayer(this);
            Wugou.Logger.Info("new player:" + playerId + ":" + playerName);

            GameObject.FindObjectOfType<InRoomPage>().UpdatePlayerName(this);
        }

        protected virtual void SetPlayerRole(int oldRole, int newRole)
        {
            // 玩家列表更新
            GameObject.FindObjectOfType<InRoomPage>().UpdatePlayerRole(this);
        }

        #endregion

        [Command]
        public void CmdSetPlayerName(string name)
        {
            // id 分配
            int id = 0;
            var players = MultiplayerGameManager.instance.roomplayers;
            HashSet<int> ids = new HashSet<int>();
            foreach (var p in players.Values)
            {
                if (p && p.playerId >= 0)
                {
                    ids.Add(p.playerId);
                }
            };

            // 寻找最小的可用id，用于在房间中的玩家列表
            while (ids.Contains(id))
            {
                ++id;
            }

            playerId = id;
            playerName = name;
        }

        [Command]
        public void CmdSetPlayerRole(int role)
        {
            playerRole = role;
        }


        public override void Start()
        {
            base.Start();
            if (isLocalPlayer)
            {
                CmdSetPlayerName(GamePlay.loginInfo.name);
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();

            if (NetworkClient.active && NetworkManager.singleton is NetworkRoomManager room)
            {
                var page = GameObject.FindObjectOfType<InRoomPage>();
                if (page)
                {
                    page.RemovePlayer(this);
                }
            }
        }
    }
}
