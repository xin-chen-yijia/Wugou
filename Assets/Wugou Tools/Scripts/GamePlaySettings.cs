using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    [CreateAssetMenu(fileName = "GamePlaySettings", menuName = "Wugou Tools/GamePlaySettings", order = 1)]
    public class GamePlaySettings : ScriptableObject
    {
        /// <summary>
        /// �����������Դ������ʹ��"/"��ͷ�����url·����UnityWebRequest��ƴ��host domain
        /// </summary>
        public string configPath = "";
        public string resourcePath = "";
        /// <summary>
        /// װ������AB��·��
        /// </summary>
        public string mainAssetbundle = "";

        public string mapEditorSceneName = "MapEditor";
        public string mainSceneName = "Main";
        public string networkMainSceneName = "NetworkMain";
    }
}
