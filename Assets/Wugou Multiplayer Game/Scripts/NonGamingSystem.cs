using Newtonsoft.Json.Linq;
using Wugou.MapEditor;
using Wugou.Multiplayer;
using Wugou.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wugou
{
    /// <summary>
    /// ����Ϸ�߼�
    /// </summary>
    public class NonGamingSystem : MonoBehaviour
    {
        // ����
        public static NonGamingSystem instance = null;

        // UI
        public UIRootWindow uiRootWindow;

        private static bool sInited_ = false;

        private static void InitOnce()
        {
            if (sInited_)
            {
                return;
            }

            sInited_ = true;
            // register sceneObject's types for map editor
            var typePrefabs = Resources.LoadAll<GameObject>("SceneObjectPrototype/Gameplay");
            foreach (var v in typePrefabs)
            {
                GameEntityManager.RegisterPrefab(v.name, v);
            }

            // ��ȡ�ʲ�
            GameAssetDatabase.RegisterAssets($"{GamePlay.settings.configPath}/assets.json");
        }

        public virtual void Awake()
        {
            instance = this;

            InitOnce();
        }


        // Start is called before the first frame update
        public virtual void Start()
        {

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
            AssetBundleAssetLoader.UnloadAllAssetBundle();
        }

        /// <summary>
        /// ������Ϸ����
        /// </summary>
        public void EnterLobby()
        {
            SceneManager.LoadScene(GamePlay.settings.networkMainSceneName);
        }
    }
}
