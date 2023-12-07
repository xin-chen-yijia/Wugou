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
        //// syncvar �ǰ�˳��ͬ���ģ�����playerid д�ڵ�һ�������ڷ����б�λ
        /// ��SyncVar ������ rpc������Ϊ���¼������޷�ͬ��֮ǰ��rpc
   
        /// <summary>
        /// player id ��ָ�ڱ�����Ϸ�е�id����0��ʼ
        /// </summary>
        [SyncVar(hook = nameof(SetPlayerID))]
        public int playerId = -1;

        [SyncVar(hook = nameof(SetPlayerName))]
        public string playerName;

        [SyncVar(hook = nameof(SetPlayerRole))]
        public int playerRole = 0;  // ��ɫ��Ϣ

        #region sync hooks

        protected virtual void SetPlayerID(int oldId, int newId)
        {
            // �����Լ��Ĵ���
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

