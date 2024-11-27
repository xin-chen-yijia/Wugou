using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 检测角色是否在掉落并矫正
    /// </summary>
    public class CharacterFallingCorrect : MonoBehaviour
    {
        /// <summary>
        /// 检查到掉落后，用于矫正的位置
        /// </summary>
        public Vector3 forCorrectPosition;

        private CapsuleCollider capsuleCollider_;

        // Start is called before the first frame update
        void Start()
        {
            //
            capsuleCollider_ = GetComponent<CapsuleCollider>();
        }

        // Update is called once per frame
        void Update()
        {
            // 判断是否在地面下
            if (!Physics.SphereCast(transform.position + Vector3.up * capsuleCollider_.height * 0.5f, capsuleCollider_.radius, Vector3.down, out RaycastHit hit, 100))
            {
                GetComponent<Rigidbody>().isKinematic = true;

                // 重置回初始位置
                transform.position = forCorrectPosition;

                // apply to rigidbody
                GetComponent<Rigidbody>().position = transform.position;

                Wugou.Utils.DoAsync(async () =>
                {
                    await new YieldInstructionAwaiter(null);
                    GetComponent<Rigidbody>().isKinematic = false;
                });
            }
        }
    }
}

