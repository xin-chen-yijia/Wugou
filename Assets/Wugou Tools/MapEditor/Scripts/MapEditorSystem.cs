using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wugou.UI;
using Wugou.MapEditor.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Wugou.MapEditor
{
    /// <summary>
    /// 可拖放物体描述，包括名称、icon等属性
    /// </summary>
    public class AssetItem
    {
        public string name;
        public string asset;
        public string type;
    }

    /// <summary>
    /// 可拖放物体集合，为了避免在每一个DraggableItemDesc都有一个assetbundle的属性,好管理
    /// </summary>
    public class AssetItemGroup
    {
        public string name;
        public List<AssetItem> items;
    }

    /// <summary>
    /// 编辑地图、场景，比如向场景中放置目标物体或配置其它属性
    /// </summary>
    public class MapEditorSystem : MonoBehaviour
    {
        public static MapEditorSystem instance { get; private set; }

        // 编辑器UI
        public UIRootWindow uiRootWindow;

        // 场景脚本
        public GameMap loadedGameMap { get; private set; }

        /// <summary>
        /// 是否使用飞行视角
        /// </summary>
        public bool useFlyCamera = true;

        // 坐标轴
        public EditorAxis editorAxis;

        /// <summary>
        /// 操作模式 
        /// </summary>
        public enum OptionModel
        {
            kView = 0,
            kTranslate,
            kRotate,
            kScale,
            kAttach,        // 附着在物体表面
        };
        private OptionModel curOptionMode_ = OptionModel.kAttach;
        public OptionModel optionModel => curOptionMode_;

        // 当前编辑摄像机
        private Camera editorCamera_ = null;
        public Camera editorCamera
        {
            get { return editorCamera_; }
            set
            {
                editorCamera_ = value;

                editorAxis.editorCamera = value;
                editorAxis.mainLayerMask = 1 << mapEditorLayer;

                frameObj_.transform.SetParent(editorCamera_.transform);
                frameObj_.transform.localPosition = new Vector3(0, 0, 10);  // 避免被剔除

                EnableOutline();    // 描边
            }
        }
        public int terrainLayer;    //地形所在层
        public float maxRayDistance = 1000;

        private GameObject currentPickedObj_; // 要放置的物体
        public GameObject selectedObject { get; private set; } // 当前选择的物体

        // AssetBundle包信息
        private Dictionary<int, AssetBundleDesc> assetbundles_ = new Dictionary<int, AssetBundleDesc>();
        // 可拖放物体信息
        private List<AssetItemGroup> draggableAssets_ = new List<AssetItemGroup>();

        public const string mapEditorLayerName = "MapEditor";
        public static int mapEditorLayer => LayerMask.NameToLayer(mapEditorLayerName); // 所有选择的物体所在层

        private const string mapToolLayerName = "MapTool";
        public static int mapToolLayer => LayerMask.NameToLayer(mapToolLayerName);   // 工具物体所在层，如坐标轴

        private const string mapGizmosLayerName = "MapGizmos";
        public static int mapGizmosLayer => LayerMask.NameToLayer(mapGizmosLayerName);   // 编辑器辅助物体所在层，比如虚化的物体

        public const string kContentFileName = "editor.json";

        // 选择框
        private GameObject frameObj_;
        private Mesh frameMesh_;

        /// <summary>
        /// 记载完成场景时事件
        /// </summary>
        public static UnityEvent onLoadedMap = new UnityEvent();

        /// <summary>
        /// 选择物体事件
        /// </summary>
        public static UnityEvent<GameObject> onSelectGameEntity = new UnityEvent<GameObject>();

        /// <summary>
        /// 放下物体后调用
        /// </summary>
        public static UnityEvent<GameObject> onGameEntityAdd = new UnityEvent<GameObject>();

        /// <summary>
        /// 删除物体事件
        /// </summary>
        public static UnityEvent<GameObject> onGameEntityRemoved = new UnityEvent<GameObject>();

        /// <summary>
        /// 保存地图时调用
        /// </summary>
        public static UnityEvent<string, GameMap> onSaveGameMap = new UnityEvent<string, GameMap>();

        /// <summary>
        /// 用于停止 
        /// </summary>
        private Coroutine lookAtCoroutine_ = null;  

        /// <summary>
        /// 有多少分组
        /// </summary>
        public int groupCount => draggableAssets_.Count;

        /// <summary>
        /// 每一个分组中有多少物体
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public int GetItemsCount(int group)
        {
            if (group < groupCount)
            {
                return draggableAssets_[group].items.Count;
            }

            return 0;
        }

        /// <summary>
        /// 获取名称
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public string GetGroupName(int group)
        {
            if (group >= groupCount)
            {
                return "";
            }
            return draggableAssets_[group].name;
        }

        /// <summary>
        /// 获取具体的物体描述信息
        /// </summary>
        /// <param name="group"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public AssetItem GetItem(int group, int index)
        {
            if (group < groupCount && index < draggableAssets_[group].items.Count)
            {
                return draggableAssets_[group].items[index];
            }

            Logger.Error($"group:{group} index:{index} out of range..");
            return null;
        }


        #region Unity3D Functions

        public virtual void Awake()
        {
            Debug.Assert(instance == null);
            instance = this;

            // 框选，不用UI的原因是UI更新会影响Unity的主流程，比如Input.GetMouseButtonUp的判定
            frameMesh_ = new Mesh();
            frameMesh_.vertices = new Vector3[4]
            {
            new Vector3(-1.0f, -1.0f, 0.0f),
            new Vector3(-1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, -1.0f, 0.0f),
            };

            frameMesh_.triangles = new int[6]
            {
                2,1,0,0,3,2
            };

            frameObj_ = GameObject.CreatePrimitive(PrimitiveType.Quad);
            frameObj_.name = "Frame Select";
            GameObject.DestroyImmediate(frameObj_.GetComponent<Collider>());
            frameObj_.GetComponent<MeshFilter>().sharedMesh = frameMesh_;

            Material frameMat = new Material(Shader.Find("Wugou/Frame"));
            frameMat.color = new Color(0, 175.0f / 255, 1.0f, 61.0f / 255);
            frameObj_.GetComponent<MeshRenderer>().material = frameMat;
            frameObj_.transform.SetParent(transform);
            frameObj_.SetActive(false);
        }

        // Start is called before the first frame update
        public virtual void Start()
        {

        }

        // Update is called once per frame
        public virtual void Update()
        {
            // 这些事件当鼠标在UI上时也能响应
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                RemoveObject(selectedObject);
            }

            if (Input.GetKeyUp(KeyCode.F))
            {
                if (selectedObject != null)
                {
                    if (lookAtCoroutine_ != null)
                    {
                        StopCoroutine(lookAtCoroutine_);
                    }
                    lookAtCoroutine_ = StartCoroutine(LookAtTarget(selectedObject.gameObject.transform));
                }
            }

            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S))
            {
                // 保存
                if (string.IsNullOrEmpty(GamePlay.loadedGameMapFile))
                {
                    uiRootWindow.GetChildWindow<GameMapSavePage>().Show();
                }
                else
                {
                    SaveGameMap(GamePlay.loadedGameMapFile);
                }
            }

            // ui
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            
            if (!editorCamera)
            {
                return;
            }

            // 物体选择等操作
            ObjectOptionalInternal();

        }

        #endregion

        /// <summary>
        /// 加载脚本信息，MapEditorSystem编辑的是脚本信息
        /// </summary>
        /// <param name="map"></param>
        public static void StartEditor(GameMap map)
        {
            // 先加载编辑器
            CoroutineLauncher.active.StartCoroutine(LoadingMapEditorScene(map));
        }

        /// <summary>
        /// 加载编辑器
        /// </summary>
        /// <returns></returns>
        static IEnumerator LoadingMapEditorScene(GameMap map)
        {
            // 这是还未加载编辑器场景时显示的加载界面，加载编辑器后还有一个 loading page
            var rootWindow = GameObject.FindObjectOfType<UIRootWindow>();
            if(rootWindow && rootWindow.GetChildWindow<LoadingScenePage>())
            {
                var loadingPage = rootWindow.GetChildWindow<LoadingScenePage>();
                loadingPage.Show();
                loadingPage.SetProgress(0);
            }
            else
            {
                Wugou.Logger.Warning("There no LoadingScenePage....");
            }


            var op = SceneManager.LoadSceneAsync(GamePlay.settings.mapEditorSceneName, LoadSceneMode.Single);
            yield return op;

            while (!op.isDone || instance == null)
            {
                yield return null;
            }

            // 加载脚本
            instance.LoadMap(map, () => {
                onLoadedMap.Invoke(); 
            }, LoadSceneMode.Additive);

            //
            _ = instance.ReadAssets();

            //
            instance.loadedGameMap = map;
        }

        public static void StopEditor()
        {
            if (instance)
            {
                instance.StopEdtorInternal();
            }

        }

        private void StopEdtorInternal()
        {
            UnloadGameMap();

            GamePlay.loadedGameMapFile = string.Empty;
            //
            currentPickedObj_ = null;
            lookAtCoroutine_ = null;

            // 加载主场景
            SceneManager.LoadScene(GamePlay.settings.mainSceneName);

            // 实例销毁了
            instance = null;
        }

        public static void Reset()
        {
            // clear events
            onLoadedMap.RemoveAllListeners();
            onGameEntityAdd.RemoveAllListeners();
            onGameEntityRemoved.RemoveAllListeners();
            onSelectGameEntity.RemoveAllListeners();
            onSaveGameMap.RemoveAllListeners();
        }

        /// <summary>
        /// 读取资产内容
        /// </summary>
        /// <returns></returns>
        private async Task<bool> ReadAssets()
        {
            string file = $"{GamePlay.settings.configPath}/{kContentFileName}";

            // load prefab description
            var content = await FileHelper.ReadText(file);
            
            var assets = JsonConvert.DeserializeObject<List<AssetItemGroup>>(content);
            draggableAssets_.AddRange(assets);

            // ui update
            uiRootWindow.GetChildWindow<AssetsPage>().UpdateBoard();

            return true;
        }

        /// <summary>
        /// 注册一个新类型
        /// </summary>
        /// <param name="assets"></param>
        public void Register(AssetItemGroup assets)
        {
            draggableAssets_.Add(assets);
        }
        
        public async void PickUp(AssetItem assetItem)
        {
            if (currentPickedObj_)
            {
                return;
            }

            var entity = CreateEntity(assetItem);
            SelectObjectInternal(null);

            // pickup
            currentPickedObj_ = entity.gameObject;
            currentPickedObj_.SetActive(false); // 先隐藏， 在放置到地面上时才显示

            await GameEntity.Instantiate(entity.GetComponent<GameEntity>());

            // move to layer
            Utils.SetLayerRecursively(entity.gameObject, mapEditorLayer);
        }

        public void PutDown()
        {
            if (currentPickedObj_ != null)
            {
                //if (!isPickedObjKinematic_ && currentPickedObj_.GetComponent<Rigidbody>())
                //{
                //    currentPickedObj_.GetComponent<Rigidbody>().isKinematic = false;
                //}

                //foreach (var v in cachedBehaviours_)
                //{
                //    v.enabled = true;
                //}


                //foreach (var v in currentPickedObj_.GetComponents<Collider>())
                //{
                //    v.enabled = true;
                //}

                // 添加物体
                AddObjectInternal(currentPickedObj_);

                // 同时选择物体
                SelectObjectInternal(currentPickedObj_);

                currentPickedObj_ = null;
            }
        }

        private void AddObjectInternal(GameObject go)
        {
            onGameEntityAdd?.Invoke(go);
        }

        private void RemoveObjectInternal(GameObject obj)
        {
            if (GameWorld.Exists(obj))
            {
                GameWorld.RemoveGameObject(obj);
                OnRemoveObject(obj);
            }
        }

        /// <summary>
        /// 删除物体
        /// </summary>
        /// <param name="obj"></param>
        private void OnRemoveObject(GameObject obj)
        {
            onGameEntityRemoved.Invoke(obj);
        }

        private void SelectObjectInternal(GameObject obj)
        {
            if (obj)
            {
                // 确定根物体
                while (obj.transform.parent && obj.transform.parent.gameObject.layer == obj.gameObject.layer)
                {
                    obj = obj.transform.parent.gameObject;
                }

                if (GameWorld.Exists(obj))
                {
                    // handle outline
                    SetOutlineEnabled(selectedObject, false);

                    selectedObject = obj;

                    // axis
                    editorAxis.SetSelectedObject(obj);

                    onSelectGameEntity?.Invoke(selectedObject);

                    // 显示属性
                    uiRootWindow.GetChildWindow<InspectorPage>().SetTarget(selectedObject);
                    uiRootWindow.GetChildWindow<InspectorPage>().Show();

                    // 显示选中状态
                    SetOutlineEnabled(selectedObject, true);
                }
                else
                {
                    Logger.Error($"{obj.name} not exists in GameWorld....");
                }
            }
            else
            {
                // 
                if (selectedObject && selectedObject.GetComponent<cakeslice.Outline>())
                {
                    selectedObject.GetComponent<cakeslice.Outline>().enabled = false;
                }

                // handle outline
                SetOutlineEnabled(selectedObject, false);

                selectedObject = null;

                // 显示属性
                uiRootWindow.GetChildWindow<InspectorPage>().SetTarget(null);
                uiRootWindow.GetChildWindow<InspectorPage>().Show();
            }
        }

        /// <summary>
        /// 实例化对象
        /// </summary>
        /// <param name="assetItem"></param>
        /// <returns></returns>
        private GameEntity CreateEntity(AssetItem assetItem)
        {
            // 记录，用于添加到脚本中
            string prototype = assetItem.type;
            var entity = GameWorld.AddGameEntity(assetItem.asset, assetItem.type);

            // move to layer
            Utils.SetLayerRecursively(entity.gameObject, mapEditorLayer);

            return entity;
        }

        /// <summary>
        /// 选择一个物体
        /// </summary>
        /// <param name="gameObject"></param>
        public void SelectObject(GameObject gameObject)
        {
            SelectObjectInternal(gameObject);
        }

        /// <summary>
        /// 删除物体
        /// </summary>
        /// <param name="gameObject"></param>
        public void RemoveObject(GameObject gameObject)
        {
            if (selectedObject == gameObject)
            {
                SelectObjectInternal(null);
                RemoveObjectInternal(gameObject);
            }
            else
            {
                RemoveObjectInternal(gameObject);
            }
        }

        public bool isPickingUp => currentPickedObj_ != null;   //是否正在放置物体

        public void SwitchOptionMode(OptionModel mode)
        {
            curOptionMode_ = mode;

            // 鼠标样式修改
            editorAxis.SetOptionMode((EditorAxis.Mode)(mode));
            editorAxis.enabled = (mode == OptionModel.kTranslate || mode == OptionModel.kRotate || mode == OptionModel.kScale);
            if(mode == OptionModel.kAttach || mode == OptionModel.kView)
            {
                editorAxis.SetOptionModeWithoutNotify(EditorAxis.Mode.kNone);
            }

            // 鼠标控制视角移动只能在view模式
            if(useFlyCamera)
            {
                editorCamera.GetComponent<FlyCamera>().moveEnable = (optionModel == OptionModel.kView);
            }
        }

        IEnumerator LookAtTarget(Transform target)
        {
            if (useFlyCamera)
            {
                FlyCamera cam = editorCamera.GetComponent<FlyCamera>();
                while (Vector3.SqrMagnitude((cam.viewCenter - target.position)) > 0.000001f)
                {
                    yield return null;

                    cam.viewCenter = Vector3.Lerp(cam.viewCenter, target.position, 0.8f);
                }
            }
        }

        private Vector3 lastMousePointOnTerrain;    // 记录便宜，避免选择物体后移动
        private bool isShowFrameSelect
        {
            get
            {
                return frameObj_.activeSelf;
            }

            set
            {
                frameObj_.SetActive(value);
            }
        }

        private Vector3 firstFrameSelectPoint;
        /// <summary>
        /// 物体点选、框选等
        /// </summary>
        void ObjectOptionalInternal()
        {
            RaycastHit hit;
            Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);
            if (currentPickedObj_)
            {
                if (Physics.Raycast(ray, out hit, maxRayDistance, 1 << terrainLayer))
                {
                    currentPickedObj_.transform.position = hit.point;
                    currentPickedObj_.transform.up = hit.normal;
                    currentPickedObj_.SetActive(true);
                }

                if (Input.GetMouseButtonDown(0))
                {
                    // 放下物体
                    PutDown();
                }

                return; // 拿着物体，就只能放下，其它的不管
            }

            if (optionModel == OptionModel.kAttach)      // 自动吸附物体表面
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (Physics.Raycast(ray, out hit, maxRayDistance, 1 << mapEditorLayer))
                    {
                        SelectObjectInternal(hit.collider.gameObject);
                        if(Physics.Raycast(ray, out hit, maxRayDistance, 1 << terrainLayer))
                        {
                            lastMousePointOnTerrain = hit.point;
                        }
                    }
                    else
                    {
                        SelectObjectInternal(null);
                    }

                }

                if (Input.GetMouseButton(0) && selectedObject != null && Physics.Raycast(ray, out hit, maxRayDistance, 1 << terrainLayer))
                {
                    selectedObject.transform.position = hit.point;
                    selectedObject.transform.up = hit.normal;
                }

                //if (Input.GetMouseButtonUp(0))
                //{
                //    SelectObjectInternal(null);
                //}
            }

            // 常规物体操作模式
            if (optionModel == OptionModel.kTranslate || optionModel == OptionModel.kRotate || optionModel == OptionModel.kScale)
            {
                if (editorAxis.isDraggingAxis)     // 先判断是否在操作坐标轴
                {
                    isShowFrameSelect = false;
                    return;
                }

                if (!isShowFrameSelect)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        firstFrameSelectPoint = new Vector3(Input.mousePosition.x, Screen.height - Input.mousePosition.y, 0);
                        isShowFrameSelect = true;
                    }
                }
                else
                {
                    if (Input.GetMouseButtonUp(0))
                    {
                        isShowFrameSelect = false;

                        // 用移动距离判断是否是点击
                        if(Vector3.Distance(new Vector3(Input.mousePosition.x, Screen.height - Input.mousePosition.y, 0), firstFrameSelectPoint) < 3.0f)
                        {
                            // 选择物体
                            if (Physics.Raycast(ray, out hit, maxRayDistance, 1 << mapEditorLayer) && hit.collider != null)
                            {
                                SelectObjectInternal(hit.collider.gameObject);
                            }
                            else
                            {
                                SelectObjectInternal(null);
                            }
                        }

                        return;
                    }

                    var leftTop = firstFrameSelectPoint;
                    var rightBottom = new Vector3(Input.mousePosition.x, Screen.height - Input.mousePosition.y, 0);

                    // 检测相对位置
                    if (leftTop.x > rightBottom.x)
                    {
                        (leftTop.x, rightBottom.x) = (rightBottom.x, leftTop.x);
                    }
                    if (leftTop.y > rightBottom.y)
                    {
                        (leftTop.y, rightBottom.y) = (rightBottom.y, leftTop.y);
                    }

                    var leftTop01 = new Vector2(leftTop.x / Screen.width, leftTop.y / Screen.height);
                    var rightBottom01 = new Vector2(rightBottom.x / Screen.width, rightBottom.y / Screen.height);

                    // 转换为opengl坐标
                    System.Func<float, float> toGL = (x) => { return 2.0f * x - 1.0f; };
                    leftTop01.x = toGL(leftTop01.x);
                    leftTop01.y = toGL(leftTop01.y);
                    rightBottom01.x = toGL(rightBottom01.x);
                    rightBottom01.y = toGL(rightBottom01.y);

                    // 更新mesh
                    var mesh = frameObj_.GetComponent<MeshFilter>().sharedMesh;
                    mesh.vertices = new Vector3[4]
                    {
                    new Vector3(leftTop01.x, leftTop01.y, 0.0f),
                    new Vector3(leftTop01.x, rightBottom01.y, 0.0f),
                    new Vector3(rightBottom01.x, rightBottom01.y, 0.0f),
                    new Vector3(rightBottom01.x, leftTop01.y, 0.0f),
                    };


                    // 选择第一个
                    var size = rightBottom - leftTop;
                    Rect area = new Rect(leftTop.x,leftTop.y, size.x, size.y);
                    GameObject firstSelected = null;
                    foreach(var v in GameWorld.gameEntities)
                    {
                        var p = editorCamera.WorldToScreenPoint(v.transform.position);
                        p.y = Screen.height - p.y;
                        if (area.Contains(p))
                        {
                            firstSelected = v.gameObject;
                            break;
                        }
                    }

                    if (firstSelected)
                    {
                        SelectObjectInternal(firstSelected);
                    }
                    else
                    {
                        SelectObjectInternal(null);
                    }
                }
            }
        }

        /// <summary>
        /// 加载脚本
        /// </summary>
        /// <param name="map"></param>
        /// <param name="onLoadedMap"></param>
        /// <param name="mode"></param>
        public virtual void LoadMap(GameMap map, Action onLoadedMap, LoadSceneMode mode)
        {
            // loading page
            var loadingPage = uiRootWindow.GetChildWindow<LoadingScenePage>();
            loadingPage.Show();
            loadingPage.SetProgress(0);
            StartCoroutine(UpdateProgressBar(loadingPage));     // 更新进度

            GameWorld.LoadMap(map, async () => {

                loadingPage.SetProgress(1.0f);

                // 等一些插件或者脚本执行初始化
                await new YieldInstructionAwaiter(null).Task;

                // 脚本物体处理一下，比如刚体、脚本、层等内容
                for (int i = 0; i < GameWorld.gameEntities.Count; ++i)
                {
                    var go = GameWorld.gameEntities[i].gameObject;
                    // move to layer
                    Utils.SetLayerRecursively(go, mapEditorLayer);
                }

                var tmpCam = OnCreateEditorCamera();
                editorCamera = tmpCam ? tmpCam : Camera.main; // use main camera
                
                // 飞行视角
                if (useFlyCamera && editorCamera.GetComponent<FlyCamera>() == null)
                {
                    // 避免FlyCamera自己创建父物体，不然还要主动清理
                    GameObject parentObj = new GameObject("FlyCamera");
                    parentObj.tag = "Player";
                    parentObj.transform.position = editorCamera.transform.position + editorCamera.transform.forward * 10.0f;
                    parentObj.transform.rotation = editorCamera.transform.rotation;
                    editorCamera.transform.SetParent(parentObj.transform);

                    // 
                    SceneManager.MoveGameObjectToScene(parentObj, GameWorld.activeScene);

                    var flyCam = editorCamera.gameObject.AddComponent<FlyCamera>();
                    flyCam.moveSpeed = 15;
                    flyCam.xRotSpeed = 90;
                    flyCam.yRotSpeed = 90;

                    editorCamera.GetComponent<FlyCamera>().moveEnable = false;  // 初始是attach模式，不能动
                }

                if (groupCount > 0)
                {
                    var groupDesc = draggableAssets_[0];
                    if (groupDesc.items.Count > 0)
                    {
                        // 先加载，避免第一次点击时等太长时间
                        await GameAssetDatabase.LoadAssetAsync<GameObject>(groupDesc.items[0].asset);
                    }
                }

                // ui
                uiRootWindow.GetChildWindow<MapEditorPage>().SetHead(map.name);
                uiRootWindow.GetChildWindow<AssetsPage>().HideLoadingMask();
                // 
                onLoadedMap?.Invoke();

                loadingPage.Hide();

            }, mode);
        }

        IEnumerator UpdateProgressBar(LoadingScenePage page)
        {
            bool bUpdateProgress = true;
            while (bUpdateProgress)
            {
                if (AssetBundleSceneManager.activeAsyncOperation != null)
                {
                    float p = AssetBundleSceneManager.activeAsyncOperation.progress;
                    page.SetProgress(p);

                    if (AssetBundleSceneManager.activeAsyncOperation.isDone || p > 0.9999999f)
                    {
                        bUpdateProgress = false;
                    }
                }

                yield return null;
            }
        }

        /// <summary>
        /// 卸载脚本 
        /// </summary>
        public virtual void UnloadGameMap()
        {
            // reset
            GameWorld.UnloadGameMap();
        }

        private const string tmpMapFile = "~map";
        public static string tmpMapFilePath => $"{Application.persistentDataPath}/{tmpMapFile}";
        public bool SaveGameMap(string fileName)
        {
            if(GameWorld.GetStartPositionCount() == 0)
            {
                Logger.Error("Save map with start position count 0.");
                uiRootWindow.GetChildWindow<MakeSurePage>().ShowTips("角色初始位置数量不能为0！");
                return false;
            }
            GamePlay.loadedGameMapFile = fileName;

            // 天气写入
            loadedGameMap.weather = WeatherSystem.activeWeather;

            GameMapWriter writer = new GameMapWriter();
            writer.WriteVersion(loadedGameMap.version);
            writer.WriteName(loadedGameMap.name);
            writer.WriteGameWorld();
            writer.WriteGameMapDetail(loadedGameMap);
            writer.Save(tmpMapFilePath);

            onSaveGameMap.Invoke(fileName, loadedGameMap);

            return true;
        }

        /// <summary>
        /// 退出当前编辑
        /// </summary>
        public void Quit()
        {
            uiRootWindow.GetChildWindow<MakeSurePage>().ShowOptions("是否保存当前脚本？", () =>
            {
                // save
                // 保存
                if (string.IsNullOrEmpty(GamePlay.loadedGameMapFile))
                {
                    var page = uiRootWindow.GetChildWindow<GameMapSavePage>();
                    page.Show(StopEditor, null);
                }
                else
                {
                    if (SaveGameMap(GamePlay.loadedGameMapFile))
                    {
                        StopEditor();
                    }

                }
            },
            () =>
            {
                StopEditor();
            },"是", "否");



        }

        private void EnableOutline()
        {
            if (!editorCamera.GetComponent<cakeslice.OutlineEffect>())
            {
                var comp = editorCamera.gameObject.AddComponent<cakeslice.OutlineEffect>();
                comp.lineThickness = 1;
                comp.lineIntensity = 1.51f;
                comp.fillAmount = 0.1f;
                ColorUtility.TryParseHtmlString("#FFC300", out comp.lineColor0);
                ColorUtility.TryParseHtmlString("#BC5BB9", out comp.lineColor1);
                ColorUtility.TryParseHtmlString("#0096FF", out comp.lineColor2);
            }
        }

        private void SetOutlineEnabled(GameObject go, bool enable)
        {
            if (enable)
            {
                foreach (var v in go.GetComponentsInChildren<Renderer>())
                {
                    var outlineComp = v.GetComponent<cakeslice.Outline>();
                    if (outlineComp)
                    {
                        outlineComp.enabled = true;
                    }
                    else
                    {
                        outlineComp = v.gameObject.AddComponent<cakeslice.Outline>();
                        outlineComp.color = 0;
                    }
                }
            }
            else
            {
                if (go)
                {
                    foreach (var v in go.GetComponentsInChildren<cakeslice.Outline>())
                    {
                        v.enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// 用于自定义创建编辑器摄像机
        /// </summary>
        /// <returns></returns>
        protected virtual Camera OnCreateEditorCamera()
        {
            return null;
        }

    }
}
