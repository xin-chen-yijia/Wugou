using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou;
using Wugou.UI;
using TMPro;
using Wugou.Multiplayer;
using System.IO;

namespace Wugou.Examples
{
    public class SimpleNonGamingSystem : NonGamingSystem
    {
        public override void Start()
        {
            base.Start();

            // show window
            uiRootWindow.GetChildWindow<HomePage>().Show();

            // 游戏记录管理
            GamePlay.gameStatsManager = new GameStatsManager(Path.Combine(Application.persistentDataPath, "gamestats"));

            if (GamePlay.lastGameStats != null)
            {
                uiRootWindow.GetChildWindow<HomePage>().Toggle(HomePage.StatisticPageId);
                uiRootWindow.GetChildWindow<SimpleStatisticPage>().SelectLastest();

                GamePlay.lastGameStats = null;
            }

            //
            WeatherSystem.ApplyWeather = () =>
            {
                // do nothing
            };
        }
    }
}
