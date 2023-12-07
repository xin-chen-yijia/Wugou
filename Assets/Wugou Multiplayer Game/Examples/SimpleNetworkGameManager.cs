using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.Multiplayer;
using Wugou.UI;
using Wugou.Examples.UI;

namespace Wugou.Examples
{
    /// <summary>
    /// ��ҽ�ɫ
    /// </summary>
    public enum PlayerRole
    {
        kTeacher = 0,
        kStudent
    }

    /// <summary>
    /// ��Ϸ��¼
    /// </summary>
    public class SimpleGameStats : GameStats
    {
        public Dictionary<string, SimpleGameSnapshot> playerStats = new Dictionary<string, SimpleGameSnapshot>();
    }

    /// <summary>
    /// ��Ϸ��¼����
    /// </summary>
    public class SimpleGameSnapshot
    {
        public string name;
        public int score;
    }

    public class SimpleNetworkGameManager : MultiplayerGameManager
    {
        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();
            // ע����PlayerRoleһ�£����Կ��ǻ�ȡPlayerRoleö�����ͣ���������ת��������Ҫ�����Ĵ����������ԣ��������Ը��
            Dictionary<string,int> roleDropdownOptions = new Dictionary<string,int>()
            {
                {"��ʦ",0 },
                {"ѧ��",1 },
            };
            uiRootWindow.GetChildWindow<SimpleInRoomPage>().SetRoleOptions(roleDropdownOptions);
        }

        // Update is called once per frame
        public override void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                StopGameplay();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        protected override void OnStartGameplay()
        {
            var gameStats = new SimpleGameStats();
            gameStats.name = "replay_" + System.DateTime.Now.ToFileTimeUtc();
            gameStats.gamemap = gameMap.name;
            gameStats.duration = Time.realtimeSinceStartup;
            GamePlay.lastGameStats = gameStats;
        }

        public override void UpdateGameplayerSnapshot(MultiplayerGamePlayer player)
        {
            var gameStats = GamePlay.lastGameStats as SimpleGameStats;
            gameStats.playerStats[player.name] = new SimpleGameSnapshot() { name = "Jack", score = 100 };
        }
    }

}
