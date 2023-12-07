using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Wugou.Multiplayer;

namespace Wugou
{
    /// <summary>
    /// ��������Ϸʵ�壬��������Unity��GameObject
    /// </summary>
    [CopyToClient]
    public class GameEntity : GameComponent
    {
        /// <summary>
        /// id ����Ψһ��ʶ����������ͬ��������£�
        /// </summary>
        [HideInInspector]
        public int id;

        /// <summary>
        /// ���ƣ�name��GameObjectռ���ˣ����������
        /// </summary>
        [HideInInspector]
        [SerializeField]
        public string entityName { get { return name; } set { name = value; } }

        /// <summary>
        /// ����ʵ����������
        /// </summary>
        public GameEntityBlueprint blueprint;

        [HideInInspector]
        [SerializeField]
        public Vector3 position { get { return transform.position; } set { transform.position = value; } }

        [HideInInspector]
        [SerializeField]
        public Vector3 eulerAngles { get { return transform.eulerAngles; } set { transform.eulerAngles = value; } }

        /// <summary>
        /// ʵ��������
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        public static async Task<GameObject> Instantiate(GameEntity entity)
        {
            var entityAsset = entity.blueprint.asset;
            GameObject goPfb = await GameAssetDatabase.LoadAssetAsync<GameObject>(entityAsset);
            GameObject go = GameObject.Instantiate<GameObject>(goPfb, entity.transform);
            if (!go)
            {
                go = new GameObject();
                go.transform.SetParent(entity.transform);
            }

            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            // ���ֺ�entity��ͬtag��layer����
            go.layer = entity.gameObject.layer;
            go.tag = entity.gameObject.tag;
            go.name = "Body";

            return go;
        }

    }
}
