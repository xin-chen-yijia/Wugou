using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// ��Ϸ����������������������л��ͷ����л�
    /// </summary>
    public class GameEntityBlueprint
    {
        /// <summary>
        /// �ʲ������ڴ����ɼ�ģ��
        /// </summary>
        public string asset;

        /// <summary>
        /// ԭ�ͣ���ǰʹ��Unity Prefab����ʾԭ�ͣ���һ���յ�GameObject+Components��ʾ�����еĶ���
        /// </summary>
        public string prototype;
    }
}

