using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// ��������Ϸʵ�壬��������Unity��GameObject
    /// </summary>
    public class GameEntity : GameComponent
    {
        /// <summary>
        /// id ����Ψһ��ʶ����������ͬ��������£�
        /// </summary>
        [HideInInspector]
        public int id;

        /// <summary>
        /// ���ƣ�name��ռ���ˣ����������
        /// </summary>
        [SerializeField]
        public string entityName { get { return name; } set { name = value; } }

        /// <summary>
        /// ����ʵ����������
        /// </summary>
        public GameEntityBlueprint blueprint;

        [SerializeField]
        public Vector3 position { get { return transform.position; } set { transform.position = value; } }

        [SerializeField]
        public Vector3 eulerAngles { get { return transform.eulerAngles; } set { transform.eulerAngles = value; } }


    }
}
