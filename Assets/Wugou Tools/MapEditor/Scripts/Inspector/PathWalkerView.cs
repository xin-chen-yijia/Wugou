using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.MapEditor
{
    public class PathWalkerView : GameComponentView<PathWalker>
    {
        public GameObject posRowPrefab;
        public GameObject posRowContainer;

        public Button editorButton;
        public Button addButton;

        public TMP_InputField speedInput;

        private bool isEditing = false;
        private bool isPreviewing = false;

        private Vector3 cachedPosition;
        private Quaternion cachedRotation;

        public override void Start()
        {
            base.Start();

            posRowPrefab.SetActive(false);

            editorButton.onClick.AddListener(() =>
            {
                SetEditorActive(!isEditing);
            });

            speedInput.onValueChanged.AddListener((value) =>
            {
                float t = targetComponent.speed;
                if(float.TryParse(value, out t) )
                {
                    targetComponent.speed = t;
                }
            });

            speedInput.onDeselect.AddListener((value) =>
            {
                speedInput.text = targetComponent.speed.ToString();
            });

            //
            addButton.onClick.AddListener(() =>
            {
                var point = new PathWalker.PathPoint
                {
                    position = targetComponent.gameObject.transform.position,
                    eulerAngles = targetComponent.gameObject.transform.eulerAngles
                };
                targetComponent.points.Add(point);
                AddPointUIInternal(targetComponent.points.Count - 1);
            });

            //var previewButton = content.Find("Buttons/PreviewButton").GetComponent<Button>();
            //previewButton.onClick.AddListener(() =>
            //{
            //    isPreviewing = !isPreviewing;
            //    if (isPreviewing)
            //    {
            //        previewButton.GetComponentInChildren<TMP_Text>().text = "停止";
            //        targetComponent.PushState();
            //        targetComponent.StartWalk();
            //    }
            //    else
            //    {
            //        targetComponent.Stop();
            //        OnStopPreview();
            //    }
            //});
        }

        void SetEditorActive(bool active)
        {
            isEditing = active;
            editorButton.GetComponentInChildren<TMP_Text>().text = isEditing ? "结束编辑" : "开始编辑";

            if (isEditing)
            {
                cachedPosition = target.transform.position;
                cachedRotation = target.transform.rotation;
            }
            else
            {
                target.transform.position = cachedPosition;
                target.transform.rotation = cachedRotation;
            }

            addButton.gameObject.SetActive(isEditing);

            foreach (var v in posRowContainer.GetComponentsInChildren<Button>())
            {
                v.interactable = isEditing;
                var c = v.GetComponentInChildren<TMP_Text>().color;
                c.a = isEditing ? 1 : 0.5f;
                v.GetComponentInChildren<TMP_Text>().color = c;
            }
        }

        private void AddPointUIInternal(int index)
        {
            GameObject go = Instantiate<GameObject>(posRowPrefab, posRowContainer.transform);
            go.SetActive(true);
            var point = targetComponent.points[index];
            go.transform.Find("LocateButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                targetComponent.transform.position = point.position;
                targetComponent.transform.eulerAngles = point.eulerAngles;
            });
            go.transform.Find("DelButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                var n = go.transform.GetSiblingIndex();
                targetComponent.points.RemoveAt(n); 
                GameObject.Destroy(go);

                // remove gizmo
                GameObject.DestroyImmediate(entityGizmo.transform.GetChild(n).gameObject);
            });

            go.transform.Find("Pos/X").GetComponent<TMP_Text>().text = point.position.x.ToString();
            go.transform.Find("Pos/X").GetComponent<TMP_Text>().text = point.position.y.ToString();
            go.transform.Find("Pos/X").GetComponent<TMP_Text>().text = point.position.z.ToString();

            Utils.ResizeContainerHeight(posRowContainer);

            // gizmos
            var gizmoObj = Instantiate<GameObject>(target.transform.Find("Body").gameObject, entityGizmo.transform);
            gizmoObj.transform.position = point.position;
            gizmoObj.transform.eulerAngles = point.eulerAngles;
            gizmoObj.layer = MapEditorSystem.mapGizmosLayer;
            while (gizmoObj.GetComponentInChildren<Collider>()) // 避免被选中
            {
                GameObject.DestroyImmediate(gizmoObj.GetComponentInChildren<Collider>());
            }
            foreach(var v in gizmoObj.GetComponentsInChildren<Renderer>())
            {
                v.material = Resources.Load<Material>("Materials/Gizmo50");
            }
        }

        private void OnStopPreview()
        {
            content.Find("Buttons/PreviewButton").GetComponentInChildren<TMP_Text>().text = "预览";
            targetComponent.PopState();
            isPreviewing = false;
        }

        public override void OnNewTarget(GameObject target)
        {
            if (target)
            {
                if (gizmoParent_ == null)
                {
                    gizmoParent_ = new GameObject("GizmoParent");
                }

                if (entityGizmo != null)
                {
                    entityGizmo.SetActive(false);
                    SetEditorActive(false);
                }

                string gizmoName = target.GetComponent<GameEntity>().id.ToString();
                var tmp = gizmoParent_.transform.Find(gizmoName);
                if (!tmp)
                {
                    entityGizmo = new GameObject(gizmoName);
                    entityGizmo.transform.SetParent(gizmoParent_.transform);
                }
                else
                {
                    entityGizmo = tmp.gameObject;
                    entityGizmo.SetActive(true);
                }


                targetComponent.PushState();

                // 清理， TODO: reuse
                foreach(Transform v in posRowContainer.transform)
                {
                    GameObject.Destroy(v.gameObject);
                }

                for(int i=0;i < targetComponent.points.Count;++i)
                {
                    AddPointUIInternal(i);
                }

                speedInput.text = targetComponent.speed.ToString();
            }

            base.OnNewTarget(target);


        }

        private static GameObject gizmoParent_ = null;
        public GameObject entityGizmo { get;private set; }

        private void OnDisable()
        {
            // hide
            if(entityGizmo != null)
            {
                entityGizmo.SetActive(false);
                if(isEditing)
                {
                    SetEditorActive(false);
                }
            }

        }

    }
}
