using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Wugou.Editor.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Wugou
{
    /// <summary>
    /// 基本的游戏实体，作用类似Unity的GameObject
    /// </summary>
    [EntitySerializable]
    [CustomGameComponentView(typeof(GameEntityCommonView))]
    public sealed class GameEntity : MonoBehaviour
    {
        public const string DefaultPrototype = "Default";

        /// <summary>
        /// id 用于唯一标识（包括网络同步的情况下）
        /// </summary>
        [EntitySerializeField]    // 注意，标识出来时为了保存脚本的时候序列化，而非Unity属性面板的显示
        public int id;

        /// <summary>
        /// 标识是否一开始就在场景中
        /// </summary>
        [HideInInspector]
        public bool isFromTheBeginning = false;

        /// <summary>
        /// 名称，name被GameObject占用了，所以用这个
        /// </summary>
        [EntitySerializeField]
        public new string name { get { return gameObject.name; } set { gameObject.name = value; } }

        /// <summary>
        /// 资产，用于创建可见模型，也就是body
        /// </summary>
        [EntitySerializeField]
        public string asset { get; set; }

        /// <summary>
        /// 原型名称，使用Prefab来表示原型，GameEntity=prototype(prefab) + asset(body)
        /// </summary>
        [EntitySerializeField]
        public string prototype { get; set; } = DefaultPrototype;

        [EntitySerializeField]
        public Vector3 position { get { return transform.position; } set { transform.position = value; } }

        [EntitySerializeField]
        public Vector3 eulerAngles { get { return transform.eulerAngles; } set { transform.eulerAngles = value; } }

        public Quaternion rotation { get { return transform.rotation; } set { transform.rotation = value; } }

        [EntitySerializeField]
        public Vector3 localScale { get { return transform.localScale; } set { transform.localScale = value; } }

        [EntitySerializeField]
        public bool activeSelf { get { return gameObject.activeSelf; } set { gameObject.SetActive(value); } }

        public void SetActive(bool value) => gameObject.SetActive(value);

        //[SerializeField]
        public int layer { get { return gameObject.layer; } set { gameObject.layer = value; } }

        /// <summary>
        /// 一开始就在场景中的物体当作时静态物体，不可移动
        /// </summary>
        public bool isStatic => isFromTheBeginning;

        /// <summary>
        /// 使用asset创建的模型
        /// </summary>
        public GameObject body { get; private set; }

        public const string kBodyName = "Body";

        /// <summary>
        /// 需要序列化
        /// 主要是为了处理：随场景来的那些GameEntity如果没有改动，则不需要保存
        /// TODO: 可对原始的那些GameEntity进行更改
        /// </summary>
        public bool needSerialize => !isFromTheBeginning;


        // hasSpawned should always be false before runtime
        [SerializeField,HideInInspector]
        private bool hasSpawned = false;

        public bool SpawnedFromInstantiate {  get; private set; }

        private void Awake()
        {
            if (hasSpawned)
            {
                Debug.LogError($"GameEntity {name} has already spawned. This meybe wrong...");
                SpawnedFromInstantiate = true;
                Destroy(gameObject);
            }
            hasSpawned = true;
        }

#if UNITY_EDITOR
        private static HashSet<int> entityIdInScenes_ = new HashSet<int>();
        private static string curScene_;
#endif

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !Utils.IsPrefab(gameObject) && PrefabStageUtility.GetCurrentPrefabStage() == null)
            {
                var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (curScene_ != sceneName)
                {
                    curScene_ = sceneName;
                    entityIdInScenes_.Clear();  // 切换场景后清理
                }

                isFromTheBeginning = true;
                if(id == 0 || entityIdInScenes_.Contains(id))
                {
                    id = GameEntityManager.AllocateEntityId();
                }

                Debug.Assert(!entityIdInScenes_.Contains(id));
                entityIdInScenes_.Add(id);
            }
#endif
        }

        private static Queue<GameEntity> needInstantiateBodyEntities_ = new Queue<GameEntity>();
        private static bool isInstantiating_ = false;
        private static HashSet<int> instantiatingEntities_ = new HashSet<int>();    // 异步操作，所以需要记录正在实例化的物体

        private void Start()
        {
            LoadBodyInternal();
        }

        /// <summary>
        /// 加载资产
        /// </summary>
        private void LoadBodyInternal()
        {
            if (body || needInstantiateBodyEntities_.Contains(this) || instantiatingEntities_.Contains(id))
            {
                return;
            }

            Debug.Assert(!string.IsNullOrEmpty(prototype));
            if (!string.IsNullOrEmpty(asset))
            {
                needInstantiateBodyEntities_.Enqueue(this);
                if (!isInstantiating_)
                {
                    DoInstiateBodyWork();
                }
            }
            else
            {
                SetBody(gameObject);
            }
        }

        async void DoInstiateBodyWork()
        {
            isInstantiating_ = true;
            while(needInstantiateBodyEntities_.TryDequeue(out GameEntity head))
            {
                instantiatingEntities_.Add(head.id);
                await head.InstantiateBody();
                instantiatingEntities_.Remove(head.id);
            }

            isInstantiating_ = false;
        }

        private void OnDestroy()
        {
            // Objects spawned from Instantiate are not allowed 
            if (SpawnedFromInstantiate)
            {
                return;
            }
        }

        /// <summary>
        /// 实例化物体
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        private async Task<GameObject> InstantiateBody()
        {
            var entityAsset = asset;
            GameObject goPfb = await GameAssetDatabase.GetAssetAsync<GameObject>(entityAsset);
            if (!goPfb)
            {
                Logger.Warning($"No '{entityAsset}' in GameAssetDatabase..");
                return null;
            }
            else
            {
                // 因为是异步，这里可能会出现物体被删除的情况（比如超时退出游戏了)，所以再检查一次
                if (!gameObject)
                {
                    Logger.Warning("GameEntity be destroyed when InstantiateBody.. ");
                    return null;
                }
                GameObject go = GameObject.Instantiate<GameObject>(goPfb, transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.SetAsFirstSibling();   // 设置为第一个子物体，用于GetComponentInChidren等接口查找对象

                // 保持和entity相同tag和layer属性
                go.layer = gameObject.layer;    // 不递归设置子物体的layer
                go.tag = gameObject.tag;
                go.name = kBodyName;

                Debug.Assert(body == null);
                SetBody(go);

                return go;
            }
        }

        /// <summary>
        /// 更新Entity的模型
        /// </summary>
        /// <param name="asset"></param>
        public async Task<GameObject> ReplaceBodyAsset(string asset)
        {
            if(this.asset == asset)
            {
                if(body == null)
                {
                    await new EnumeratorAwaiter(new WaitUntil(() => { return body != null; }));
                }
                return body;
            }

            if (body && body != gameObject)
            {
                GameObject.Destroy(body);   // 删除原有的
            }

            body = null;
            this.asset = asset;
            return await InstantiateBody();
        }

        /// <summary>
        /// 设置为一个已有的GameObject为body
        /// </summary>
        /// <param name="body"></param>
        private void SetBody(GameObject body)
        {
            this.body = body;

            foreach (var v in GetComponents<IOnLoadedGameEntityBody>())
            {
                v.OnLoadedGameEntityBody(body);
            }
        }

        public T AddComponent<T>() where T: Component
        {
            var comp = gameObject.AddComponent<T>();
            if (body)
            {
                if (comp is IOnLoadedGameEntityBody it)
                {
                    it.OnLoadedGameEntityBody(body);
                }
            }

            return comp;
        }

        #region Object Compare
        private static bool CompareBase(GameEntity lhs, GameEntity rhs)
        {
            bool flag = (object)lhs == null;
            bool flag2 = (object)rhs == null;
            if (flag2 && flag)
            {
                return true;
            }

            if (flag2)
            {
                return false;
            }

            if (flag)
            {
                return false;
            }

            return lhs.id == rhs.id;
        }

        public static bool operator ==(GameEntity lhs, GameEntity rhs)
        {
            return CompareBase(lhs, rhs);
        }

        public static bool operator !=(GameEntity lhs, GameEntity rhs)
        {
            return !CompareBase(lhs, rhs);
        }

        public override bool Equals(object obj)
        {
            GameEntity other = obj as GameEntity;
            return CompareBase(this, other);
        }

        public override int GetHashCode()
        {
            return id;
        }
        #endregion
    }
}
