using Newtonsoft.Json;
using Wugou.UI;
using Wugou.Editor.UI;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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
        public GameMapProj currentProj { get; private set; }

        // 场景脚本
        public GameMap loadedGameMap => currentProj?.gameMap;

        // 坐标轴
        public EditorAxis editorAxis;

        /// <summary>
        /// 是否使用飞行视角
        /// </summary>
        public bool useFlyCamera = true;

        private FlyCamera flyCamera_ => editorCamera?.GetComponent<FlyCamera>(); 

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
        public Camera editorCamera;

        // 预览摄像机，主要面向一些后处理、雾效等效果，这些效果在编辑状态下不需要
        [HideInInspector]
        public Camera previewCamera;

        private int selectableMask;    // 可选物体层标记

        public float maxRayDistance = 1000;

        private GameEntity currentPickedGameEntity_; // 要放置的物体

        /// <summary>
        /// 用于判断鼠标是否在操作场景的区域
        /// </summary>
        public bool isPointerInSceneView { get; private set; } = true;

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
        public bool placeGameEntityForwardHitNormal { get; set; } = false;


        // 可拖放物体信息
        private List<EditorAssetItemGroup> draggableAssets_ = new List<EditorAssetItemGroup>();

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
        public static UnityEvent onLoadedMap = new UnityEvent();

        /// <summary>
        /// 选择物体事件
        /// </summary>
        public static UnityEvent<GameEntity> onSelectGameEntity = new UnityEvent<GameEntity>();

        /// <summary>
        /// 放下物体后调用
        /// </summary>
        public static UnityEvent<GameEntity> onGameEntityAdd = new UnityEvent<GameEntity>();

        /// <summary>
        /// 删除物体事件
        /// </summary>
        public static UnityEvent<GameEntity> onGameEntityDestory = new UnityEvent<GameEntity>();

        /// <summary>
        /// 保存地图时调用
        /// </summary>
        public static UnityEvent onSave = new UnityEvent();

        /// <summary>
        ///  退出编辑器时使用
        /// </summary>
        public static UnityEvent onExit = new UnityEvent();

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
            return draggableAssets_[group].icon;
        }

        /// <summary>
        /// 获取具体的物体描述信息
        /// </summary>
        /// <param name="group"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public EditorAssetItem GetItem(int group, int index)
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

            selectableMask = (0xFFFF & ~(1 << mapGizmosLayer));
        }

        // Start is called before the first frame update
        public virtual void Start()
        {
            // 飞行视角
            if (useFlyCamera && editorCamera.GetComponent<FlyCamera>() == null)
            {
                // 避免FlyCamera自己创建父物体，不然还要主动清理
                //GameObject parentObj = new GameObject("FlyCamera");
                //parentObj.tag = "Player";
                //parentObj.transform.position = editorCamera.transform.position + editorCamera.transform.forward * 10.0f;
                //parentObj.transform.rotation = editorCamera.transform.rotation;
                //editorCamera.transform.SetParent(parentObj.transform);

                var flyCam = editorCamera.gameObject.AddComponent<FlyCamera>();
                flyCam.moveSpeed = 15;
                flyCam.xRotSpeed = 90;
                flyCam.yRotSpeed = 90;
                flyCam.freezeMouseMove = true;  // 初始是attach模式，不能动
            }

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

            frameObj_.transform.SetParent(editorCamera.transform);
            frameObj_.transform.localPosition = new Vector3(0, 0, 10);  // 避免被剔除

            // 描边
            EnableOutline();    
        }

        // Update is called once per frame
        public virtual void Update()
        {
            CheckUIState();

            ApplyShortcutKeys();

            // 鼠标在UI上时，禁止一些三维操作 
            if (!isPointerInSceneView)
            {
                // 锁住相机
                LockEditorCamera();
            }
            else
            {
                UnlockEditorCamera();

                // 物体选择等操作
                ObjectOptionalInternal();
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
        }

        /// <summary>
        /// 快捷键
        /// </summary>
        private void ApplyShortcutKeys()
        {
            // 正在输入，不响应快捷键
            if (isFocusedOnInputComponent_)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                if (selectedEntity)
                {
                    using (var transaction = new TransactionScope())
                    {
                        // undo
                        var entity = selectedEntity;

                        transaction.Record(new EditorSelectGameEntity());
                        var delRecord = new EditorDeleteGameEntity(entity.id); // 注意要先保存

                        // change selectedEntity, then delete
                        SelectObjectInternal(null);
                        DestroyGameEntity(entity);

                        transaction.Record(delRecord);
                    }
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
                if (string.IsNullOrEmpty(Gameplay.loadedGameMapFile))
                {
                    uiRootWindow.GetChildWindow<GameMapSavePage>().Show();
                }
                else
                {
                    Save();
                }
            }

            if (!Input.GetMouseButton(1))   // 按住右键的时候屏蔽
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    SwitchOptionMode(OptionModel.kView);
                }

                if (Input.GetKeyDown(KeyCode.W))
                {
                    SwitchOptionMode(OptionModel.kTranslate);
                }

                if (Input.GetKey(KeyCode.E))
                {
                    SwitchOptionMode(OptionModel.kRotate);
                }

                if (Input.GetKey(KeyCode.R))
                {
                    SwitchOptionMode(OptionModel.kScale);
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

        public async Task<bool> LoadGameMap(GameMap map)
        {
            // 加载脚本
            var succ = await GameWorld.LoadGameMap(map, LoadSceneMode.Additive);
            if (succ == GameWorld.Error.kSuccess)
            {
                // 设置GameWorld加载的场景为活动场景
                SceneManager.SetActiveScene(GameWorld.activeScene);

                DaemonUI.loadingPage.SetProgress(1.0f);

                // 等一些插件或者脚本执行初始化
                await new YieldInstructionAwaiter(null).Task;

                // 刚体取消
                foreach(var v in GameObject.FindObjectsOfType<Rigidbody>())
                {
                    v.isKinematic = true;
                }

                // 
                foreach(var v in GameWorld.gameEntities)
                {
                    Utils.SetLayerRecursively(v.gameObject, mapEditorLayer);
                    //v.layer = mapEditorLayer;

                }

                // 预览摄像机用场景中带的
                var tmpCam = OnCreatePreviewCamera();
                previewCamera = tmpCam ? tmpCam : Camera.main; // use main camera

                // 切换为编辑视角
                if (editorCamera.GetComponent<FlyCamera>())
                {
                    editorCamera.GetComponent<FlyCamera>().Init();
                    editorCamera.GetComponent<FlyCamera>().LocateCameraTo(previewCamera.transform);

                    // 把预览相机也放到FlyCamera下
                    previewCamera.transform.SetParent(editorCamera.transform.parent);
                    previewCamera.transform.localPosition = editorCamera.transform.localPosition;
                    previewCamera.transform.localEulerAngles = editorCamera.transform.localEulerAngles;
                    Utils.CopyComponent(previewCamera.gameObject, editorCamera.GetComponent<FlyCamera>());  // 同等操作
                }
                else
                {
                    editorCamera.transform.position = previewCamera.transform.position;
                    editorCamera.transform.rotation = previewCamera.transform.rotation;
                }
                SwitchPreviewMode(false);

                //if (groupCount > 0)
                //{
                //    var groupDesc = draggableAssets_[0];
                //    if (groupDesc.items.Count > 0)
                //    {
                //        // 先加载，避免第一次点击时等太长时间
                //        await GameAssetDatabase.GetAssetAsync<GameObject>(groupDesc.items[0].asset);
                //    }
                //}

                return true;
            }
            else
            {
                // back
                StopInternal();
            }

            return false;
        }

        /// <summary>
        /// 卸载脚本 
        /// </summary>
        protected virtual void UnloadGameMap()
        {
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
            var op = SceneManager.LoadSceneAsync(Gameplay.settings.editorScene, LoadSceneMode.Single);
            await op;

            while (!op.isDone || instance == null)
            {
                await new Wugou.YieldInstructionAwaiter(null);
            }

            instance.currentProj = mapProj;

            // read assets
            // Resources/editor
            foreach(var path in Directory.GetDirectories($"{Gameplay.resourcePath}/editor"))
            {
                instance.ImportAssetPackage(path);
            }
            // order
            instance.draggableAssets_.Sort((a, b) => { 
                if (a.order == b.order) 
                    return 0; 
                return a.order > b.order ? 1 : -1; 
            });

            foreach (var v in mapProj.packages)
            {
                var path = $"{mapProj.path}/{v}";
                if (GameAssetDatabase.IsAssetbundle(path))
                {
                    await GameAssetDatabase.MountAssetBundle($"/{v}", path);
                }

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
                onLoadedMap.Invoke();
            }
            else
            {
                StopEditor();
                Logger.Error($"Load map {mapProj.gameMap.name} error...");
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

            Gameplay.loadedGameMapFile = string.Empty;
            //
            currentPickedGameEntity_ = null;
            lookAtCoroutine_ = null;

            // 加载主场景
            SceneManager.LoadScene(Gameplay.settings.editorHomeScene);

            // 实例销毁了
            instance = null;
        }

        public static void Reset()
        {
            // clear events
            onLoadedMap.RemoveAllListeners();
            onGameEntityAdd.RemoveAllListeners();
            onGameEntityDestory.RemoveAllListeners();
            onSelectGameEntity.RemoveAllListeners();
            onSave.RemoveAllListeners();
        }

        /// <summary>
        /// 导入资产包，包括普通文件夹和AB包两种类型
        /// </summary>
        /// <param name="path"></param>
        /// <param name="copyToProj">是否拷贝资产到当前GameMap目录</param>
        /// <returns></returns>
        public EditorAssetItemGroup ImportAssetPackage(string path, bool copyToProj = false)
        {
            var contentFile = $"{path}/{kContentFileName}";
            if(!File.Exists(contentFile))
            {
                return null;
            }

            // load prefab description
            var content = System.IO.File.ReadAllText(contentFile);
            var assets = JsonConvert.DeserializeObject<EditorAssetItemGroup>(content);
            draggableAssets_.Add(assets);

            if (copyToProj)
            {
                currentProj.Import(Path.GetFileName(path));

                var dst = $"{currentProj.path}/{System.IO.Path.GetFileName(path)}";
                if (!Directory.Exists(dst))
                {
                    // 拷贝导入的资产到目标工程
                    Utils.CopyDirectory(path, dst);
                }
            }

            return assets;
        }

        /// <summary>
        /// 删除已导入的包
        /// </summary>
        /// <param name="packageName"></param>
        public void DeleteAssetPackage(string packageName)
        {
            var path = $"{currentProj.path}/{packageName}";
            var contentFile = $"{path}/{kContentFileName}";
            if (Directory.Exists(path) && File.Exists(contentFile))
            {
                var content = System.IO.File.ReadAllText(contentFile);
                var assets = JsonConvert.DeserializeObject<EditorAssetItemGroup>(content);
                for(int i=draggableAssets_.Count-1; i>=0; i--)
                {
                    if (draggableAssets_[i].name == assets.name)
                    {
                        draggableAssets_.RemoveAt(i);
                        break;
                    }
                }

                Directory.Delete(path, true);
            }

        }

        /// <summary>
        /// 注册一组新类型
        /// </summary>
        /// <param name="assets"></param>
        public void RegisterGroup(EditorAssetItemGroup assets)
        {
            draggableAssets_.Add(assets);
        }
        
        public void PickUp(EditorAssetItem assetItem)
        {
            if (currentPickedGameEntity_)
            {
                DestroyGameEntity(currentPickedGameEntity_);    // 删除它
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
            currentPickedGameEntity_ = entity;
            currentPickedGameEntity_.SetActive(false); // 先隐藏， 鼠标到地面上时才显示

            HandleCollidersWhenPickUp(currentPickedGameEntity_);
        }

        public void PutDown()
        {
            if (currentPickedGameEntity_ != null)
            {
                int entityId = currentPickedGameEntity_.id;

                // 添加物体
                onGameEntityAdd?.Invoke(currentPickedGameEntity_);

                Undo.Record(new EditorSelectGameEntity());

                // 同时选择物体
                SelectObjectInternal(currentPickedGameEntity_.gameObject);

                currentPickedGameEntity_ = null;

                Undo.EndTransaction();  // 这个地方有两种情况：创建物体和移动物体

                HandleCollidersWhenPutdown();
            }
        }

        private GameEntity GetGameEntity(GameObject obj)
        {
            if (!obj)
            {
                return null;
            }

            var entity = obj.GetComponent<GameEntity>();
            if (!entity)
            {
                entity = obj.GetComponentInParent<GameEntity>();    // 
            }

            return entity;
        }

        private List<Collider> selectedEntityColliders_ = new List<Collider>(); // 用于还原

        /// <summary>
        /// 在拾取物体时，把Entity的碰撞去掉，否则无法选择地面
        /// </summary>
        /// <param name="entity"></param>
        private async void HandleCollidersWhenPickUp(GameEntity entity)
        {
            // 等待body
            await new EnumeratorAwaiter(WaitGameEnityBody(entity));

            // 不在放下的时候清除是因为未放下前物体可能被清除了。。。
            selectedEntityColliders_.Clear();   

            // 选中后collider禁用，避免干扰射线
            foreach (var v in entity.GetComponentsInChildren<Collider>())
            {
                if (v.enabled)
                {
                    v.enabled = false;
                    selectedEntityColliders_.Add(v);
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
        }


        private void SelectObjectInternal(GameObject obj)
        {
            // update axis
            editorAxis.SetSelectedObject(null);
            // hide outline
            if (selectedEntity)
            {
                SetOutlineEnabled(selectedEntity.gameObject, false);
            }

            if (obj)
            {
                var entity = GetGameEntity(obj);
                
                if (entity)
                {
                    if (GameWorld.ExistsEntity(entity))
                    {
                        if (!entity.isStatic)
                        {
                            editorAxis.SetSelectedObject(entity.gameObject);
                        }

                        // 
                        onSelectGameEntity?.Invoke(entity);

                        // 显示属性
                        uiRootWindow.GetChildWindow<InspectorPage>().SetTarget(entity.gameObject);
                        uiRootWindow.GetChildWindow<InspectorPage>().Show();

                        // 显示选中状态
                        SetOutlineEnabled(entity.gameObject, true);
                    }
                    else
                    {
                        Logger.Error($"{entity.name} not exists in GameWorld....");
                    }
                }
                else
                {
                    onSelectGameEntity?.Invoke(null);
                }

            }
            else
            {
                onSelectGameEntity?.Invoke(null);
                // 显示属性
                uiRootWindow.GetChildWindow<InspectorPage>().SetTarget(null);
                uiRootWindow.GetChildWindow<InspectorPage>().Show();
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
            var entity = GameWorld.AddGameEntity(assetItem.asset, assetItem.type);
            entity.layer = mapEditorLayer;

            Utils.DoAsync(async () =>
            {
                await entity.InstantiateBody();

                Utils.SetLayerRecursively(entity.gameObject,mapEditorLayer);

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
            GameWorld.DestroyGameEntity(entity);
        }

        /// <summary>
        /// 复制物体
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public GameEntity DuplicateGameEntity(GameEntity entity)
        {
            var newEntity = GameEntityManager.DuplicateGameEntity(entity);
            GameWorld.AddExistGameEntity(newEntity);

            onGameEntityAdd?.Invoke(newEntity);

            SelectGameEntity(newEntity);

            return newEntity;
        }

        public void SwitchOptionMode(OptionModel mode)
        {
            curOptionMode_ = mode;

            // 鼠标样式修改
            if(mode == OptionModel.kAttach || mode == OptionModel.kView)
            {
                editorAxis.SetOptionModeWithoutNotify(EditorAxis.Mode.kNone);
            }
            else
            {
                editorAxis.SetOptionMode((EditorAxis.Mode)(mode));
            }
            editorAxis.enabled = (mode == OptionModel.kTranslate || mode == OptionModel.kRotate || mode == OptionModel.kScale);

            // 鼠标控制视角移动只能在view模式
            if(flyCamera_)
            {
                flyCamera_.freezeMouseMove = (optionModel != OptionModel.kView);
            }
        }

        IEnumerator LookAtTarget(Transform target)
        {
            if (flyCamera_)
            {
                while (Vector3.SqrMagnitude((flyCamera_.viewCenter - target.position)) > 0.000001f)
                {
                    yield return null;

                    flyCamera_.viewCenter = Vector3.Lerp(flyCamera_.viewCenter, target.position, 0.8f);
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

        private bool isMovingGameEntity_ = false;

        /// <summary>
        /// 物体点选、框选等
        /// </summary>
        void ObjectOptionalInternal()
        {
            // 没有相机时不操作
            if (!editorCamera)
            {
                return;
            }

            RaycastHit hit;
            Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);
            if (currentPickedGameEntity_)
            {
                if (Physics.Raycast(ray, out hit, maxRayDistance, selectableMask))
                {
                    print(hit.collider.gameObject);
                    currentPickedGameEntity_.transform.position = hit.point;
                    if (placeGameEntityForwardHitNormal)
                    {
                        currentPickedGameEntity_.transform.up = hit.normal;
                    }
                    currentPickedGameEntity_.SetActive(true);
                }

                if (Input.GetMouseButtonUp(0))
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
                    RaycastGameEntity();

                    if (selectedEntity)
                    {
                        HandleCollidersWhenPickUp(selectedEntity);
                    }
                }

                // 物体随鼠标走
                if (Input.GetMouseButton(0) && selectedEntity != null && !selectedEntity.isStatic && Physics.Raycast(ray, out hit, maxRayDistance, selectableMask))
                {
                    selectedEntity.position = hit.point;
                    if (placeGameEntityForwardHitNormal)
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

            // 常规物体操作模式
            if (optionModel == OptionModel.kTranslate || optionModel == OptionModel.kRotate || optionModel == OptionModel.kScale)
            {
                if(!isMovingGameEntity_ && editorAxis.isDragging) // 开始拖动
                {
                    isMovingGameEntity_ = true;

                    Undo.BeginTransaction();
                    Undo.Record(new EditorMoveGameEntity(selectedEntity.id));
                }

                if(isMovingGameEntity_ && !editorAxis.isDragging) // 结束拖动
                {
                    isMovingGameEntity_= false;
                    Undo.EndTransaction();
                }

                // 选择物体
                if (editorAxis.activeAxisName == EditorAxis.kEmptyAxisName && Input.GetMouseButtonUp(0))
                {
                    RaycastGameEntity();
                }

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

        /// <summary>
        /// 保存
        /// </summary>
        /// <returns></returns>
        public bool Save()
        {
            if(GameWorld.GetStartPositionCount() == 0)
            {
                Logger.Error("Save map with start position count 0.");
                DaemonUI.makeSurePage.Tips("角色初始位置数量不能为0！");
                return false;
            }

            cachedUndoIndex_ = Undo.GetIndex();

            // 天气写入
            loadedGameMap.weather = new WeatherDesc()
            {
                type = Gameplay.weatherSystem.weatherType,
                time = Gameplay.weatherSystem.time,
                fogDensity = Gameplay.weatherSystem.fogDensity,
                windDir = Gameplay.weatherSystem.windDirection,
                windForce = Gameplay.weatherSystem.windForce,
            };

            // 保存工程
            currentProj.Save();

            onSave.Invoke();

            return true;
        }

        public void BuildGameMap()
        {
            // 保存
            Save();

            // 天气写入
            loadedGameMap.weather = new WeatherDesc()
            {
                type = Gameplay.weatherSystem.weatherType,
                time = Gameplay.weatherSystem.time,
                fogDensity = Gameplay.weatherSystem.fogDensity,
                windDir = Gameplay.weatherSystem.windDirection,
                windForce = Gameplay.weatherSystem.windForce,
            };

            currentProj.Build();

            DaemonUI.makeSurePage.Tips($"'{loadedGameMap.name}'构建成功");
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

            DaemonUI.makeSurePage.ShowOptions("是否保存当前脚本？", () =>
            {
                // save
                // 保存
                if (string.IsNullOrEmpty(Gameplay.loadedGameMapFile))
                {
                    var page = uiRootWindow.GetChildWindow<GameMapSavePage>();
                    page.Show(StopEditor, null);
                }
                else
                {
                    if (Save())
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
            OutlineEffect.Apply(editorCamera);
        }

        private void SetOutlineEnabled(GameObject go, bool enable)
        {
            if (enable)
            {
                OutlineEffect.AddOrEnableOutline(go);
            }
            else
            {
                OutlineEffect.DisableOutline(go);
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
            flyCamera_.enabled = false;
        }

        /// <summary>
        /// 解锁编辑器相机，不让操作
        /// </summary>
        public void UnlockEditorCamera()
        {
            flyCamera_.enabled = true;
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
            previewCamera.gameObject.SetActive(isPreviewMode_);
            editorCamera.gameObject.SetActive(!isPreviewMode_);
        }

        /// <summary>
        /// 用于自定义创建编辑器摄像机
        /// </summary>
        /// <returns></returns>
        protected virtual Camera OnCreatePreviewCamera()
        {
            return null;
        }

        private void OnDestroy()
        {
            Reset();
        }

    }
}
