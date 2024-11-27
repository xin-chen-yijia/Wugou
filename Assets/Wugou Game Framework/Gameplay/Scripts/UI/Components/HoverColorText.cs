using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Wugou.UI
{
    /// <summary>
    /// 文字悬浮也变色
    /// </summary>
    public class HoverColorText : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        public Color hoverColor;

        private Color cachedColor_;
        public void OnPointerEnter(PointerEventData eventData)
        {
            cachedColor_ = GetComponent<TMP_Text>().color;
            GetComponent<TMP_Text>().color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            GetComponent<TMP_Text>().color = cachedColor_;
        }
    }
}
