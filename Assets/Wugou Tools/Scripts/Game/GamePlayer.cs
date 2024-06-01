using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Wugou
{
    /// <summary>
    /// 角色可选择
    /// </summary>
    public interface IGamePlayerChosen
    {
        public void OnGamePlayerChosen(GamePlayer player, GameObject hitObj);
    }

    /// <summary>
    /// 代表当前玩家控制的角色
    /// </summary>
    public class GamePlayer : MonoBehaviour
    {
        public static GamePlayer instance { get; private set; }

        public virtual Camera playerCamera { get; }

        [Tooltip("拾取物体射线的最长距离")]
        public float maxSelectDistance = 100;

        /// <summary>
        /// 点击物体，触发IGamePlayerChosen
        /// </summary>
        public bool enableGameObjectChosen { get; set; } = true;

        public GameObject selectedObject { get; protected set; }

        /// <summary>
        /// 鼠标所悬停在的物体
        /// </summary>
        private GameObject hoverObject_ = null;

        public virtual void Awake()
        {
            instance = this;
        }

        // Start is called before the first frame update
        public virtual void Start()
        {
        }

        // Update is called once per frame
        public virtual void Update()
        {
            if (hoverObject_)
            {
                OutlineEffect.DisableOutline(hoverObject_);
                hoverObject_ = null;
            }

            // 判断是否在UI上
            if (enableGameObjectChosen && !Utils.IsPointerOnUI() && playerCamera)
            {
                RaycastHit hit;
                Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
                //Debug.DrawRay(ray.origin, ray.origin + ray.direction * 100, Color.red);
                if (Physics.Raycast(ray, out hit, maxSelectDistance))
                {
                    var hitObj = hit.collider.gameObject;
                    if (hitObj.GetComponentInParent<GameEntity>())
                    {
                        hoverObject_ = hitObj;

                        if (Input.GetMouseButtonDown(0))  // playerCamera 可能会禁用
                        {
                            selectedObject = hoverObject_;
                            OnSelectObject(hit.collider.gameObject);

                            foreach (var v in hoverObject_.GetComponentsInParent<IGamePlayerChosen>())
                            {
                                v.OnGamePlayerChosen(this, hoverObject_);
                            }
                        }

                        OutlineEffect.AddOrEnableOutline(hoverObject_);
                    }
                }
                else
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        selectedObject = null;
                    }
                }
            }
        }

        /// <summary>
        /// 启用描边
        /// </summary>
        public void EnableOutline()
        {
            OutlineEffect.Apply(playerCamera);
            OutlineEffect.enable = true;
        }

        /// <summary>
        /// 禁用描边
        /// </summary>
        public void DisableOutline()
        {
            OutlineEffect.enable = false;
        }

        protected virtual void OnSelectObject(GameObject go)
        {

        }
    }
}

