using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

//using UnityEngine.PostProcessing;
//using UniStorm;
using System.Threading.Tasks;
using System.IO;
using System;
using Wugou.UI;
using UnityEditor;
#if WUGOU_XR
using Wugou.XR;
#endif

namespace Wugou.Examples.AssetbundlePreviewer
{
    public class AssetbundlePreviewer : MonoBehaviour
    {
        private List<GameObject> loadedObjects_ = new List<GameObject>();

        public static AssetbundlePreviewer instance;

        public LoadingScenePage loadingPage;

        public UIRootWindow rootWindow;


        /// <summary>
        /// assetbundle loader
        /// </summary>
        public AssetPackageLoader assetbundleLoader { private set; get; } = null;

        private FlyCamera flyCamera_ = null;


        private void Awake()
        {
            DontDestroyOnLoad(this);
            instance = this;

            // 天气系统
            //UnistormWeatherSystemConfig.Apply();
        }

        // Start is called before the first frame update
        void Start()
        {
            // 
            WeatherSystem.Load();
        }

        // Update is called once per frame
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        private void OnApplicationQuit()
        {
            if (assetbundleLoader != null)
            {
                assetbundleLoader.Unload(true);
            }
        }

        public async void LoadAssetBundle(string path)
        {
            UnloadAssetBundle();

            assetbundleLoader = await AssetPackageLoader.GetOrCreate(path);
            if(assetbundleLoader == null)
            {
                Debug.LogError($"load {path} failed...");
                return;
            }
            List<string> assets = new List<string>();
            foreach (var asset in assetbundleLoader.GetAllConetents())
            {
                // 只加载场景和gameobject
                if (asset.EndsWith(".unity") || asset.EndsWith(".prefab"))
                {
                    assets.Add(asset);
                }
            }

            rootWindow.GetChildWindow<AssetsListPage>().SetAssets(assets);
            rootWindow.GetChildWindow<AssetsListPage>().Show();
        }

        public async void LoadScene(string sceneName)
        {
            // load scene
            await assetbundleLoader.LoadAssetBundleAsync(assetbundleLoader.GetAssetBundleByAssetName(sceneName));
            StartCoroutine(LoadingScene(sceneName));

        }

        public async Task<GameObject> LoadAsset(string path)
        {
            try
            {
                // load prefab
                var assetPfb = await assetbundleLoader.LoadAssetAsync<GameObject>(path);
                if (!assetPfb)
                {
                    LogWindow.instance.Log($"there have no object {path} in assetbundle....");
                    return null;
                }

                // 禁用脚本
                GameObject go = GameObject.Instantiate<GameObject>(assetPfb);
                foreach (var v in go.GetComponents<MonoBehaviour>())
                {
                    v.enabled = false;
                }
                loadedObjects_.Add(go);

                go.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 15f;

                rootWindow.GetChildWindow<HierachyPage>().AddObject(go);

                return go;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }

        }

        /// <summary>
        /// 加载场景
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        IEnumerator LoadingScene(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            loadingPage.Show();
            while (!op.isDone)
            {
                yield return null;
            }

            loadingPage.Hide();
            yield return null;
            yield return null;

            // 摄像机启用 FlyCamera
            var cam = Camera.main.gameObject;
            flyCamera_ = cam.AddComponent<FlyCamera>();
            flyCamera_.moveSpeed = 30;
            flyCamera_.xRotSpeed = 180;
            flyCamera_.yRotSpeed = 90;

            WeatherSystem.Load();
            EnableOutline();
        }
        //描边
        private void EnableOutline()
        {
            if (!Camera.main.GetComponent<cakeslice.OutlineEffect>())
            {
                var comp = Camera.main.gameObject.AddComponent<cakeslice.OutlineEffect>();
                comp.lineThickness = 1;
                comp.lineIntensity = 1.51f;
                comp.fillAmount = 0.1f;
                ColorUtility.TryParseHtmlString("#FFC300", out comp.lineColor0);
                ColorUtility.TryParseHtmlString("#BC5BB9", out comp.lineColor1);
                ColorUtility.TryParseHtmlString("#0096FF", out comp.lineColor2);
            }
        }
        public void UnloadAssetBundle()
        {
            if(assetbundleLoader != null)
            {
                assetbundleLoader.UnloadAsync(true);
                rootWindow.GetChildWindow<HierachyPage>().ClearObjs();
            }
            //SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
            //SceneManager.LoadScene("AssetbundlePreviewer");
        }

#if WUGOU_XR
        // for vr mode
        public GameObject steamVRObj;

        public void EnterVR()
        {

            isVRMode = true;
            XRSystem.StartXR(0, () =>
            {
                // 相机操作
                originCamera.SetActive(false);

                steamVRObj.SetActive(true);
                if (flyCamera_)
                {
                    steamVRObj.transform.position = flyCamera_.transform.position;
                }
            });
        }

        public void ExitVR()
        {
            isVRMode = false;
            XRSystem.StopXR();

            steamVRObj.SetActive(false);

            // 相机操作
            originCamera.SetActive(true);
        }
#endif
    }
}

