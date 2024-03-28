using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace Wugou
{
    /// <summary>
    /// 
    /// </summary>
    public static class GameMapTemplateManager
    {

        /// <summary>
        /// 获取所有脚本模板
        /// </summary>
        /// <returns></returns>
        public static List<GameMapTemplateDesc> GetAllGameMapTemplates()
        {
            var res = JsonConvert.DeserializeObject<List<GameMapTemplateDesc>>(File.ReadAllText($"{Gameplay.configPath}/map-templates.json"));
            return res;
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
                map.Parse(template.content);
            }
            else
            {
                map.Parse("{}");    // 空白模板
                map.weather.time = 0.4f;
            }

            map.version = GameMap.kLatestVersion;
            map.createTime = DateTime.Now.ToString();
            map.name = $"新建 {template.name}";

            return map;
        }
    }

    /// <summary>
    /// 模板描述
    /// </summary>
    public class GameMapTemplateDesc
    {
        public string name;
        public string sceneType;
        public string icon;

        /// <summary>
        /// 可选场景
        /// </summary>
        public List<string> sceneTags { get; set; }

        /// <summary>
        /// 地图内容
        /// </summary>
        public string content { get; set; }
    }

}
