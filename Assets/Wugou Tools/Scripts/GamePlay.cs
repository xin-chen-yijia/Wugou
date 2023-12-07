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

        // ��¼��Ϣ
        public static Authorization.User loginInfo { get; set; }

        /// <summary>
        /// ��Ϸģʽ�������ڱ༭ģʽ
        /// </summary>
        public static bool isGaming { get; set; } = false;

        /// <summary>
        /// ���һ�ֵļ�¼
        /// </summary>
        public static GameStats lastGameStats = null;

        /// <summary>
        /// ѵ����¼����
        /// </summary>
        public static GameStatsManager gameStatsManager { get; set; } = null;

        /// <summary>
        /// ���صĽű��ļ�����
        /// </summary>
        public static string loadedGameMapFile { get; set; } = string.Empty;
    }
}
