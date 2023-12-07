using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    [CreateAssetMenu(fileName = "GamePlaySettings", menuName = "Wugou Tools/GamePlaySettings", order = 1)]
    public class GamePlaySettings : ScriptableObject
    {
        /// <summary>
        /// 如果是网络资源，建议使用"/"开头的相对url路径，UnityWebRequest会拼接host domain
        /// </summary>
        public string configPath = "";
        public string resourcePath = "";
        /// <summary>
        /// 装备所在AB包路径
        /// </summary>
        public string mainAssetbundle = "";

        public string mapEditorSceneName = "MapEditor";
        public string mainSceneName = "Main";
        public string networkMainSceneName = "NetworkMain";
    }
}
