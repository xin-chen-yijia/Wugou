using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wugou.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Wugou.MapEditor
{
    /// <summary>
    /// ���Ϸ������������������ơ�icon������
    /// </summary>
    public class DraggableItemDesc
    {
        public string name;
        public string type;
        public AssetBundleAsset assetDesc;
        public string icon;
    }

    /// <summary>
    /// ���Ϸ����弯�ϣ�Ϊ�˱�����ÿһ��DraggableItemDesc����һ��assetbundle������,�ù���
    /// </summary>
    public class AssetBundleDraggablesDesc
    {
        public string name;
        public List<DraggableItemDesc> items;
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

        // ������
        public GameObject editorAixsObj;
        public EditorAxis editorAxis { get; private set; } = null;

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
                editorAxis.editorCameraObj = editorCamera_.gameObject;

                frameObj_.transform.SetParent(editorCamera_.transform);
                frameObj_.transform.localPosition = new Vector3(0, 0, 10);  // ���ⱻ�޳�
            }
        }
        public int terrainLayer;    //�������ڲ�
        public float maxRayDistance = 1000;

        private GameObject currentPickedObj_; // Ҫ���õ�����
        public GameObject selectedObject { get; private set; } // ��ǰѡ�������

        // AssetBundle����Ϣ
        private Dictionary<int, AssetBundleDesc> assetbundles_ = new Dictionary<int, AssetBundleDesc>();
        // ���Ϸ�������Ϣ
        private List<AssetBundleDraggablesDesc> draggableAssets_ = new List<AssetBundleDraggablesDesc>();

        public const string mapEditorLayerName = "MapEditor";
        public static int mapEditorLayer => LayerMask.NameToLayer(mapEditorLayerName); // ����ѡ����������ڲ�

        public const string kContentFileName = "editor.json";

        // ѡ���
        private GameObject frameObj_;
        private Mesh frameMesh_;

        /// <summary>
        /// ѡ�������¼�
        /// </summary>
        public UnityEvent<GameObject> onSelectGameEntity = new UnityEvent<GameObject>();

        /// <summary>
        /// ������������
        /// </summary>
        public UnityEvent<GameObject> onGameEntityAdd = new UnityEvent<GameObject>();

        /// <summary>
        /// ɾ�������¼�
        /// </summary>
        public UnityEvent<GameObject> onGameEntityRemoved = new UnityEvent<GameObject>();

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
        public DraggableItemDesc GetItem(int group, int index)
        {
            if (group < groupCount && index < draggableAssets_[group].items.Count)
            {
                return draggableAssets_[group].items[index];
            }

            return null;
        }


        #region Unity3D Functions

        public virtual void Awake()
        {
            Debug.Assert(instance == null);
            instance = this;

            // ������
            editorAxis = new EditorAxis(editorAixsObj);

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
            // ��UI��ʱ������ҲҪ����Ŀ�궯
            editorAxis.UpdatePosition();

            // ui
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            
            if (!editorCamera)
            {
                return;
            }

            // �����ӽ�
            if (editorCamera.GetComponent<FlyCamera>() == null)
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

            // ��Ҫ���������е�һЩ�߼��ͱ༭�������к�ǿ������ԣ�����update�ŵ�eidtor��update�У�ȷ��ִ��˳��
            editorAxis.Update();  

            // ����ѡ��Ȳ���
            ObjectOptionalInternal();


            if (Input.GetKeyDown(KeyCode.Delete))
            {
                if (selectedObject != null)
                {
                    RemoveObjectInternal(selectedObject);
                    SelectObjectInternal(null);
                }
            }

            if (Input.GetKeyUp(KeyCode.F))
            {
                if(selectedObject != null)
                {
                    if(lookAtCoroutine_ != null)
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
                    uiRootWindow.GetChildWindow<GameMapBasicInfoPage>().Show();
                }
                else
                {
                    SaveMap(GamePlay.loadedGameMapFile);
                }
            }

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
            var op = SceneManager.LoadSceneAsync(GamePlay.kMapEditorSceneName, LoadSceneMode.Single);
            yield return op;

            while (!op.isDone || instance == null)
            {
                yield return null;
            }

            // ���ؽű�
            instance.LoadMap(map, null, LoadSceneMode.Additive);

            //
            instance.SetResourceDir(GameMapManager.resourceDir);

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

            // clear events
            onGameEntityAdd.RemoveAllListeners();
            onGameEntityRemoved.RemoveAllListeners();
            onSelectGameEntity.RemoveAllListeners();

            // ����������
            SceneManager.LoadScene(GamePlay.kMainSceneName);

            // ʵ��������
            instance = null;

            //uiRootWindow.GetChildWindow<MapEditorPage>().Hide();
        }


        /// <summary>
        /// ��Դ·����Ĭ����Application.streamingAssetsPath
        /// </summary>
        public string resourceDir { get; private set; }

        /// <summary>
        /// ���������ļ���ʼ��
        /// </summary>
        /// <param name="resourceRootPath"></param>
        /// <returns></returns>
        public bool SetResourceDir(string resourceRootPath)
        {
            string file = Path.Combine(resourceRootPath, kContentFileName);
            if (!File.Exists(file))
            {
                Logger.Error($"{file} not exists...");
                return false;
            }

            // load prefab description
            var content = File.ReadAllText(file);

            JObject jo = JObject.Parse(content);
            // ����AB��
            List<AssetBundleDesc> bundles = JsonConvert.DeserializeObject<List<AssetBundleDesc>>(jo["assetbundles"].ToString());
            assetbundles_ = bundles.ToDictionary(p=>p.id);

            AssetBundleDescHashConvert abConvert = new AssetBundleDescHashConvert(assetbundles_);
            draggableAssets_ = JsonConvert.DeserializeObject<List<AssetBundleDraggablesDesc>>(jo["groups"].ToString(), abConvert);

            //
            resourceDir = resourceRootPath;

            // ui update
            uiRootWindow.GetChildWindow<AssetsPage>().UpdateModelBoard(MapEditorSystem.instance.resourceDir);

            return true;
        }

        /// <summary>
        /// ע��һ��������
        /// </summary>
        /// <param name="assets"></param>
        public void Register(AssetBundleDraggablesDesc assets)
        {
            draggableAssets_.Add(assets);
        }
        
        /// <summary>
        /// ����ָ���ʲ����ڴ�
        /// </summary>
        /// <param name="group"></param>
        /// <param name="index"></param>
        /// <param name="onLoaded"></param>
        public async void LoadAsset(int group, int index, System.Action onLoaded = null)
        {
            var groupDesc = draggableAssets_[group];
            if (!(group < groupCount && index < groupDesc.items.Count))
            {
                Logger.Error($"group:{group} index:{index} out of range..");
                return;
            }

            // ��¼��������ӵ��ű���
            var assetDesc = groupDesc.items[index].assetDesc;
            var loader = await AssetBundleAssetLoader.GetOrCreate($"{resourceDir}/{assetDesc.assetbundle.path}");
            var goMem = await loader.LoadAssetAsync<GameObject>(assetDesc.asset);

            onLoaded?.Invoke();
        }

        public async void PickUp(int group,int index)
        {
            var entity = CreateEntity(group, index);
            await GameEntityManager.InstantiateGameObject(entity.GetComponent<GameEntity>());
            if(entity == null) { 
                return; 
            }

            SelectObjectInternal(null);

            // pickup
            currentPickedObj_ = entity.gameObject;
            currentPickedObj_.SetActive(false); // �����أ� �ڷ��õ�������ʱ����ʾ
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
                onGameEntityRemoved.Invoke(obj);
                RemoveObject(obj);
            }

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
                    selectedObject = obj;
                    if (editorAxis != null && (optionModel == OptionModel.kTranslate || optionModel == OptionModel.kRotate || optionModel == OptionModel.kScale))
                    {
                        // ��������ʾ
                        editorAxis.attachedObject = selectedObject;
                    }

                    onSelectGameEntity?.Invoke(selectedObject);

                    // ��ʾ����
                    uiRootWindow.GetChildWindow<InspectorPage>().SetTarget(selectedObject);
                    uiRootWindow.GetChildWindow<InspectorPage>().Show();
                }
                else
                {
                    Logger.Error($"{obj.name} not exists in GameWorld....");
                }
            }
            else
            {
                // udpate info
                if (selectedObject)
                {
                    //GameWorld.UpdateSceneObject(selectedObject);
                }

                selectedObject = null;
                if (editorAxis != null)
                {
                    editorAxis.attachedObject = null;
                }

                // ��ʾ����
                uiRootWindow.GetChildWindow<InspectorPage>().SetTarget(null);
                uiRootWindow.GetChildWindow<InspectorPage>().Show();
            }
        }

        /// <summary>
        /// ʵ��������
        /// </summary>
        /// <param name="group"></param>
        /// <param name="index"></param>
        private GameEntity CreateEntity(int group, int index)
        {
            var groupDesc = draggableAssets_[group];
            if (!(group < groupCount && index < groupDesc.items.Count))
            {
                Logger.Error($"group:{group} index:{index} out of range..");
                return null;
            }

            // ��¼��������ӵ��ű���
            string prototype = groupDesc.items[index].type;
            var entity = GameWorld.AddGameEntity(groupDesc.items[index].assetDesc, groupDesc.items[index].type);

            // move to layer
            Utils.SetLayerRecursively(entity.gameObject, mapEditorLayer);

            return entity;
        }

        /// <summary>
        /// ɾ������
        /// </summary>
        /// <param name="obj"></param>
        private void RemoveObject(GameObject obj)
        {
            GameWorld.RemoveGameObject(obj);
        }

        public bool isPickingUp => currentPickedObj_ != null;   //�Ƿ����ڷ�������

        public void SwitchOptionMode(OptionModel mode)
        {
            curOptionMode_ = mode;

            // �����ʽ�޸�
            editorAxis.SetMode((EditorAxis.Mode)(mode));
            if(mode == OptionModel.kAttach || mode == OptionModel.kView)
            {
                editorAxis.Hide();
            }
            else
            {
                if (editorAxis.attachedObject)
                {
                    editorAxis.Show();
                }
            }

            // �������ӽ��ƶ�ֻ����viewģʽ
            editorCamera.GetComponent<FlyCamera>().moveEnable = (optionModel == OptionModel.kView);
        }

        IEnumerator LookAtTarget(Transform target)
        {
            FlyCamera cam = editorCamera.GetComponent<FlyCamera>();
            while (Vector3.SqrMagnitude((cam.viewCenter - target.position)) > 0.000001f)
            {
                yield return null;

                cam.viewCenter = Vector3.Lerp(cam.viewCenter, target.position, 0.8f);
            }
        }

        private bool showFrameSelect = false;
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

                if (Input.GetMouseButtonUp(0))
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
                if (editorAxis.isDragging)     // ���ж��Ƿ��ڲ���������
                {
                    return;
                }

                if (!showFrameSelect)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        firstFrameSelectPoint = new Vector3(Input.mousePosition.x, Screen.height - Input.mousePosition.y, 0);
                        showFrameSelect = true;
                    }
                }
                else
                {
                    if (Input.GetMouseButtonUp(0))
                    {
                        frameObj_.SetActive(false);
                        showFrameSelect = false;

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

                    frameObj_.SetActive(true);
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

            GameWorld.LoadMap(map, () => {

                // Ӧ������
                var weather = WeatherSystem.activeWeather;
                weather.type = map.weather.type;
                weather.time = (map.weather.time);
                WeatherSystem.ApplyWeather();

                // �ű����崦��һ�£�������塢�ű����������
                for (int i = 0; i < GameWorld.gameEntities.Count; ++i)
                {
                    var go = GameWorld.gameEntities[i].gameObject;
                    // move to layer
                    Utils.SetLayerRecursively(go, mapEditorLayer);
                }

                // use main camera
                editorCamera = Camera.main;

                // ui
                uiRootWindow.GetChildWindow<MapEditorPage>().SetHead(map.name);

                // �ȼ��أ������һ�ε��ʱ��̫��ʱ��
                LoadAsset(0, 0, () =>
                {
                    uiRootWindow.GetChildWindow<AssetsPage>().HideLoadingMask();
                });

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

        public bool SaveMap(string fileName)
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
            return GameMapManager.SaveGameMap(fileName, loadedGameMap);
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
                    uiRootWindow.GetChildWindow<GameMapBasicInfoPage>().Show();
                }
                else
                {
                    if (SaveMap(GamePlay.loadedGameMapFile))
                    {
                        StopEditor();
                    }

                }
            },
            () =>
            {
                StopEditor();
            });



        }


        private void OnDrawGizmos()
        {
            if(editorAxis != null)
            {
                editorAxis.OnDrawGizmos();
            }
        }
    }
}
