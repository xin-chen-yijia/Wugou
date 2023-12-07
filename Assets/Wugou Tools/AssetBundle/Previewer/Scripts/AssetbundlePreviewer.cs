using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using System.Threading.Tasks;
using System.IO;
#if WUGOU_XR
using Wugou.XR;
#endif

using Wugou.UI;
namespace Wugou.AssetbundlePreviewer
{
    public class AssetbundlePreviewer : MonoBehaviour
    {
        public GameObject originCamera;

        // for vr mode
        public GameObject steamVRObj;

        private string loadedSceneName_ = "";
        private List<GameObject> loadedObjects_ = new List<GameObject>();

        public static AssetbundlePreviewer instance;

        public LoadingPage loadingPage;


        /// <summary>
        /// assetbundle loader
        /// </summary>
        public AssetBundleAssetLoader assetbundleLoader { private set; get; } = null;

        private FlyCamera flyCamera_ = null;


        private void Awake()
        {
            DontDestroyOnLoad(this);
            instance = this; ;
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnApplicationQuit()
        {
            if (assetbundleLoader != null)
            {
                assetbundleLoader.UnloadAssetBundle(true);
            }
        }

        public async Task<AssetBundleAssetLoader> LoadAssetbundle(string assetsDir)
        {
            // 更新信息
            if (assetbundleLoader != null)
            {
                assetbundleLoader.UnloadAssetBundle(false);
            }

            assetbundleLoader = await AssetBundleAssetLoader.GetOrCreate(assetsDir);
            if (assetbundleLoader == null)
            {
                LogWindow.instance.Log("AsyncAssetBundleLoader init failed .... ");
                return null;
            }

            return assetbundleLoader;
        }

        public async void LoadAssets(List<string> paths)
        {
            // 卸载
            UnloadAll();

            if (assetbundleLoader == null)
            {
                return;
            }

            loadingPage.Show(true);
            loadingPage.SetProgress(0.0f);

            // 先加载场景
            string sceneName = "";
            foreach (var path in paths)
            {
                if(path.EndsWith(".unity"))
                {
                    sceneName = path;
                    if (loadedSceneName_ == sceneName)
                    {
                        LogWindow.instance.Log($"{sceneName} already loaded...");
                        sceneName = "";
                        continue;
                    }

                    break;  // 只加载第一个场景
                }
            }

            if (!string.IsNullOrEmpty(sceneName))
            {
                // load scene
                var sceneBundle = await assetbundleLoader.LoadAssetBundleAsync(assetbundleLoader.GetAssetBundleByAssetName(sceneName));
                loadingPage.SetProgress(0.32f);
                if (!sceneBundle)
                {
                    sceneName = "Empty";
                    LogWindow.instance.Log($"Assetbundle not contain's scene: ${sceneName}");
                }

                //sceneName = sceneName.Replace(".unity", "");
                var ops = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                StartCoroutine(LoadingScene(ops, sceneName));
                await ops;

                loadingPage.SetProgress(0.82f);
            }


            int cc = 0;
            foreach (var path in paths)
            {
                // load prefab
                if (!path.EndsWith(".unity"))
                {
                    var assetPfb = await assetbundleLoader.LoadAssetAsync<GameObject>(path);
                    if (!assetPfb)
                    {
                        LogWindow.instance.Log($"there have no object {path} in assetbundle....");
                        return;
                    }

                    // 禁用脚本
                    var go = GameObject.Instantiate<GameObject>(assetPfb);
                    foreach(var v in go.GetComponents<MonoBehaviour>())
                    {
                        v.enabled = false;
                    }
                    loadedObjects_.Add(go);

                    ++cc;
                    loadingPage.SetProgress(0.82f + 0.18f * cc / paths.Count);
                }
            }

            loadingPage.Hide();
        }

        /// <summary>
        /// 加载场景
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        IEnumerator LoadingScene(AsyncOperation ops, string sceneName)
        {
            while (!ops.isDone)
            {
                loadingPage.SetProgress(0.32f + ops.progress * 0.5f); 

                yield return new WaitForEndOfFrame();
            }

            yield return null;

            //originCamera.SetActive(originCamera.scene.name == sceneName);
            loadedSceneName_ = sceneName;

            // 摄像机启用 FlyCamera
            var cams = GameObject.FindGameObjectsWithTag("MainCamera");
            if (cams.Length <= 0)
            {
                LogWindow.instance.Log($"{sceneName} have no MainCamera");
            }

            foreach (var cam in cams)
            {
                if (cam.gameObject.activeInHierarchy && cam != originCamera)
                {
                    flyCamera_ = cam.AddComponent<FlyCamera>();
                    flyCamera_.moveSpeed = 30;
                    flyCamera_.xRotSpeed = 180;
                    flyCamera_.yRotSpeed = 90;

                    // move to start posisition
                    //GameObject startPosParent = GameObject.Find("StartPositions");
                    //Debug.Assert(startPosParent != null && startPosParent.transform.childCount > 0);
                    

                    break;
                }
            }
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            // camera replace
            //print(originCamera.scene.name);
            //print(scene.name);

        }

        public void UnloadAll()
        {
            //if (!string.IsNullOrEmpty(loadedSceneName_))
            //{
            //    await SceneManager.UnloadSceneAsync(loadedSceneName_);
            //    loadedSceneName_ = "";

            //    assetbundleLoader.Unload(true);
            //}

            foreach (var v in loadedObjects_)
            {
                GameObject.Destroy(v);
            }
            loadedObjects_.Clear();

            if (originCamera)
            {
                originCamera.SetActive(true);
            }
        }

        public void EnterVR()
        {
#if WUGOU_XR
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
#endif
        }

        public void ExitVR()
        {
#if WUGOU_XR
            isVRMode = false;
            XRSystem.StopXR();

            steamVRObj.SetActive(false);

            // 相机操作
            originCamera.SetActive(true);
#endif
        }
    }
}

