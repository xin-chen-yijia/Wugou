using Wugou;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Wugou.Multiplayer {

    public class MultiplayerRoomPlayer : NetworkRoomPlayer
    {
        //// syncvar 是按顺序同步的，所以playerid 写在第一个，用于房间列表定位
        /// 用SyncVar 而不是 rpc，是因为后登录的玩家无法同步之前的rpc
   
        /// <summary>
        /// player id 是指在本次游戏中的id，从0开始
        /// </summary>
        [SyncVar(hook = nameof(PlayerIDChanged))]
        public int playerId = -1;

        [SyncVar(hook = nameof(PlayerNameChanged))]
        public string playerName="";

        [SyncVar(hook = nameof(PlayerRoleChanged))]
        public int playerRole = -1;  // 角色信息

        /// <summary>
        /// 在房间中的位置
        /// </summary>
        [SyncVar(hook = nameof(RoomSeatChanged))]
        public int roomSeat = -1;

        /// <summary>
        /// 地图是否准备好
        /// </summary>
        [SyncVar]
        public bool isGameMapReady;

        #region sync hooks

        protected virtual void PlayerIDChanged(int oldId, int newId)
        {
            // 报告自己的存在
            allPlayers[newId] = this;
        }

        protected virtual void PlayerNameChanged(string oldName, string newName)
        {
            gameObject.name = newName;

            Wugou.Logger.Info("new player:" + playerId + ":" + playerName);
        }

        protected virtual void PlayerRoleChanged(int oldRole, int newRole)
        {
        }

        protected virtual void RoomSeatChanged(int oldSeat, int newSeat)
        {

        }

        #endregion

        /// <summary>
        /// 在房间中的玩家
        /// </summary>
        public static Dictionary<int, MultiplayerRoomPlayer> allPlayers { get; private set; } = new Dictionary<int, MultiplayerRoomPlayer>();
        public static MultiplayerRoomPlayer Get(int id)
        {
            if (allPlayers.ContainsKey(id))
            {
                return allPlayers[id];
            }

            return null;
        }

        [Command]
        public virtual void CmdSetPlayerName(string name)
        {
            playerId = MultiplayerGameManager.instance.AllocatePlayerID();
            playerName = name;

            roomSeat = MultiplayerGameManager.instance.AllocateRoomSeat();

            // 报告自己的存在,注意这个要比player id 早
            allPlayers[playerId] = this;
        }

        [Command]
        public virtual void CmdSetPlayerRole(int role)
        {
            playerRole = role;
        }

        [Command]
        public virtual void CmdSetRoomSeat(int roomSeat)
        {
            foreach(var v in allPlayers)
            {
                if(v.Value.roomSeat == roomSeat)
                {
                    Logger.Info($"Roomseat '{roomSeat}' already exists...");
                    return;
                }
            }

            this.roomSeat = roomSeat;
        }

        [Command]
        public void CmdSetIsGameMapReady(bool value)
        {
            isGameMapReady = value;
        }


        public override void Start()
        {
            base.Start();
            if (isLocalPlayer)
            {
                CmdSetPlayerName(Authorization.activeUser?.name ?? "unknown");

                if (!isServer)
                {
                    StartCoroutine(CheckGameMapReady());
                }
                else
                {
                    isGameMapReady = true;
                }
            }
        }

        IEnumerator CheckGameMapReady()
        {
            while (MultiplayerGameManager.instance.gameMapPackage == null)
            {
                yield return null;
            }

            CmdSetIsGameMapReady(true);
        }

        public override void OnDisable()
        {
            base.OnDisable();

            if (allPlayers.ContainsKey(playerId))
            {
                allPlayers.Remove(playerId);
            }
        }
    }
}

