using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou;
using Wugou.UI;
using TMPro;
using Wugou.Multiplayer;
using System.IO;
using Wugou.Examples.UI;
using Wugou.Editor;

namespace Wugou.Examples
{
    public class SimpleNonGamingSystem : MonoBehaviour
    {
        public static FileAssetsManager<GameMap> gameMapManager { get; private set; }

        public UIRootWindow rootWindow;

        public void Start()
        {
            // show window
            rootWindow.Show();
            rootWindow.GetChildWindow<SimpleHomePage>().Show();
            rootWindow.GetChildWindow<SimpleGameMapListPage>().Show();
            rootWindow.GetChildWindow<SimpleGameMapListPage>().Refresh();

            gameMapManager = new FileAssetsManager<GameMap>($"{Gameplay.downloadGameMapsPath}", ".map", new GameMapFileParser());

            // 游戏记录管理
            Gameplay.gameStatsManager = new FileAssetsManager<GameStats>($"{Application.persistentDataPath}/gamestats",".gt");

            StartCoroutine(DelayDo());

            WeatherSystem.Load = () =>
            {

            };
            //
            WeatherSystem.ApplyWeather = () =>
            {
                // do nothing
            };

            WeatherSystem.Clear = () => { };
        }

        IEnumerator DelayDo()
        {
            yield return null;

            if (Gameplay.lastGameStats != null)
            {
                rootWindow.GetChildWindow<SimpleHomePage>().Toggle(SimpleHomePage.StatisticPageId);
                rootWindow.GetChildWindow<SimpleStatisticPage>().SelectLastest();

                Gameplay.lastGameStats = null;
            }
        }
    }
}
