using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    public static class GamePlay
    {
        private static GamePlaySettings settings_ = null;
        public static GamePlaySettings settings
        {
            get
            {
                if (settings_ == null)
                {
                    settings_ = Resources.Load<GamePlaySettings>("GamePlaySettings");
                }

                return settings_;
            }
        }

        // 登录信息
        public static Authorization.User loginInfo { get; set; }

        /// <summary>
        /// 游戏模式，区别于编辑模式
        /// </summary>
        public static bool isGaming { get; set; } = false;

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
    }
}
