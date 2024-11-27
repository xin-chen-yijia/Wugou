using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Wugou.Editor.UI;
using Wugou.UI;

namespace Wugou.Editor
{
    /// <summary>
    /// 可拖放物体描述，包括名称、icon等属性
    /// </summary>
    public class EditorAssetItem
    {
        public string name;
        public string asset;
        public string type;
        public string icon;
    }

    /// <summary>
    /// 可拖放物体集合，为了避免在每一个DraggableItemDesc都有一个assetbundle的属性,好管理
    /// </summary>
    public class EditorAssetItemGroup
    {
        public string name;
        public string icon;
        public List<EditorAssetItem> items;
        public int order = int.MaxValue;
    }

    /// <summary>
    /// 编辑地图、场景，比如向场景中放置目标物体或配置其它属性
    /// </summary>
    public class GameMapEditor : MonoBehaviour
    {
        public static GameMapEditor instance { get; private set; }

        // 编辑器UI
        public UIRootWindow uiRootWindow;

        /// <summary>
        /// 当前编辑的GameMap工程
        /// </summary>
        public GameMapProj currentProject { get; private set; }

        // 场景脚本
        public GameMap loadedGameMap => currentProject?.gameMap;

        /// <summary>
        /// 标识地图已加载
        /// </summary>
        public bool isLoadedGameMap { get; private set; }

        // 坐标轴
        public EditorAxis editorAxis;

        /// <summary>
        /// 飞行视角
        /// </summary>
        public FlyCamera flyCameraComp => editorCamera.GetComponent<FlyCamera>();

        /// <summary>
        /// 操作模式 
        /// </summary>
        public enum OptionMode
        {
            kView = 0,
            kTranslate,
            kRotate,
            kScale,
            kAttach,        // 附着在物体表面
        };
        private OptionMode curOptionMode_ = OptionMode.kView;
        public OptionMode optionModel => curOptionMode_;

        // 当前编辑摄像机
        public Camera editorCamera;

        public int selectableMask { get; private set; }    // 可选物体层标记

        public float maxRayDistance = 1000;

        public GameEntity curHoldingGameEntity { get; private set; } // 要放置的物体

        public bool isHoldingGameEntity => curHoldingGameEntity != null;

        /// <summary>
        /// 用于判断鼠标是否在操作场景的区域
        /// </summary>
        public bool isPointerInSceneView { get; private set; }

        // 标识是否正在输入
        private bool isFocusedOnInputComponent_ = false;

        // 当前选择的物体
        public GameObject selectedObject
        {
            get;
            private set;
        }
        public GameEntity selectedEntity => GetGameEntity(selectedObject);

        /// <summary>
        /// 在贴地模式下，放置物体是否朝向法线
        /// </summary>
        public bool isPlaceGameEntityForwardHitNormal { get; set; } = false;

        private bool _enableControlGameEntity = true;
        /// <summary>
        /// 当前是否可选择物体
        /// </summary>
        public bool enableControlGameEntity {
            get 
            {
                return _enableControlGameEntity;
            }
            set
            {
                _enableControlGameEntity = value;
                editorAxis.enabled = value;
            }
        }

        /// <summary>
        /// 快捷键
        /// </summary>
        public bool enableShortcutKeys { get; set; } = true;

        // 可拖放物体信息
        private List<EditorAssetItemGroup> assetsGroups_ = new List<EditorAssetItemGroup>();

        public const string mapEditorLayerName = "MapEditor";
        public static int mapEditorLayer => LayerMask.NameToLayer(mapEditorLayerName); // 所有选择的物体所在层

        private const string mapToolLayerName = "MapTool";
        public static int mapToolLayer => LayerMask.NameToLayer(mapToolLayerName);   // 工具物体所在层，如坐标轴

        private const string mapGizmosLayerName = "MapGizmos";
        public static int mapGizmosLayer => LayerMask.NameToLayer(mapGizmosLayerName);   // 编辑器辅助物体所在层，比如虚化的物体

        public const string kContentFileName = "editor-assets";

        /// <summary>
        /// Undo/Redo 系统
        /// </summary>
        public Transactor Undo { get; private set; } = new Transactor();

        private int cachedUndoIndex_ = -1;    // 用于判断当前是否变更了

        /// <summary>
        /// 类似c++的LockGuard,即RAII的思想
        /// 注意，在一次完整的事务中要就都使用Undo，要不就都使用TransactionScope
        /// </summary>
        public class TransactionScope : IDisposable
        {
            public static TransactionScope activeTransaction { get; private set; }  // 用于跨函数调用

            public TransactionScope()
            {
                GameMapEditor.instance.Undo.BeginTransaction();
                activeTransaction = this;
            }
            public void Dispose()
            {
                GameMapEditor.instance.Undo.EndTransaction();
                activeTransaction = null;
            }

            public void Record(ObjectRecord record)
            {
                GameMapEditor.instance.Undo.Record(record);
            }
        }

        // 选择框
        private GameObject frameObj_;
        private Mesh frameMesh_;

        /// <summary>
        /// 地图加载完成后调用
        /// </summary>
        [HideInInspector]
        public UnityEvent onLoadedMap = new UnityEvent();

        /// <summary>
        /// 选择物体事件
        /// </summary>
        [HideInInspector]
        public UnityEvent<GameEntity> onSelectGameEntity = new UnityEvent<GameEntity>();

        /// <summary>
        /// 放下物体后调用
        /// </summary>
        [HideInInspector]
        public UnityEvent<GameEntity> onGameEntityAdd = new UnityEvent<GameEntity>();

        /// <summary>
        /// 删除物体事件
        /// </summary>
        [HideInInspector]
        public UnityEvent<GameEntity> onGameEntityDestory = new UnityEvent<GameEntity>();

        /// <summary>
        /// 保存地图时调用
        /// </summary>
        [HideInInspector]
        public UnityEvent onSave = new UnityEvent();

        /// <summary>
        ///  退出编辑器时使用
        /// </summary>
        [HideInInspector]
        public UnityEvent onExit = new UnityEvent();

        /// <summary>
        /// 导入包后调用
        /// </summary>
        [HideInInspector]
        public UnityEvent<int> onImportedPackage = new UnityEvent<int>();

        /// <summary>
        /// 更新操作模式时调用
        /// </summary>
        [HideInInspector]
        public UnityEvent<OptionMode> onOptionModeChanged = new UnityEvent<OptionMode>();

        /// <summary>
        /// 变换物体
        /// </summary>
        private System.Action TransformGameObject;

        /// <summary>
        /// 用于停止 
        /// </summary>
        private Coroutine lookAtCoroutine_ = null;  

        /// <summary>
        /// 已导入的包（非内置包），用于删除
        /// </summary>
        private List<string> importedPackages_= new List<string>();

        /// <summary>
        /// 内置资产组数量
        /// </summary>
        private int builtinGroupCount = int.MaxValue;

        /// <summary>
        /// 有多少分组
        /// </summary>
        public int groupCount => assetsGroups_.Count;

        /// <summary>
        /// 每一个分组中有多少物体
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public int GetItemsCount(int group)
        {
            if (group < groupCount)
            {
                return assetsGroups_[group].items.Count;
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
            return assetsGroups_[group].name;
        }

        /// <summary>
        /// 获取图标
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public string GetGroupIcon(int group)
        {
            if (group >= groupCount)
            {
                return "";
            }
            return assetsGroups_[group].icon;
        }

        /// <summary>
        /// 获取具体的物体描述信息
        /// </summary>
        /// <param name="group"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public EditorAssetItem GetIAssettem(int group, int index)
        {
            if (group < groupCount && index < assetsGroups_[group].items.Count)
            {
                return assetsGroups_[group].items[index];
            }

            Logger.Error($"group:{group} index:{index} out of range..");
            return null;
        }


        #region Unity3D Functions

        public virtual void Awake()
        {
            Debug.Assert(instance == null);
            instance = this;

            // LayerMask.NameToLayer 不能在初始化函数中使用
            selectableMask = (Physics.AllLayers & ~(1 << mapGizmosLayer) & ~(Physics.IgnoreRaycastLayer));
        }

        // Start is called before the first frame update
        public virtual void Start()
        {
            // 坐标轴
            editorAxis.editorCamera = editorCamera;
            editorAxis.layer = 1 << mapEditorLayer;

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
            frameObj_.transform.SetParent(transform);
            frameObj_.SetActive(false);
            GameObject.DestroyImmediate(frameObj_.GetComponent<Collider>());
            frameObj_.GetComponent<MeshFilter>().sharedMesh = frameMesh_;

            Material frameMat = new Material(Shader.Find("Wugou/Frame"));
            frameMat.color = new Color(0, 175.0f / 255, 1.0f, 61.0f / 255);
            frameObj_.GetComponent<MeshRenderer>().material = frameMat;

            //frameObj_.transform.SetParent(editorCamera.transform);
            frameObj_.transform.localPosition = new Vector3(0, 0, 10);  // 避免被剔除
        }

        // Update is called once per frame
        public virtual void Update()
        {
            // 
            if (!isLoadedGameMap)
            {
                return;
            }

            CheckUIState();

            ApplyShortcutKeys();

            // 鼠标在UI上时，禁止一些三维操作 
            if (isPointerInSceneView)
            {
                // 没有相机时不操作
                if (!editorCamera)
                {
                    return;
                }

                if (isHoldingGameEntity)
                {
                    // 添加物体到场景中
                    HandlePutDownEntity();
                }
                else
                {
                    if (enableControlGameEntity)
                    {
                        //
                        TransformGameObject();
                    }

                }
            }
        }

        #endregion

        /// <summary>
        /// 检查UI状态，判断当前是否正在操作UI，因为会影响选择物体、快捷键等功能
        /// </summary>
        private void CheckUIState()
        {
            // 不在UI上，则是在操作三维场景
            isPointerInSceneView = !EventSystem.current.IsPointerOverGameObject();

            if(Input.GetMouseButtonUp(0))
            {
                // 输入时需要禁用快捷键
                if(EventSystem.current.currentSelectedGameObject && (EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>() != null || EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>()))
                {
                    isFocusedOnInputComponent_ = true;
                }
                else
                {
                    isFocusedOnInputComponent_ = false;
                }
            }

            if (!isPointerInSceneView)
            {
                // 锁住相机
                LockEditorCamera();
            }
            else
            {
                UnlockEditorCamera();
            }
        }

        /// <summary>
        /// 快捷键
        /// </summary>
        private void ApplyShortcutKeys()
        {
            // 正在输入，不响应快捷键
            if (!enableShortcutKeys || isFocusedOnInputComponent_)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                if (selectedEntity)
                {
                    DestroyGameEntityAndRecord(selectedEntity);
                }

            }

            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.Z))
            {
                Undo.Undo();
            }

            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.Y))
            {
                Undo.Redo();
            }

            // duplicate
            if (selectedEntity && !selectedEntity.isStatic && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.D))
            {
                DuplicateGameEntity(selectedEntity);
            }

            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S))
            {
                // 保存
                Save();
            }

            if (!Input.GetMouseButton(1))   // 按住右键的时候屏蔽
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    SetOptionMode(OptionMode.kView);
                }

                if (Input.GetKeyDown(KeyCode.W))
                {
                    SetOptionMode(OptionMode.kTranslate);
                }

                if (Input.GetKey(KeyCode.E))
                {
                    SetOptionMode(OptionMode.kRotate);
                }

                if (Input.GetKey(KeyCode.R))
                {
                    SetOptionMode(OptionMode.kScale);
                }
            }

            if (isPointerInSceneView)
            {
                // 快捷键， 鼠标在三维场景上时响应
                if (Input.GetKeyUp(KeyCode.F))
                {
                    if (selectedEntity != null)
                    {
                        if (lookAtCoroutine_ != null)
                        {
                            StopCoroutine(lookAtCoroutine_);
                        }
                        lookAtCoroutine_ = StartCoroutine(LookAtTarget(selectedEntity.gameObject.transform));
                    }
                }
            }
        }

        /// <summary>
        /// 替换相机
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="caasFlyCameramera"></param>
        public void ReplaceCamera(Camera camera, bool asFlyCamera = true)
        {
            // clear old
            if (flyCameraComp)
            {
                Destroy(flyCameraComp.transform.root.gameObject);
            }
            else if(editorCamera)
            {
                Destroy(editorCamera);
            }

            // replace
            editorCamera = camera;
            editorAxis.editorCamera = editorCamera;

            if (asFlyCamera && !editorCamera.GetComponent<FlyCamera>())
            {
                var flyCam = editorCamera.gameObject.AddComponent<FlyCamera>();
                flyCam.Init();
            }
        }

        public async Task<bool> LoadGameMap(GameMap map)
        {
            // 加载的场景中也会有一个相机
            editorCamera.GetComponent<AudioListener>().enabled = false;

            // 加载脚本
            var succ = await GameWorld.LoadGameMap(map, LoadSceneMode.Additive);
            if (succ == GameWorld.Error.kSuccess)
            {
                DaemonUI.loadingPage.SetProgress(1.0f);

                // 等一些插件或者脚本执行初始化
                await new YieldInstructionAwaiter(null);

                // 刚体取消
                foreach(var v in GameObject.FindObjectsOfType<Rigidbody>())
                {
                    v.isKinematic = true;
                }

                // 
                foreach(var v in GameWorld.gameEntities)
                {
                    Utils.SetLayerRecursively(v.gameObject, mapEditorLayer);
                }

                // 替换为场景中的相机
                ReplaceCamera(Camera.main);

                // 描边
                EnableOutline(editorCamera);

                // 天气系统特效
                if (loadedGameMap.needWeather)
                {
                    GameWorld.weatherSystem.ApplyWeatherEffectsToPlayer(editorCamera.transform.parent.gameObject);
                }

                // 默认切换为Attach模式
                SetOptionMode(OptionMode.kAttach);

                //
                isLoadedGameMap = true;

                // 已完成GameMap加载
                onLoadedMap.Invoke();

                return true;
            }

            return false;
        }

        /// <summary>
        /// 卸载脚本 
        /// </summary>
        private void UnloadGameMap()
        {
            // 
            GameEntityManager.DestroyAllGameEntity();

            // reset
            GameWorld.UnloadGameMap();
        }

        /// <summary>
        /// 加载脚本信息，MapEditorSystem编辑的是脚本信息
        /// </summary>
        /// <param name="mapProj"></param>
        public static async void StartEditor(GameMapProj mapProj)
        {
            // 这是还未加载编辑器场景时显示的加载界面，加载编辑器后还有一个 loading page
            DaemonUI.loadingPage.Show();
            DaemonUI.loadingPage.SetProgress(0);

            // 先加载编辑器
            var op = SceneManager.LoadSceneAsync(GameConsole.settings.editorScene, LoadSceneMode.Single);
            await op;

            while (!op.isDone || instance == null)
            {
                await new Wugou.YieldInstructionAwaiter(null);
            }

            instance.currentProject = mapProj;

            instance.builtinGroupCount = int.MaxValue;  //
            // read assets
            // Resources/editor
            foreach (var path in Directory.GetDirectories($"{GameConsole.resourcePath}/editor"))
            {
                instance.ImportAssetPackage(path);
            }
            instance.builtinGroupCount = instance.groupCount - 1;   // 内置资产

            // order
            instance.assetsGroups_.Sort((a, b) => { 
                if (a.order == b.order) 
                    return 0; 
                return a.order > b.order ? 1 : -1; 
            });

            // mount imported packages
            foreach (var v in mapProj.packages)
            {
                var path = mapProj.GetPackagePath(v);
                //if (GameAssetDatabase.IsAssetbundle(path))
                //{
                //    await GameAssetDatabase.MountAssetBundle($"/{v}", path);
                //}

                instance.ImportAssetPackage(path);
            }

            // loading page
            DaemonUI.loadingPage.SetProgress(0.3f);
            DaemonUI.loadingPage.UpdateProgressBar(() => {
                if (GameWorld.loadingSceneOperation != null)
                {
                    if (GameWorld.loadingSceneOperation.isDone)
                    {
                        return 1.0f;
                    }

                    return 0.3f + GameWorld.loadingSceneOperation.progress * 0.7f;
                }

                return DaemonUI.loadingPage.GetProgress();
            });     // 更新进度

            var succ = await instance.LoadGameMap(mapProj.gameMap);
            if (succ)
            {
                // 恢复相机位置
                if (mapProj.cameraCache != null)
                {
                    instance.flyCameraComp.LocateCameraTo(mapProj.cameraCache.position, mapProj.cameraCache.rotation);
                }
            }
            else
            {
                Logger.Error($"Load map {mapProj.gameMap.name} error...");
                StopEditor();
            }

            DaemonUI.loadingPage.Hide();
        }


        public static void StopEditor()
        {
            if (instance)
            {
                instance.StopInternal();
            }
        }

        private void StopInternal()
        {
            //
            onExit.Invoke();

            UnloadGameMap();

            GameConsole.loadedGameMapFile = string.Empty;
            //
            // 加载主场景
            SceneManager.LoadScene(GameConsole.settings.editorHomeScene);

            // 实例销毁了
            instance = null;
        }

        /// <summary>
        /// 导入资产包，包括普通文件夹和AB包两种类型
        /// </summary>
        /// <param name="path"></param>
        /// <param name="importToProj">是否导入到当前工程</param>
        /// <returns></returns>
        public EditorAssetItemGroup ImportAssetPackage(string path, bool importToProj = false)
        {
            var contentFile = $"{path}/{kContentFileName}";
            if(!File.Exists(contentFile))
            {
                return null;
            }

            // load prefab description
            var content = System.IO.File.ReadAllText(contentFile);
            var assetsGroup = JsonConvert.DeserializeObject<EditorAssetItemGroup>(content);
            AddAssetsGroup(assetsGroup);

            var packageName = GameMapProj.GetPackageName(path);
            var packagePath = path;

            if (importToProj)
            {
                currentProject.ImportPackage(path);

                packagePath = currentProject.GetPackagePath(packageName);
            }

            //
            importedPackages_.Add(packageName);

            // 挂载文件夹
            _ = GameAssetDatabase.Mount($"/{Path.GetFileName(packagePath)}", packagePath, GameAssetDatabase.MountContentType.kFileSystem);
            // ab包挂载
            foreach (var dir in Directory.GetDirectories(packagePath))
            {
                if (GameAssetDatabase.IsAssetbundle(dir))
                {
                    var mountPoint = $"/{Path.GetFileName(dir)}";
                    _ = GameAssetDatabase.Mount(mountPoint, dir);
                }
            }

            // 内置包隐藏路径
            onImportedPackage.Invoke(groupCount - 1);

            return assetsGroup;
        }

        public bool IsBuiltinGroup(int group)
        {
            return group < builtinGroupCount;
        }

        /// <summary>
        /// 删除已导入的包
        /// </summary>
        /// <param name="group"></param>
        public void DeleteAssetsGroup(int group)
        {
            if (IsBuiltinGroup(group))
            {
                Logger.Error("Can't delete builtin package..");
                return;
            }

            Debug.Assert(group < assetsGroups_.Count);
            RemoveAssetsGroupAt(group);

            //var contentFile = $"{path}/{kContentFileName}";
            //string groupName = "";
            //if (Directory.Exists(path) && File.Exists(contentFile))
            //{
            //    var content = System.IO.File.ReadAllText(contentFile);
            //    var assetsGroup = JsonConvert.DeserializeObject<EditorAssetItemGroup>(content);
            //    for(int i=assetsGroups_.Count-1; i>=0; i--)
            //    {
            //        if (assetsGroups_[i].name == assetsGroup.name)
            //        {
            //            assetsGroups_.RemoveAt(i);
            //            groupName = assetsGroup.name;
            //            break;
            //        }
            //    }
            //}

            var packageName = importedPackages_[group];
            importedPackages_.RemoveAt(group);
            //
            currentProject.DeleteAssetPackage(packageName);

        }

        /// <summary>
        /// 注册一组新类型
        /// </summary>
        /// <param name="assetsGroup"></param>
        public void AddAssetsGroup(EditorAssetItemGroup assetsGroup)
        {
            assetsGroups_.Add(assetsGroup);
        }

        public void RemoveAssetsGroup(EditorAssetItemGroup assetsGroup)
        {
            assetsGroups_.Remove(assetsGroup);
        }

        public void RemoveAssetsGroupAt(int index)
        {
            assetsGroups_.RemoveAt(index);
        }


        public void PickUp(EditorAssetItem assetItem)
        {
            if (curHoldingGameEntity)
            {
                DestroyGameEntity(curHoldingGameEntity);    // 删除它
                return;
            }

            var entity = CreateGameEntity(assetItem);
            PickUp(entity);
        }

        private void PickUp(GameEntity entity)
        {
            // undo record
            Undo.BeginTransaction();
            Undo.Record(new EditorCreateGameEntity(entity.id));

            // pickup
            curHoldingGameEntity = entity;

            HandleCollidersWhenPickUp(curHoldingGameEntity);
        }

        private GameEntity GetGameEntity(GameObject obj)
        {
            if(obj == null)
            {
                return null;
            }
            return obj.GetComponentInParent<GameEntity>();
        }

        private List<Collider> selectedEntityColliders_ = new List<Collider>(); // 用于还原
        
        // 当前正被拾取的Entity，用于处理它的Collider
        private GameEntity curDisabledColliderEntity_;  

        /// <summary>
        /// 在拾取物体时，把Entity的碰撞去掉，否则无法选择地面
        /// </summary>
        /// <param name="entity"></param>
        private async void HandleCollidersWhenPickUp(GameEntity entity)
        {
            curDisabledColliderEntity_ = entity;
            // 等待body
            await new EnumeratorAwaiter(WaitGameEnityBody(entity));

            // 不在放下的时候清除是因为未放下前物体可能被清除了。。。
            selectedEntityColliders_.Clear();

            if (curDisabledColliderEntity_) // curDisabledColliderEntity_==null表示已经放下
            {
                // 选中后collider禁用，避免干扰射线
                foreach (var v in curDisabledColliderEntity_.GetComponentsInChildren<Collider>())
                {
                    if (v.enabled)
                    {
                        v.enabled = false;
                        selectedEntityColliders_.Add(v);
                    }
                }
            }

        }

        IEnumerator WaitGameEnityBody(GameEntity entity)
        {
            while (!string.IsNullOrEmpty(entity.asset) && !entity.body)
            {
                yield return null;
            }
        }

        private void HandleCollidersWhenPutdown()
        {
            //
            foreach (var v in selectedEntityColliders_)
            {
                v.enabled = true;
            }

            curDisabledColliderEntity_ = null;  // 因为HandleCollidersWhenPickUp为异步操作，可能在等待body的时候就已经放下物体，所以标记一下
        }


        private void SelectObjectInternal(GameObject obj)
        {
            //
            var newEntity = GetGameEntity(obj);
            if (newEntity != selectedEntity)
            {
                // hide outline
                if (selectedEntity)
                {
                    SetOutlineEnabled(selectedEntity.gameObject, false);
                }

                if (!newEntity || !newEntity.isStatic)
                {
                    editorAxis.SetSelectedObject(newEntity?.gameObject);
                }

                if (newEntity)
                {
                    Debug.Assert(GameWorld.ExistsEntity(newEntity));

                    // 显示选中状态
                    SetOutlineEnabled(newEntity.gameObject, true);
                }

                // 显示属性
                uiRootWindow.GetChildWindow<InspectorPage>().SetTarget(newEntity?.gameObject);
                uiRootWindow.GetChildWindow<InspectorPage>().Show();

                onSelectGameEntity?.Invoke(newEntity);
            }

            selectedObject = obj;
        }

        /// <summary>
        /// 实例化对象
        /// </summary>
        /// <param name="assetItem"></param>
        /// <returns></returns>
        private GameEntity CreateGameEntity(EditorAssetItem assetItem)
        {
            // 记录，用于添加到脚本中
            string prototype = assetItem.type;
            var entity = GameEntityManager.CreateGameEntity(assetItem.type, assetItem.asset);
            GameWorld.AddGameEntity(entity);

            Utils.DoAsync(async () =>
            {
                await new EnumeratorAwaiter(new WaitUntil(() => { return entity.body != null; }));

                Utils.SetLayerRecursively(entity.gameObject, mapEditorLayer);
                // 显示选中状态
                SetOutlineEnabled(entity.gameObject, true);
            });

            return entity;
        }

        /// <summary>
        /// 选择一个物体
        /// </summary>
        /// <param name="gameObject"></param>
        public void SelectGameEntity(GameEntity entity)
        {
            SelectObjectInternal(entity?.gameObject);
        }

        /// <summary>
        /// 删除物体
        /// </summary>
        /// <param name="entity"></param>
        public void DestroyGameEntity(GameEntity entity)
        {
            if (selectedEntity == entity)
            {
                Wugou.Logger.Error("Can't destroy current selected entity.. ");
                return;
            }
            onGameEntityDestory.Invoke(entity);

            GameWorld.RemoveGameEntity(entity);
            GameEntityManager.DestroyGameEntity(entity);
        }

        /// <summary>
        /// 删除并记录该操作，用于回退
        /// </summary>
        /// <param name="entity"></param>
        public void DestroyGameEntityAndRecord(GameEntity entity)
        {
            using (var transaction = new TransactionScope())
            {
                // undo
                transaction.Record(new EditorSelectGameEntity());
                var delRecord = new EditorDeleteGameEntity(entity.id); // 注意要先保存

                // change selectedEntity, then delete
                SelectObjectInternal(null);
                DestroyGameEntity(entity);

                transaction.Record(delRecord);
            }
        }

        /// <summary>
        /// 复制物体
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public GameEntity DuplicateGameEntity(GameEntity entity)
        {
            var newEntity = GameEntityManager.DuplicateGameEntity(entity);
            GameWorld.AddGameEntity(newEntity);

            onGameEntityAdd?.Invoke(newEntity);

            SelectGameEntity(newEntity);

            return newEntity;
        }

        public void SetOptionMode(OptionMode mode)
        {
            if(curOptionMode_ == mode)
            {
                return;
            }

            curOptionMode_ = mode;

            switch (mode)
            {
                case OptionMode.kView:
                    TransformGameObject = () => { };
                    editorAxis.enabled = false;
                    editorAxis.SetOptionModeWithoutNotify(EditorAxis.Mode.kNone);
                    break;
                case OptionMode.kTranslate:
                case OptionMode.kRotate:
                case OptionMode.kScale:
                    TransformGameObject = HandleTranlateRotateScaleMode;
                    editorAxis.enabled = true;
                    editorAxis.SetOptionMode((EditorAxis.Mode)(mode));
                    break;
                case OptionMode.kAttach:
                    TransformGameObject = HandleAttachMode;
                    editorAxis.enabled = false;
                    editorAxis.SetOptionModeWithoutNotify(EditorAxis.Mode.kNone);
                    break;
                default:
                    break;
            }

            // 鼠标控制视角移动只能在view模式
            if(flyCameraComp)
            {
                flyCameraComp.freezeMouseMove = (mode != OptionMode.kView);
            }

            //
            onOptionModeChanged.Invoke(mode);
        }

        IEnumerator LookAtTarget(Transform target)
        {
            if (flyCameraComp)
            {
                while (Vector3.SqrMagnitude((flyCameraComp.viewCenter - target.position)) > 0.000001f)
                {
                    yield return null;

                    flyCameraComp.viewCenter = Vector3.Lerp(flyCameraComp.viewCenter, target.position, 0.8f);
                }
            }
        }

        private bool isMovingGameEntity_ = false;

        /// <summary>
        /// 选择物体放到场景中
        /// </summary>
        void HandlePutDownEntity()
        {
            Debug.Assert(isHoldingGameEntity);

            RaycastHit hit;
            Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, maxRayDistance, selectableMask))
            {
                // for test
                if (Input.GetKeyDown(KeyCode.P))
                {
                    DaemonUI.fadeOutTipsPage.Show(hit.collider.name);
                }
                curHoldingGameEntity.transform.position = hit.point;
                if (isPlaceGameEntityForwardHitNormal)
                {
                    curHoldingGameEntity.transform.up = hit.normal;
                }
            }

            // 放下物体
            if (Input.GetMouseButtonUp(0))
            {
                int entityId = curHoldingGameEntity.id;

                // 添加物体
                onGameEntityAdd?.Invoke(curHoldingGameEntity);

                Undo.Record(new EditorSelectGameEntity());

                // 同时选择物体
                SelectObjectInternal(curHoldingGameEntity.gameObject);

                curHoldingGameEntity = null;

                Undo.EndTransaction();  // 这个地方有两种情况：创建物体和移动物体

                HandleCollidersWhenPutdown();
            }
        }

        /// <summary>
        /// 处理启动贴合物体表面模式
        /// </summary>
        void HandleAttachMode()
        {
            Debug.Assert(optionModel == OptionMode.kAttach);

            if (Input.GetMouseButtonDown(0))
            {
                RaycastGameEntity();

                if (selectedEntity)
                {
                    HandleCollidersWhenPickUp(selectedEntity);
                }
            }

            // 物体随鼠标走
            RaycastHit hit;
            Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);
            if (Input.GetMouseButton(0) && selectedEntity != null && !selectedEntity.isStatic && Physics.Raycast(ray, out hit, maxRayDistance, selectableMask))
            {
                selectedEntity.position = hit.point;
                if (isPlaceGameEntityForwardHitNormal)
                {
                    selectedEntity.transform.up = hit.normal;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (isMovingGameEntity_)
                {
                    isMovingGameEntity_ = false;
                    Undo.EndTransaction();  // 移动完成

                    HandleCollidersWhenPutdown();
                }
            }
        }

        /// <summary>
        /// 物体移动、旋转、缩放等
        /// </summary>
        void HandleTranlateRotateScaleMode()
        {
            Debug.Assert(optionModel == OptionMode.kTranslate ||  optionModel == OptionMode.kRotate || optionModel == OptionMode.kScale);

            if (!isMovingGameEntity_ && editorAxis.isDragging) // 开始拖动
            {
                isMovingGameEntity_ = true;

                Undo.BeginTransaction();
                Undo.Record(new EditorMoveGameEntity(selectedEntity.id));
            }

            if (isMovingGameEntity_ && !editorAxis.isDragging) // 结束拖动
            {
                isMovingGameEntity_ = false;
                Undo.EndTransaction();
            }

            // 选择物体
            if (editorAxis.activeAxisName == EditorAxis.kEmptyAxisName && Input.GetMouseButtonUp(0))
            {
                RaycastGameEntity();
            }

            ShowFrameSelect();
        }

        /// <summary>
        /// 射线选择GameEntity
        /// </summary>
        private void RaycastGameEntity()
        {
            RaycastHit hit;
            Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, maxRayDistance, 1 << mapEditorLayer))
            {
                Undo.BeginTransaction();
                if (selectedEntity)
                {
                    Undo.Record(new EditorSelectGameEntity());
                }

                SelectObjectInternal(hit.collider.gameObject);

                if (selectedEntity)
                {
                    isMovingGameEntity_ = true;
                    Undo.Record(new EditorMoveGameEntity(selectedEntity.id));
                }
            }
            else
            {
                using (var transaction = new TransactionScope())
                {
                    transaction.Record(new EditorSelectGameEntity());

                    SelectObjectInternal(null);
                }
            }
        }

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
        /// 框选
        /// </summary>
        void ShowFrameSelect()
        {
            //if (editorAxis.isDraggingAxis)     // 先判断是否在操作坐标轴
            //{
            //    isShowFrameSelect = false;
            //    return;
            //}

            if (!isShowFrameSelect)
            {
                //if (Input.GetMouseButtonDown(0))
                //{
                //    firstFrameSelectPoint = new Vector3(Input.mousePosition.x, Screen.height - Input.mousePosition.y, 0);
                //    isShowFrameSelect = true;
                //}
            }
            else
            {
                //if (Input.GetMouseButtonUp(0))
                //{
                //    isShowFrameSelect = false;

                //    // 用移动距离判断是否是点击
                //    if(Vector3.Distance(new Vector3(Input.mousePosition.x, Screen.height - Input.mousePosition.y, 0), firstFrameSelectPoint) < 3.0f)
                //    {
                //        // 选择物体
                //        if (Physics.Raycast(ray, out hit, maxRayDistance, 1 << mapEditorLayer) && hit.collider != null)
                //        {
                //            SelectObjectInternal(hit.collider.gameObject);
                //        }
                //        else
                //        {
                //            SelectObjectInternal(null);
                //        }
                //    }

                //    return;
                //}

                //var leftTop = firstFrameSelectPoint;
                //var rightBottom = new Vector3(Input.mousePosition.x, Screen.height - Input.mousePosition.y, 0);

                //// 检测相对位置
                //if (leftTop.x > rightBottom.x)
                //{
                //    (leftTop.x, rightBottom.x) = (rightBottom.x, leftTop.x);
                //}
                //if (leftTop.y > rightBottom.y)
                //{
                //    (leftTop.y, rightBottom.y) = (rightBottom.y, leftTop.y);
                //}

                //var leftTop01 = new Vector2(leftTop.x / Screen.width, leftTop.y / Screen.height);
                //var rightBottom01 = new Vector2(rightBottom.x / Screen.width, rightBottom.y / Screen.height);

                //// 转换为opengl坐标
                //System.Func<float, float> toGL = (x) => { return 2.0f * x - 1.0f; };
                //leftTop01.x = toGL(leftTop01.x);
                //leftTop01.y = toGL(leftTop01.y);
                //rightBottom01.x = toGL(rightBottom01.x);
                //rightBottom01.y = toGL(rightBottom01.y);

                //// 更新mesh
                //var mesh = frameObj_.GetComponent<MeshFilter>().sharedMesh;
                //mesh.vertices = new Vector3[4]
                //{
                //new Vector3(leftTop01.x, leftTop01.y, 0.0f),
                //new Vector3(leftTop01.x, rightBottom01.y, 0.0f),
                //new Vector3(rightBottom01.x, rightBottom01.y, 0.0f),
                //new Vector3(rightBottom01.x, leftTop01.y, 0.0f),
                //};


                //// 选择第一个
                //var size = rightBottom - leftTop;
                //Rect area = new Rect(leftTop.x,leftTop.y, size.x, size.y);
                //GameObject firstSelected = null;
                //foreach(var v in GameWorld.gameEntities)
                //{
                //    var p = editorCamera.WorldToScreenPoint(v.transform.position);
                //    p.y = Screen.height - p.y;
                //    if (area.Contains(p))
                //    {
                //        firstSelected = v.gameObject;
                //        break;
                //    }
                //}

                //if (firstSelected)
                //{
                //    SelectObjectInternal(firstSelected);
                //}
                //else
                //{
                //    SelectObjectInternal(null);
                //}
            }
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <returns></returns>
        public bool Save()
        {
            if(GameWorld.GetStartPositionCount() == 0)
            {
                Logger.Error("Save map with start position count 0.");
                DaemonUI.makeSurePage.Show("角色初始位置数量不能为0！");
                return false;
            }

            cachedUndoIndex_ = Undo.GetIndex();

            // 天气写入
            if (GameWorld.weatherSystem.isLoaded)
            {
                loadedGameMap.weather = new WeatherSetting()
                {
                    type = GameWorld.weatherSystem.weatherType,
                    time = GameWorld.weatherSystem.time,
                    fogDensity = GameWorld.weatherSystem.fogDensity,
                    windDir = GameWorld.weatherSystem.windDirection,
                    windForce = GameWorld.weatherSystem.windForce,
                };
            }


            // 保存工程
            currentProject.Save();

            onSave.Invoke();

            return true;
        }

        public void BuildGameMap()
        {
            // 保存
            Save();

            // 天气写入
            //if (Gameplay.weatherSystem.isLoaded)
            //{
            //    loadedGameMap.weather = new WeatherDesc()
            //    {
            //        type = Gameplay.weatherSystem.weatherType,
            //        time = Gameplay.weatherSystem.time,
            //        fogDensity = Gameplay.weatherSystem.fogDensity,
            //        windDir = Gameplay.weatherSystem.windDirection,
            //        windForce = Gameplay.weatherSystem.windForce,
            //    };
            //}

            currentProject.Build();

            DaemonUI.makeSurePage.Show($"'{loadedGameMap.name}'构建成功");
        }

        /// <summary>
        /// 退出当前编辑
        /// </summary>
        public void Quit()
        {
            if (Undo.GetIndex() == cachedUndoIndex_)
            {
                StopEditor();
                return;
            }

            DaemonUI.makeSurePage.Show("是否保存当前脚本？", () =>
            {
                // save
                if (Save())
                {
                    StopEditor();
                }
            },
            () =>
            {
                StopEditor();
            },"是", "否");
        }

        private void EnableOutline(Camera camera)
        {
            OutlineEffect.Apply(camera);
        }

        private void SetOutlineEnabled(GameObject go, bool enable)
        {
            if (enable)
            {
                OutlineEffect.AddOutline(go);
            }
            else
            {
                OutlineEffect.RemoveOutline(go);
            }
        }

        //private int cameraLockBit_ = 0;
        ///// <summary>
        ///// 有多种情况需要锁住相机，用位来记录多种情况
        ///// </summary>
        ///// <param name="bit"></param>
        //private void LockEditorCamera(int bit)
        //{
        //    cameraLockBit_ |= 1 << bit;
        //    if (flyCamera_)
        //    {
        //        flyCamera_.enabled = (cameraLockBit_ == 0);
        //    }

        //}

        //private void UnlockEditorCamera(int bit)
        //{
        //    cameraLockBit_ &= ~(1 << bit);
        //    if (flyCamera_)
        //    {
        //        flyCamera_.enabled = (cameraLockBit_ == 0);
        //    }
        //}

        /// <summary>
        /// 锁住编辑器相机，不让操作
        /// </summary>
        public void LockEditorCamera()
        {
            if (flyCameraComp)
            {
                flyCameraComp.enabled = false;
            }
        }

        /// <summary>
        /// 解锁编辑器相机，不让操作
        /// </summary>
        public void UnlockEditorCamera()
        {
            if (flyCameraComp)
            {
                flyCameraComp.enabled = true;
            }
        }

        /// <summary>
        /// 设置是否是正交矩阵
        /// </summary>
        /// <param name="enable"></param>
        public void SetOrtho(bool enable)
        {
            editorCamera.orthographic = enable;
            editorCamera.orthographicSize = 50;
        }


        private bool isPreviewMode_ = false;
        public void SwitchPreviewMode(bool preview)
        {
            isPreviewMode_ = preview;
            //previewCamera.gameObject.SetActive(isPreviewMode_);
            //editorCamera.gameObject.SetActive(!isPreviewMode_);


        }
    }
}
