using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou;
using Wugou.UI;
using TMPro;
using Wugou.Multiplayer;
using System.IO;
using Wugou.Examples.UI;
using Wugou.MapEditor;

namespace Wugou.Examples
{
    public class SimpleNonGamingSystem : NonGamingSystem
    {
        public override void Start()
        {
            base.Start();

            // show window
            uiRootWindow.Show();
            uiRootWindow.GetChildWindow<SimpleHomePage>().Show();
            uiRootWindow.GetChildWindow<SimpleGameMapListPage>().Show();
            uiRootWindow.GetChildWindow<SimpleGameMapListPage>().Refresh();

            // 游戏记录管理
            GamePlay.gameStatsManager = new GameStatsManager(Path.Combine(Application.persistentDataPath, "gamestats"));

            // 
            MapEditorSystem.onSaveGameMap.AddListener((fileName, map) =>
            {
                GameMapManager.SaveGameMap(fileName, map);
            });

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

            if (GamePlay.lastGameStats != null)
            {
                uiRootWindow.GetChildWindow<SimpleHomePage>().Toggle(SimpleHomePage.StatisticPageId);
                uiRootWindow.GetChildWindow<SimpleStatisticPage>().SelectLastest();

                GamePlay.lastGameStats = null;
            }
        }
    }
}
