using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    public static class GamePlay
    {
        // 登录信息
        public static Authorization.User loginInfo { get; set; }

        /// <summary>
        /// 游戏模式，区别于编辑模式
        /// </summary>
        public static bool isGaming { get; set; } = false;

        /// <summary>
        /// 装备所在AB包路径
        /// </summary>
        public static string dynamicResourcePath { get; set; }

        /// <summary>
        /// 最近一局的记录
        /// </summary>
        public static GameStats lastGameStats = null;

        /// <summary>
        /// 训练记录管理
        /// </summary>
        public static GameStatsManager gameStatsManager { get; set; } = null;

        /// <summary>
        /// 加载的脚本文件名称
        /// </summary>
        public static string loadedGameMapFile { get; set; } = string.Empty;

        // scene
        public const string kMapEditorSceneName = "MapEditor";
        public const string kMainSceneName = "Main";
        public const string kNetworkMainSceneName = "NetworkMain";

        public const string kUnistormWeatherCollectionName = "UnistormWeatherCollection";

        /// <summary>
        /// 当前应用版本，用于加载控制
        /// </summary>
        public const int version = 1;
    }
}
