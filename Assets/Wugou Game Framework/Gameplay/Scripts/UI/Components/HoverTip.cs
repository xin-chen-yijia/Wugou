using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Wugou.UI
{
    /// <summary>
    /// 悬停文本提示
    /// </summary>
    public class HoverTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        public string tip;
        // Start is called before the first frame update
        //void Start()
        //{

        //}

        //// Update is called once per frame
        //void Update()
        //{

        //}

        private void OnDisable()
        {
            if (DaemonUI.isInitialized)
            {
                isTipShow_ = false;
                DaemonUI.hoverTipsPage.Hide();
            }
        }

        private bool isTipShow_ = false;
        public async void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(tip))
            {
                return;
            }

            isTipShow_ = true;
            await new YieldInstructionAwaiter(new WaitForSeconds(1f));

            if (isTipShow_)
            {
                DaemonUI.hoverTipsPage.Show(tip);
            }
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            var page = DaemonUI.hoverTipsPage;
            var pageTrans = page.transform as RectTransform;
            var parentTrans = pageTrans.parent as RectTransform;

            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(page.transform.parent as RectTransform, eventData.position, eventData.enterEventCamera, out pos);

            // 注意，这里pos对应的是localPosition，是父物体的坐标系而非UI的锚点坐标系
            pos.x = Mathf.Max(-(parentTrans.sizeDelta.x * 0.5f - pageTrans.sizeDelta.x), pos.x);
            pos.y = Mathf.Min((parentTrans.sizeDelta.y * 0.5f - pageTrans.sizeDelta.y), pos.y);
            page.transform.localPosition = pos;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isTipShow_ = false;
            DaemonUI.hoverTipsPage.Hide();
        }
    }
}
