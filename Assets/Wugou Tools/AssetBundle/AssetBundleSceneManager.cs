using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.Events;
using System.IO;

namespace Wugou
{
    /// <summary>
    /// 管理AssetBundle中的场景加载
    /// </summary>
    public class AssetBundleSceneManager
    {
        // 用于获取当前加载进度
        public static AsyncOperation activeAsyncOperation {  get; private set; }

        /// <summary>
        /// 获取场景全名称
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        public static string GetFullSceneName(string sceneName)
        {
            if (!sceneName.EndsWith(".unity"))
            {
                return sceneName + ".unity";
            }

            return sceneName;
        }

        /// <summary>
        /// 加载指定场景
        /// </summary>
        /// <param name="scene"></param>
        public static async void LoadScene(AssetBundleScene scene, LoadSceneMode model, System.Action onLoaded = null)
        {
            // 不能用Task，因为Unity3D只支持在主线程中调用相关API，如AssetBundle.Load
            //var v = Task<AssetBundleLoader>.Run(async () =>
            //{
                AssetBundleAssetLoader loader = await AssetBundleAssetLoader.GetOrCreate(scene.assetbundle.path);
                if (loader == null)
                {
                    Logger.Error($"Loader init with{scene.assetbundle} fail...");
                    return;
                }

                // 先加载有该场景的AB包
                await loader.LoadAssetBundleAsync(loader.GetAssetBundleByAssetName(GetFullSceneName(scene.sceneName)));

                // 再加载场景
                Logger.DebugInfo($"Start load scene {scene.sceneName}");
                activeAsyncOperation = SceneManager.LoadSceneAsync(scene.sceneName, model);
                await activeAsyncOperation;
                Logger.DebugInfo($"load scene {scene.sceneName} complete..");
                onLoaded?.Invoke();

                activeAsyncOperation = null;
            //});


        }

        /// <summary>
        /// 卸载场景
        /// </summary>
        /// <param name="sceneName"></param>
        public static void UnLoadSceneAsync(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene != null)
                CoroutineLauncher.active.StartCoroutine(UnLoadScene(scene));
        }

        private static IEnumerator UnLoadScene(Scene scene)
        {
            AsyncOperation async = SceneManager.UnloadSceneAsync(scene);
            yield return async;
        }

    }

}
