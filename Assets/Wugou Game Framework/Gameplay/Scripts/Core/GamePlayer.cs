using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 代表当前玩家控制的角色
    /// </summary>
    public class GamePlayer : MonoBehaviour
    {
        public static GamePlayer instance { get; private set; }

        public virtual Camera playerCamera { get; }

        public int selectableMask { get; set; } = Physics.AllLayers & ~(Physics.IgnoreRaycastLayer);

        [Tooltip("拾取物体射线的最长距离")]
        public float maxSelectDistance = 100;

        protected bool _enablePointerEvent = true;
        /// <summary>
        /// 光标相关事件是否启用，如click,hover
        /// </summary>
        public bool enablePointerEvent
        {
            get
            {
                return _enablePointerEvent;
            }

            set
            {
                _enablePointerEvent = value;
                if (!value) // 清理
                {
                    PointerExit();
                    selectedObject = null;
                }
            }
        }

        private System.Func<GameEntity, bool> IsGameEntitySelectable = null;

        /// <summary>
        /// 选择的物体
        /// </summary>
        public GameObject selectedObject { get; protected set; }

        public GameEntity selectedGameEntity {
            get 
            {
                if( selectedObject == null)
                {
                    return null;
                }
                var entity = selectedObject.GetComponentInParent<GameEntity>();
                if(IsGameEntitySelectable == null || IsGameEntitySelectable(entity))
                {
                    return entity;
                }

                return null;
            }
        }

        /// <summary>
        /// 鼠标所悬停在的物体
        /// </summary>
        public GameObject hoverObject { get; protected set; }

        /// <summary>
        /// 当前持有装备
        /// </summary>
        public virtual Equip curEquip { get; }

        public virtual void Awake()
        {
            Debug.Assert(instance == null);
            instance = this;
        }

        // Start is called before the first frame update
        public virtual void Start()
        {
        }

        // Update is called once per frame
        public virtual void Update()
        {
            var lastSelectedEntity = selectedGameEntity;

            // 判断是否在UI上
            if (!Utils.IsPointerOnUI() && enablePointerEvent && playerCamera)
            {
                RaycastHit hit;
                Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
                // TODO: 距离过远是否也出发exit？
                if (Physics.Raycast(ray, out hit, maxSelectDistance, selectableMask))
                {
                    var hitObj = hit.collider.gameObject;
                    if (hoverObject != hitObj)
                    {
                        // exit
                        PointerExit();

                        // enter
                        PointerEnter(hit);
                    }

                    // hover
                    PointerHover(hit);

                    // click
                    if (Input.GetMouseButtonDown(0))  // playerCamera 可能会禁用
                    {
                        PointerClick(hitObj, hit);
                    }
                }
                else
                {
                    PointerExit();

                    // 未选这种物体
                    if (Input.GetMouseButtonDown(0))
                    {
                        PointerClick(null, hit);
                    }
                }
            }
            else
            {
                PointerExit();
            }

            //if (selectedGameEntity && !selectedGameEntity.gameObject.activeInHierarchy)
            //{

            //}
        }

        void PointerEnter(RaycastHit hit)
        {
            var go = hit.collider.gameObject;
            foreach (var v in go.GetComponentsInParent<IPointerEnterSolver>())
            {
                v.OnPointerEnter(new PointerEventContext() { source = gameObject, raycastHit = hit });
            }

            AddOutline(go.GetComponentInParent<GameEntity>());
        }

        void PointerHover(RaycastHit hit)
        {
            hoverObject = hit.collider.gameObject;
            foreach (var v in hoverObject.GetComponentsInParent<IPointerHoverSolver>())
            {
                v.OnPointerHover(new PointerEventContext() { source = gameObject, raycastHit = hit });
            }
        }

        void PointerExit()
        {
            if (hoverObject)
            {
                foreach (var v in hoverObject.GetComponentsInParent<IPointerExitSolver>(true))
                {
                    v.OnPointerExit(new PointerEventContext() { source = gameObject });
                }

                if (selectedObject != hoverObject)
                {
                    RemoveOutline(hoverObject.GetComponentInParent<GameEntity>());
                }
            }

            hoverObject = null;
        }

        void PointerClick(GameObject go, RaycastHit hit)
        {
            if (selectedGameEntity)
            {
                RemoveOutline(selectedGameEntity);
            }

            if (go)
            {
                foreach (var v in go.GetComponentsInParent<IPointerClickSolver>())
                {
                    v.OnPointerClick(new PointerEventContext() { source = gameObject, raycastHit = hit });
                }

                curEquip?.OnPointerClick(go);

                AddOutline(go.GetComponentInParent<GameEntity>());
            }

            selectedObject = go;
        }

        /// <summary>
        /// 启用描边
        /// </summary>
        /// <param name="filter">描边物体判定</param>
        public void EnableOutline(System.Func<GameEntity, bool> filter)
        {
            OutlineEffect.Apply(playerCamera);
            OutlineEffect.enable = true;
            IsGameEntitySelectable = filter;

            AddOutline(selectedGameEntity);
        }

        /// <summary>
        /// 禁用描边
        /// </summary>
        public void DisableOutline()
        {
            RemoveOutline(selectedGameEntity);
            OutlineEffect.enable = false;
        }

        private void AddOutline(GameEntity entity)
        {
            if (OutlineEffect.enable && entity && (IsGameEntitySelectable == null || IsGameEntitySelectable(entity)))
            {
                OutlineEffect.AddOutline(entity.gameObject);
            }
        }

        private void RemoveOutline(GameEntity entity)
        {
            if (OutlineEffect.enable && entity)
            {
                OutlineEffect.RemoveOutline(entity.gameObject);
            }
        }
    }
}

