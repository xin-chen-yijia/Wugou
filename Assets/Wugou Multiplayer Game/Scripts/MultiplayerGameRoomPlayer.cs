using Wugou;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.MapEditor;
using Mirror;
using Wugou.Multiplayer;

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
            // 报告自己的存在
            MultiplayerGameManager.instance.AddRoomPlayer(this);
        }

        protected virtual void SetPlayerName(string oldName, string newName)
        {
            gameObject.name = newName;

            Wugou.Logger.Info("new player:" + playerId + ":" + playerName);
        }

        protected virtual void SetPlayerRole(int oldRole, int newRole)
        {
        }

        #endregion

        [Command]
        public void CmdSetPlayerName(string name)
        {
            playerId = MultiplayerGameManager.instance.AllocatePlayerID();
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
                MultiplayerGameManager.instance.RemoveRoomPlayer(this);
            }
        }
    }
}

