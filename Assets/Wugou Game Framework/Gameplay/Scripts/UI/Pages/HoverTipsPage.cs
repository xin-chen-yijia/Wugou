using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Wugou.UI
{
    /// <summary>
    /// 用于鼠标悬停文本显示
    /// </summary>
    public class HoverTipsPage : UIBaseWindow
    {
        public TMP_Text tipsText;

        //// Start is called before the first frame update
        //void Start()
        //{

        //}

        //// Update is called once per frame
        //void Update()
        //{

        //}

        public void Show(string text)
        {
            tipsText.text = text;
            // 自适应长度
            var size = GetComponent<RectTransform>().sizeDelta;
            size.x = Mathf.Max(160, text.Length / 2 * 18 + 16);
            GetComponent<RectTransform>().sizeDelta = size;
            Show();
        }
    }
}
