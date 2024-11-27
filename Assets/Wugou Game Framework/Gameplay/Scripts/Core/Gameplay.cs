using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Wugou
{
    /// <summary>
    /// 游戏设计或玩法
    /// </summary>
    public class Gameplay : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            GameObject.DontDestroyOnLoad(gameObject);

            Init();
        }

        // Update is called once per frame
        //void Update()
        //{

        //}

        private void OnDestroy()
        {
            Exit();
        }

        /// <summary>
        /// 通用内容的初始化：
        /// 1. GameEntity的Prototype注册；
        /// 2. GameAssetDatabase 挂载；
        /// 3. GameMapManager 初始化；
        /// </summary>
        private void Init()
        {
            GameConsole.CreateWorkDirsIfNeed();

            // 读取配置文件
            try
            {
                string configContent = File.ReadAllText($"{GameConsole.configPath}/{GameConsole.kCONFIG_FILE_NAME}");
                var jo = Newtonsoft.Json.Linq.JObject.Parse(configContent);
                GameConsole.fps = jo["fps"].ToObject<int>();
            }
            catch
            {
                Logger.Error("Read config error...");
            }

            // register sceneObject's types
            var typePrefabs = Resources.LoadAll<GameObject>("GameEntityPrototype");
            foreach (var v in typePrefabs)
            {
                GameEntityManager.RegisterPrototype(v.name, v);
            }

            // 默认资产
            _ = GameAssetDatabase.Mount($"{GameAssetDatabase.kBuiltInMountPoint}", $"{GameConsole.resourcePath}", GameAssetDatabase.MountContentType.kFileSystem);

            // 下载的文件
            _ = GameAssetDatabase.Mount($"{GameConsole.kDownloadDir}", $"{GameConsole.workplacePath}", GameAssetDatabase.MountContentType.kFileSystem);

            // 扫描AB包
            _ = GameConsole.MountAssetbundles($"{GameConsole.resourcePath}");

            // 挂在下载的场景包
            _ = GameConsole.MountAssetbundles($"{GameConsole.scenesPath}");

            //
            OnGameplayInitialized();
        }

        private void Exit()
        {
            GameAssetDatabase.UnmountAll();
            HttpDownloader.WriteCache();

            //
            OnGameplayExit();
        }


        /// <summary>
        /// 初始化后调用
        /// </summary>
        protected virtual void OnGameplayInitialized()
        {

        }

        protected virtual void OnGameplayExit()
        {

        }

    }
}

