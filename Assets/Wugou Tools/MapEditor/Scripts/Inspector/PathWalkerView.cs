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
        public TMP_InputField speedInput;

        private bool isPreviewing = false;

        private PathWalker walker_ => target.GetComponent<PathWalker>();

        public override void Start()
        {
            base.Start();

            posRowPrefab.SetActive(false);

            speedInput.onValueChanged.AddListener((value) =>
            {
                float t = walker_.speed;
                if(float.TryParse(value, out t) )
                {
                    walker_.speed = t;
                }
            });

            speedInput.onDeselect.AddListener((value) =>
            {
                speedInput.text = walker_.speed.ToString();
            });

            //
            content.Find("Buttons/AddButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                var point = new PathWalker.PathPoint
                {
                    position = walker_.gameObject.transform.position,
                    eulerAngles = walker_.gameObject.transform.eulerAngles
                };
                walker_.points.Add(point);
                AddPointInternal(walker_.points.Count - 1);
            });

            var previewButton = content.Find("Buttons/PreviewButton").GetComponent<Button>();
            previewButton.onClick.AddListener(() =>
            {
                isPreviewing = !isPreviewing;
                if (isPreviewing)
                {
                    previewButton.GetComponentInChildren<TMP_Text>().text = "Õ£÷π";
                    walker_.PushState();
                    walker_.StartWalk();
                }
                else
                {
                    walker_.Stop();
                    OnStopPreview();
                }
            });
        }

        private void AddPointInternal(int index)
        {
            GameObject go = Instantiate<GameObject>(posRowPrefab, posRowContainer.transform);
            go.SetActive(true);
            go.transform.Find("Toggle").GetComponent<Toggle>().onValueChanged.AddListener((isOn) =>
            {
                if (!isOn)
                {
                    return;
                }
                walker_.transform.position = walker_.points[index].position;
                walker_.transform.eulerAngles = walker_.points[index].eulerAngles;
            });
            go.transform.Find("DelButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                walker_.points.RemoveAt(go.transform.GetSiblingIndex()); 
                GameObject.Destroy(go);
            });

            var size = posRowContainer.GetComponent<RectTransform>().sizeDelta;
            var layout = posRowContainer.GetComponent<VerticalLayoutGroup>();
            size.y += layout.spacing + go.GetComponent<RectTransform>().sizeDelta.y;
            posRowContainer.GetComponent<RectTransform>().sizeDelta = size;
        }

        private void OnStopPreview()
        {
            content.Find("Buttons/PreviewButton").GetComponentInChildren<TMP_Text>().text = "‘§¿¿";
            walker_.PopState();
            isPreviewing = false;
        }

        public override void Show()
        {
            base.Show();

            if (walker_ != null)
            {
                walker_.OnWalkStop.AddListener(OnStopPreview);
            }

        }

        public override void Hide()
        {
            base.Hide();

            if(target)
            {
                walker_.OnWalkStop.RemoveListener(OnStopPreview);

                walker_.Stop();
                //walker_.PopState();
            }

        }

        public override void OnNewTarget(GameObject target)
        {
            if (target)
            {
                walker_.PushState();

                // «Â¿Ì£¨ TODO: reuse
                foreach(Transform v in posRowContainer.transform)
                {
                    GameObject.Destroy(v.gameObject);
                }

                for(int i=0;i < walker_.points.Count;++i)
                {
                    AddPointInternal(i);
                }

                speedInput.text = walker_.speed.ToString();
            }

            base.OnNewTarget(target);


        }

    }
}
