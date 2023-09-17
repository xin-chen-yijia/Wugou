using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    public static class GamePlay
    {
        // ��¼��Ϣ
        public static Authorization.User loginInfo { get; set; }

        /// <summary>
        /// ��Ϸģʽ�������ڱ༭ģʽ
        /// </summary>
        public static bool isGaming { get; set; } = false;

        /// <summary>
        /// װ������AB��·��
        /// </summary>
        public static string dynamicResourcePath { get; set; }

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

        // scene
        public const string kMapEditorSceneName = "MapEditor";
        public const string kMainSceneName = "Main";
        public const string kNetworkMainSceneName = "NetworkMain";

        public const string kUnistormWeatherCollectionName = "UnistormWeatherCollection";

        /// <summary>
        /// ��ǰӦ�ð汾�����ڼ��ؿ���
        /// </summary>
        public const int version = 1;
    }
}
