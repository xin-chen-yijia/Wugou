using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Wugou.Editor;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace Wugou
{
    /// <summary>
    /// 基本的游戏实体，作用类似Unity的GameObject
    /// </summary>
    [System.Serializable]
    [CustomGameComponentView(typeof(GameEntityCommonView))]
    public class GameEntity : MonoBehaviour
    {
        /// <summary>
        /// id 用于唯一标识（包括网络同步的情况下）
        /// </summary>
        [SerializeField]    // 注意，标识出来时为了保存脚本的时候序列化，而非Unity属性面板的显示
        public int id;

        /// <summary>
        /// 标识是否一开始就在场景中
        /// </summary>
        public bool isFromTheBeginning = false;

        /// <summary>
        /// 名称，name被GameObject占用了，所以用这个
        /// </summary>
        [SerializeField]
        public new string name { get { return gameObject.name; } set { gameObject.name = value; } }

        /// <summary>
        /// 资产，用于创建可见模型，也就是body
        /// </summary>
        [SerializeField]
        public string asset { get; set; }

        /// <summary>
        /// 原型名称，使用Prefab来表示原型，GameEntity=prototype(prefab) + asset(body)
        /// </summary>
        [SerializeField]
        public string prototype { get; set; }

        [SerializeField]
        public Vector3 position { get { return transform.position; } set { transform.position = value; } }

        [SerializeField]
        public Vector3 eulerAngles { get { return transform.eulerAngles; } set { transform.eulerAngles = value; } }

        public Quaternion rotation { get { return transform.rotation; } set { transform.rotation = value; } }

        [SerializeField]
        public Vector3 localScale { get { return transform.localScale; } set { transform.localScale = value; } }

        [SerializeField]
        public bool activeSelf { get { return gameObject.activeSelf; } set { gameObject.SetActive( value); } }

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

        /// <summary>
        /// 实例化模型后调用
        /// </summary>
        public UnityEvent onInstantiateBody = new UnityEvent();

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (id == 0 && !Utils.IsPrefab(gameObject) && PrefabStageUtility.GetCurrentPrefabStage() == null)
            {
                id = GameEntityManager.AllocateEntityId();
                isFromTheBeginning = true;
            }
#endif
        }

        /// <summary>
        /// 实例化物体
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        public async Task<GameObject> InstantiateBody()
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

                // 保持和entity相同tag和layer属性
                go.layer = gameObject.layer;    // 不递归设置子物体的layer
                go.tag = gameObject.tag;
                go.name = kBodyName;

                body = go;

                onInstantiateBody.Invoke();
                return go;
            }
        }

        /// <summary>
        /// 更新Entity的模型
        /// </summary>
        /// <param name="asset"></param>
        public async Task<GameObject> UpdateBodyAsset(string asset)
        {
            var body = transform.Find(kBodyName)?.gameObject;
            if (body)
            {
                GameObject.Destroy(body);   // 删除原有的
            }

            this.asset = asset;
            return await InstantiateBody();
        }

        /// <summary>
        /// 设置为一个已有的GameObject为body
        /// </summary>
        /// <param name="body"></param>
        public void SetBody(GameObject body)
        {
            this.body = body;
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
