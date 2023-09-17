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
            Debug.Log(nameof(SetPlayerID));
            // ����б����
            GameObject.FindObjectOfType<InRoomPage>().AddPlayer(this);
        }

        protected virtual void SetPlayerName(string oldName, string newName)
        {
            gameObject.name = newName;

            // �����Լ��Ĵ���
            MultiplayerGameManager.instance.ReportRoomPlayer(this);
            Wugou.Logger.Info("new player:" + playerId + ":" + playerName);

            GameObject.FindObjectOfType<InRoomPage>().UpdatePlayerName(this);
        }

        protected virtual void SetPlayerRole(int oldRole, int newRole)
        {
            // ����б����
            GameObject.FindObjectOfType<InRoomPage>().UpdatePlayerRole(this);
        }

        #endregion

        [Command]
        public void CmdSetPlayerName(string name)
        {
            // id ����
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

            // Ѱ����С�Ŀ���id�������ڷ����е�����б�
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
