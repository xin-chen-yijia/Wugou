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
    /// ���Ϸ������������������ơ�icon������
    /// </summary>
    public class AssetItem
    {
        public string name;
        public string asset;
        public string type;
    }

    /// <summary>
    /// ���Ϸ����弯�ϣ�Ϊ�˱�����ÿһ��DraggableItemDesc����һ��assetbundle������,�ù���
    /// </summary>
    public class AssetItemGroup
    {
        public string name;
        public List<AssetItem> items;
    }

    /// <summary>
    /// �༭��ͼ�������������򳡾��з���Ŀ�������������������
    /// </summary>
    public class MapEditorSystem : MonoBehaviour
    {
        public static MapEditorSystem instance { get; private set; }

        // �༭��UI
        public UIRootWindow uiRootWindow;

        // �����ű�
        public GameMap loadedGameMap { get; private set; }

        /// <summary>
        /// �Ƿ�ʹ�÷����ӽ�
        /// </summary>
        public bool useFlyCamera = true;

        // ������
        public EditorAxis editorAxis;

        /// <summary>
        /// ����ģʽ 
        /// </summary>
        public enum OptionModel
        {
            kView = 0,
            kTranslate,
            kRotate,
            kScale,
            kAttach,        // �������������
        };
        private OptionModel curOptionMode_ = OptionModel.kAttach;
        public OptionModel optionModel => curOptionMode_;

        // ��ǰ�༭�����
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
                frameObj_.transform.localPosition = new Vector3(0, 0, 10);  // ���ⱻ�޳�

                EnableOutline();    // ���
            }
        }
        public int terrainLayer;    //�������ڲ�
        public float maxRayDistance = 1000;

        private GameObject currentPickedObj_; // Ҫ���õ�����
        public GameObject selectedObject { get; private set; } // ��ǰѡ�������

        // AssetBundle����Ϣ
        private Dictionary<int, AssetBundleDesc> assetbundles_ = new Dictionary<int, AssetBundleDesc>();
        // ���Ϸ�������Ϣ
        private List<AssetItemGroup> draggableAssets_ = new List<AssetItemGroup>();

        public const string mapEditorLayerName = "MapEditor";
        public static int mapEditorLayer => LayerMask.NameToLayer(mapEditorLayerName); // ����ѡ����������ڲ�

        private const string mapToolLayerName = "MapTool";
        public static int mapToolLayer => LayerMask.NameToLayer(mapToolLayerName);   // �����������ڲ㣬��������

        private const string mapGizmosLayerName = "MapGizmos";
        public static int mapGizmosLayer => LayerMask.NameToLayer(mapGizmosLayerName);   // �༭�������������ڲ㣬�����黯������

        public const string kContentFileName = "editor.json";

        // ѡ���
        private GameObject frameObj_;
        private Mesh frameMesh_;

        /// <summary>
        /// ������ɳ���ʱ�¼�
        /// </summary>
        public static UnityEvent onLoadedMap = new UnityEvent();

        /// <summary>
        /// ѡ�������¼�
        /// </summary>
        public static UnityEvent<GameObject> onSelectGameEntity = new UnityEvent<GameObject>();

        /// <summary>
        /// ������������
        /// </summary>
        public static UnityEvent<GameObject> onGameEntityAdd = new UnityEvent<GameObject>();

        /// <summary>
        /// ɾ�������¼�
        /// </summary>
        public static UnityEvent<GameObject> onGameEntityRemoved = new UnityEvent<GameObject>();

        /// <summary>
        /// �����ͼʱ����
        /// </summary>
        public static UnityEvent<string, GameMap> onSaveGameMap = new UnityEvent<string, GameMap>();

        /// <summary>
        /// ����ֹͣ 
        /// </summary>
        private Coroutine lookAtCoroutine_ = null;  

        /// <summary>
        /// �ж��ٷ���
        /// </summary>
        public int groupCount => draggableAssets_.Count;

        /// <summary>
        /// ÿһ���������ж�������
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
        /// ��ȡ����
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
        /// ��ȡ���������������Ϣ
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

            // ��ѡ������UI��ԭ����UI���»�Ӱ��Unity�������̣�����Input.GetMouseButtonUp���ж�
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
            // ��Щ�¼��������UI��ʱҲ����Ӧ
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
                // ����
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

            // ����ѡ��Ȳ���
            ObjectOptionalInternal();

        }

        #endregion

        /// <summary>
        /// ���ؽű���Ϣ��MapEditorSystem�༭���ǽű���Ϣ
        /// </summary>
        /// <param name="map"></param>
        public static void StartEditor(GameMap map)
        {
            // �ȼ��ر༭��
            CoroutineLauncher.active.StartCoroutine(LoadingMapEditorScene(map));
        }

        /// <summary>
        /// ���ر༭��
        /// </summary>
        /// <returns></returns>
        static IEnumerator LoadingMapEditorScene(GameMap map)
        {
            // ���ǻ�δ���ر༭������ʱ��ʾ�ļ��ؽ��棬���ر༭������һ�� loading page
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

            // ���ؽű�
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

            // ����������
            SceneManager.LoadScene(GamePlay.settings.mainSceneName);

            // ʵ��������
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
        /// ��ȡ�ʲ�����
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
        /// ע��һ��������
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
            currentPickedObj_.SetActive(false); // �����أ� �ڷ��õ�������ʱ����ʾ

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

                // �������
                AddObjectInternal(currentPickedObj_);

                // ͬʱѡ������
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
        /// ɾ������
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
                // ȷ��������
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

                    // ��ʾ����
                    uiRootWindow.GetChildWindow<InspectorPage>().SetTarget(selectedObject);
                    uiRootWindow.GetChildWindow<InspectorPage>().Show();

                    // ��ʾѡ��״̬
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

                // ��ʾ����
                uiRootWindow.GetChildWindow<InspectorPage>().SetTarget(null);
                uiRootWindow.GetChildWindow<InspectorPage>().Show();
            }
        }

        /// <summary>
        /// ʵ��������
        /// </summary>
        /// <param name="assetItem"></param>
        /// <returns></returns>
        private GameEntity CreateEntity(AssetItem assetItem)
        {
            // ��¼��������ӵ��ű���
            string prototype = assetItem.type;
            var entity = GameWorld.AddGameEntity(assetItem.asset, assetItem.type);

            // move to layer
            Utils.SetLayerRecursively(entity.gameObject, mapEditorLayer);

            return entity;
        }

        /// <summary>
        /// ѡ��һ������
        /// </summary>
        /// <param name="gameObject"></param>
        public void SelectObject(GameObject gameObject)
        {
            SelectObjectInternal(gameObject);
        }

        /// <summary>
        /// ɾ������
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

        public bool isPickingUp => currentPickedObj_ != null;   //�Ƿ����ڷ�������

        public void SwitchOptionMode(OptionModel mode)
        {
            curOptionMode_ = mode;

            // �����ʽ�޸�
            editorAxis.SetOptionMode((EditorAxis.Mode)(mode));
            editorAxis.enabled = (mode == OptionModel.kTranslate || mode == OptionModel.kRotate || mode == OptionModel.kScale);
            if(mode == OptionModel.kAttach || mode == OptionModel.kView)
            {
                editorAxis.SetOptionModeWithoutNotify(EditorAxis.Mode.kNone);
            }

            // �������ӽ��ƶ�ֻ����viewģʽ
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

        private Vector3 lastMousePointOnTerrain;    // ��¼���ˣ�����ѡ��������ƶ�
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
        /// �����ѡ����ѡ��
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
                    // ��������
                    PutDown();
                }

                return; // �������壬��ֻ�ܷ��£������Ĳ���
            }

            if (optionModel == OptionModel.kAttach)      // �Զ������������
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

            // �����������ģʽ
            if (optionModel == OptionModel.kTranslate || optionModel == OptionModel.kRotate || optionModel == OptionModel.kScale)
            {
                if (editorAxis.isDraggingAxis)     // ���ж��Ƿ��ڲ���������
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

                        // ���ƶ������ж��Ƿ��ǵ��
                        if(Vector3.Distance(new Vector3(Input.mousePosition.x, Screen.height - Input.mousePosition.y, 0), firstFrameSelectPoint) < 3.0f)
                        {
                            // ѡ������
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

                    // ������λ��
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

                    // ת��Ϊopengl����
                    System.Func<float, float> toGL = (x) => { return 2.0f * x - 1.0f; };
                    leftTop01.x = toGL(leftTop01.x);
                    leftTop01.y = toGL(leftTop01.y);
                    rightBottom01.x = toGL(rightBottom01.x);
                    rightBottom01.y = toGL(rightBottom01.y);

                    // ����mesh
                    var mesh = frameObj_.GetComponent<MeshFilter>().sharedMesh;
                    mesh.vertices = new Vector3[4]
                    {
                    new Vector3(leftTop01.x, leftTop01.y, 0.0f),
                    new Vector3(leftTop01.x, rightBottom01.y, 0.0f),
                    new Vector3(rightBottom01.x, rightBottom01.y, 0.0f),
                    new Vector3(rightBottom01.x, leftTop01.y, 0.0f),
                    };


                    // ѡ���һ��
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
        /// ���ؽű�
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
            StartCoroutine(UpdateProgressBar(loadingPage));     // ���½���

            GameWorld.LoadMap(map, async () => {

                loadingPage.SetProgress(1.0f);

                // ��һЩ������߽ű�ִ�г�ʼ��
                await new YieldInstructionAwaiter(null).Task;

                // �ű����崦��һ�£�������塢�ű����������
                for (int i = 0; i < GameWorld.gameEntities.Count; ++i)
                {
                    var go = GameWorld.gameEntities[i].gameObject;
                    // move to layer
                    Utils.SetLayerRecursively(go, mapEditorLayer);
                }

                var tmpCam = OnCreateEditorCamera();
                editorCamera = tmpCam ? tmpCam : Camera.main; // use main camera
                
                // �����ӽ�
                if (useFlyCamera && editorCamera.GetComponent<FlyCamera>() == null)
                {
                    // ����FlyCamera�Լ����������壬��Ȼ��Ҫ��������
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

                    editorCamera.GetComponent<FlyCamera>().moveEnable = false;  // ��ʼ��attachģʽ�����ܶ�
                }

                if (groupCount > 0)
                {
                    var groupDesc = draggableAssets_[0];
                    if (groupDesc.items.Count > 0)
                    {
                        // �ȼ��أ������һ�ε��ʱ��̫��ʱ��
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
        /// ж�ؽű� 
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
                uiRootWindow.GetChildWindow<MakeSurePage>().ShowTips("��ɫ��ʼλ����������Ϊ0��");
                return false;
            }
            GamePlay.loadedGameMapFile = fileName;

            // ����д��
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
        /// �˳���ǰ�༭
        /// </summary>
        public void Quit()
        {
            uiRootWindow.GetChildWindow<MakeSurePage>().ShowOptions("�Ƿ񱣴浱ǰ�ű���", () =>
            {
                // save
                // ����
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
            },"��", "��");



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
        /// �����Զ��崴���༭�������
        /// </summary>
        /// <returns></returns>
        protected virtual Camera OnCreateEditorCamera()
        {
            return null;
        }

    }
}
