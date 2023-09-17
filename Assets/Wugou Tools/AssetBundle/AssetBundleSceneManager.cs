using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.Events;

namespace Wugou
{
    /// <summary>
    /// ����AssetBundle�еĳ�������
    /// </summary>
    public class AssetBundleSceneManager
    {
        // ���ڻ�ȡ��ǰ���ؽ���
        public static AsyncOperation activeAsyncOperation {  get; private set; }

        /// <summary>
        /// ��ȡ����ȫ����
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
        /// ����ָ������
        /// </summary>
        /// <param name="scene"></param>
        public static async void LoadScene(AssetBundleScene scene, LoadSceneMode model, System.Action onLoaded = null)
        {
            // ������Task����ΪUnity3Dֻ֧�������߳��е������API����AssetBundle.Load
            //var v = Task<AssetBundleLoader>.Run(async () =>
            //{
                AssetBundleAssetLoader loader = await AssetBundleAssetLoader.GetOrCreate(scene.assetbundle.path);
                if (loader == null)
                {
                    Logger.Error($"Loader init with{scene.assetbundle} fail...");
                    return;
                }

                // �ȼ����иó�����AB��
                await loader.LoadAssetBundleAsync(loader.GetAssetBundleByAssetName(GetFullSceneName(scene.sceneName)));

                // �ټ��س���
                CoroutineLauncher.active.StartCoroutine(LoadingScene(scene.sceneName, model, onLoaded)); 
            //});

        }

        private static IEnumerator LoadingScene(string sceneName, LoadSceneMode model, System.Action onLoaded)
        {
            activeAsyncOperation = SceneManager.LoadSceneAsync(sceneName, model);
            while (!activeAsyncOperation.isDone)
            {
                yield return null;

            }

            yield return null;

            onLoaded?.Invoke();

            activeAsyncOperation = null;
        }

        /// <summary>
        /// ж�س���
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
