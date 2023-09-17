using Newtonsoft.Json.Linq;
using Wugou.MapEditor;
using Wugou.Multiplayer;
using Wugou.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wugou
{
    /// <summary>
    /// 非游戏逻辑
    /// </summary>
    public class NonGamingSystem : MonoBehaviour
    {
        // 单例
        public static NonGamingSystem instance = null;

        // UI
        public UIRootWindow uiRootWindow;


        public virtual void Awake()
        {
            instance = this;

            // register sceneObject's types for map editor
            var typePrefabs = Resources.LoadAll<GameObject>("SceneObjectPrototype/Gameplay");
            foreach (var v in typePrefabs)
            {
                GameEntityManager.RegisterPrefab(v.name, v);
            }
        }

        // Start is called before the first frame update
        public virtual void Start()
        {
            //
            JObject config = JObject.Parse(File.ReadAllText($"{Application.streamingAssetsPath}/config.json"));
            string resoureDir = config["resource"].ToString();
            GamePlay.dynamicResourcePath = resoureDir + "/" + config["mainAssetBundle"].ToString();

            // 脚本路径
            GameMapManager.Initialize(resoureDir);
            GameEntityManager.resourcePath = resoureDir;

            string mapsDir = config["mapDir"].ToString();
            if (mapsDir != "")
            {
                GameMapManager.gameMapsDir = mapsDir;
            }

            // UI
            // show window
            uiRootWindow.Show();
            uiRootWindow.GetChildWindow<HomePage>().Show();
            uiRootWindow.GetChildWindow<GameMapListPage>().Show();
            uiRootWindow.GetChildWindow<GameMapListPage>().Refresh();

        }

        // Update is called once per frame
        public virtual void Update()
        {
            if(Input.GetKeyUp(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        public virtual void OnApplicationQuit()
        {
            // assets
            GameMapManager.UnloadAllAssetBundles();
        }

        /// <summary>
        /// 进入游戏大厅
        /// </summary>
        public void EnterLobby()
        {
            SceneManager.LoadScene(GamePlay.kNetworkMainSceneName);
        }
    }
}
