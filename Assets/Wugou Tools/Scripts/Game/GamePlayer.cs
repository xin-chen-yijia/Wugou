using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Wugou
{
    public class GamePlayer : MonoBehaviour
    {
        public static GamePlayer instance { get; private set; }

        public GameObject body { get; protected set; } // 从assetbundle中加载的角色模型

        protected Camera mainCam;
        public float maxSelectDistance = 100;

        public virtual void Awake()
        {
            instance = this;
        }

        // Start is called before the first frame update
        public virtual void Start()
        {
            Init();
            mainCam = Camera.main;
        }

        // Update is called once per frame
        public virtual void Update()
        {
            // 判断是否在UI上
            if (!EventSystem.current || !EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.GetMouseButtonDown(0))
                {
                    RaycastHit hit;
                    Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out hit, maxSelectDistance))
                    {
                        var hitObj = hit.collider.gameObject;
                        OnSelectObject(hit.collider.gameObject);
                        foreach(var v in hitObj.GetComponentsInParent<GameComponent>())
                        {
                            v.OnPlayerChosen(this,hitObj);
                        }
                    }
                }
            }
        }

        private void Init()
        {
            InstantiateBody();
        }

        /// <summary>
        /// 实例化Body的模型
        /// </summary>
        protected virtual void InstantiateBody()
        {
            Wugou.Logger.Error("Not InstantiateBody, the body property will be null..");
        }

        protected virtual void OnSelectObject(GameObject go)
        {

        }
    }
}

