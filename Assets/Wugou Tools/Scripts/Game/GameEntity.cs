using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Wugou.Editor;

namespace Wugou
{
    /// <summary>
    /// 基本的游戏实体，作用类似Unity的GameObject
    /// </summary>
    [System.Serializable]
    [HasGameComponentView]
    [CustomGameComponentView(typeof(GameEntityCommonView))]
    public class GameEntity : MonoBehaviour
    {
        /// <summary>
        /// id 用于唯一标识（包括网络同步的情况下）
        /// </summary>
        [SerializeField]
        public int id;

        /// <summary>
        /// 名称，name被GameObject占用了，所以用这个
        /// </summary>
        [SerializeField]
        public new string name { get { return gameObject.name; } set { gameObject.name = value; } }

        /// <summary>
        /// 资产，用于创建可见模型
        /// </summary>
        [SerializeField]
        public string asset;

        /// <summary>
        /// 原型，当前使用Unity Prefab来表示原型，即一个空的GameObject+Components表示场景中的对象
        /// </summary>
        [SerializeField]
        public string prototype;

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

        [SerializeField]
        public int layer { get { return gameObject.layer; } set { gameObject.layer = value; } }

        public GameObject body { get; private set; }

        public const string kBodyName = "Body";

        /// <summary>
        /// 实例化模型后调用
        /// </summary>
        public UnityEvent onLoadBody = new UnityEvent();

        private void OnValidate()
        {
            if (!Utils.IsPrefab(gameObject) && id == 0)
            {
                id = GameEntityManager.AllocateEntityId();
            }
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
                GameObject go = GameObject.Instantiate<GameObject>(goPfb, transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;

                // 保持和entity相同tag和layer属性
                Utils.SetLayerRecursively(go,gameObject.layer);
                go.tag = gameObject.tag;
                go.name = kBodyName;

                // send to other component
                //foreach(var v in GetComponents<GameComponent>())
                //{
                //    v.OnLoadBody();
                //}
                onLoadBody.Invoke();

                body = go;
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
