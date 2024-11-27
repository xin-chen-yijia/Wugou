using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    [CreateAssetMenu(fileName = "GamePlaySettings", menuName = "Wugou Tools/GamePlaySettings", order = 1)]
    public class GamePlaySettings : ScriptableObject
    {
        public string host = "";
        public string clienScene = "client";
        public string editorHomeScene = "EditorHome";
        public string editorScene = "MapEditor";
        public string networkMainScene = "NetworkMain";
        public string singlePlayScene = "Single";
        public string gameModeScene = "GameMode";
    }
}
