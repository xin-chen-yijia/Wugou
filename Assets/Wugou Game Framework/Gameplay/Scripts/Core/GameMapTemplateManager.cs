using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace Wugou
{
    /// <summary>
    /// 模板描述
    /// </summary>
    public class GameMapTemplateDesc
    {
        public string name;
        public string sceneType;

        public bool needWeather;
        public WeatherSetting weather;

        public string gameScript;
    }

    /// <summary>
    /// 游戏脚本模板管理
    /// </summary>
    public static class GameMapTemplateManager
    {

        /// <summary>
        /// 获取所有脚本模板
        /// </summary>
        /// <returns></returns>
        public static List<GameMapTemplateDesc> GetAllGameMapTemplates()
        {
            return new List<GameMapTemplateDesc>()
            {
                new GameMapTemplateDesc
                {
                    name = "多角色模拟演练",
                    sceneType = "3D",
                    needWeather = true,
                    weather = new WeatherSetting
                    {
                        type=1,
                        time = 0.4f,
                        fogDensity = 0.12f,
                        windForce = 0,
                        windDir = 0,
                    },
                    gameScript = "MultplayerTraining"
                },
            };

        }

        /// <summary>
        /// 基于模板创建一个新脚本
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public static GameMap CreateGameMapFromTemplate(GameMapTemplateDesc template)
        {
            GameMap map = GameMap.Create();
            if (template != null)
            {
                map.needWeather = template.needWeather;
                map.weather = template.weather;
                map.gameScript = template.gameScript;
            }
            else
            {
                map.needWeather = true;
                map.weather = new WeatherSetting
                {
                    type = 1,
                    time = 0.4f,
                    fogDensity = 0.5f,
                    windForce = 0,
                    windDir = 0,
                };
            }

            map.version = GameMap.kLatestVersion;
            map.createTime = DateTime.Now.ToString();
            map.name = $"新建 {template.name}";

            return map;
        }
    }



}
