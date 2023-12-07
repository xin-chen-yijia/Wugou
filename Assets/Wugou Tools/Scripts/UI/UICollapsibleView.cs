using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.UI
{
    using TMPro;
    using Text = TMPro.TMP_Text;

    public class UICollapsibleView : MonoBehaviour
    {
        public GameObject headObj;
        public RectTransform content;
        // 折叠相关高度
        public float foldedHeight => headObj.GetComponent<RectTransform>().rect.height;

        /// <summary>
        /// 标题
        /// </summary>
        public string head
        {
            get
            {
                return headObj.GetComponentInChildren<Text>().text;
            }

            set
            {
                headObj.GetComponentInChildren<Text>().text = value;
            }
        }

        private bool isFolded = false;



        // Start is called before the first frame update
        void Start()
        {
            transform.Find("Head/FoldButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                isFolded = !isFolded;
                if (isFolded) {
                    Fold();
                }
                else {
                    Unfold();
                }
            });
        }


        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// 添加内容
        /// </summary>
        /// <param name="content"></param>
        public void AddContent(RectTransform content)
        {
            content.SetParent(this.content);
        }

        /// <summary>
        /// 重新计算大小
        /// </summary>
        public void Resize()
        {
            // pos
            var pos = content.localPosition;
            pos.y = -foldedHeight;
            content.localPosition = pos;

            // size
            float height = foldedHeight + content.sizeDelta.y;
            var sz = GetComponent<RectTransform>().sizeDelta;
            sz.y = height;
            GetComponent<RectTransform>().sizeDelta = sz;
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Fold()
        {
            Vector2 sz = GetComponent<RectTransform>().sizeDelta;
            sz.y = foldedHeight;
            GetComponent<RectTransform>().sizeDelta = sz;

            content.gameObject.SetActive(false);

            transform.Find("Head/FoldButton/Unfold").gameObject.SetActive(false);
            transform.Find("Head/FoldButton/Folded").gameObject.SetActive(true);
        }

        public void Unfold()
        {
            //Vector2 sz = GetComponent<RectTransform>().sizeDelta;
            //sz.y = foldedHeight + content.sizeDelta.y;
            //GetComponent<RectTransform>().sizeDelta = sz;
            Resize();

            content.gameObject.SetActive(true);
            transform.Find("Head/FoldButton/Unfold").gameObject.SetActive(true);
            transform.Find("Head/FoldButton/Folded").gameObject.SetActive(false);
        }
    }
}

