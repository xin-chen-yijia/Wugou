using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 用于鼠标点击等事件数据记录
    /// </summary>
    public class PointerEventContext
    {
        public GameObject source;
        public RaycastHit raycastHit;

        public Collider collider => raycastHit.collider;
        public Vector3 point => raycastHit.point;
        public Vector3 normal => raycastHit.normal;
    }



}