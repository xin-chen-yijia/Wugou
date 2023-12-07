using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.Multiplayer;
using Wugou.UI;
using Wugou.Examples.UI;

namespace Wugou.Examples
{
    /// <summary>
    /// 玩家角色
    /// </summary>
    public enum PlayerRole
    {
        kTeacher = 0,
        kStudent
    }

    /// <summary>
    /// 游戏记录
    /// </summary>
    public class SimpleGameStats : GameStats
    {
        public Dictionary<string, SimpleGameSnapshot> playerStats = new Dictionary<string, SimpleGameSnapshot>();
    }

    /// <summary>
    /// 游戏记录详情
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
            // 注意与PlayerRole一致，可以考虑获取PlayerRole枚举类型，不过名称转换可能需要其它的处理，比如特性，后续可以搞搞
            Dictionary<string,int> roleDropdownOptions = new Dictionary<string,int>()
            {
                {"教师",0 },
                {"学生",1 },
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
