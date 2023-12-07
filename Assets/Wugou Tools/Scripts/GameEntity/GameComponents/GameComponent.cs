using Wugou;
using Wugou.MapEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// �̳�MonoBehaviour�ǲ����Լ���ʵ����һ�ף���Ϊ֮ǰ�ǻ���Unity����һ������
    /// �������л���
    /// 1. �������л���Ҫ����SerializeField���ԣ�
    /// 2. public �ֶμ�NonSerialized�����л���
    /// </summary>
    public class GameComponent : MonoBehaviour
    {
        #region �Զ�����������ں���
        /// <summary>
        /// ��ʼ������
        /// </summary>
        public virtual void BeginPlay()
        {

        }

        /// <summary>
        /// ÿһ֡����
        /// </summary>
        public virtual void Tick()
        {

        }

        /// <summary>
        /// ��ɾ��ʱ����
        /// </summary>
        public virtual void EndPlay()
        {

        }
        #endregion

        /// <summary>
        /// ����start����Ϊstart�ĵ�����ʹ��instantiateʱ����һ֡������ڴ����߼��кܴ��Ӱ��
        /// </summary>
        public virtual void Start()
        {
            if (GamePlay.isGaming)
            {
                BeginPlay();
            }
            //else
            //{
            //    // �������������
            //    foreach (var v in GetComponentsInChildren<Rigidbody>())
            //    {
            //        v.isKinematic = true;
            //    }
            //}
        }

        public virtual void Update()
        {
            if(GamePlay.isGaming)
            {
                Tick();
            }
        }

        public virtual void OnDestroy()
        {
            if (GamePlay.isGaming)
            {
                EndPlay();
            }
        }
    }

}
