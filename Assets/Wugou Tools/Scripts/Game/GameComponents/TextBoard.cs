using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.Editor;

namespace Wugou
{
    [System.Serializable]
    [DefaultGameComponentView]
    public class TextBoard : GameComponent
    {
        public TMPro.TMP_Text board;

        [CustomProperty("文本", callback: nameof(SetText))]
        [SerializeField]
        public string text = "";

        [CustomProperty("自动缩放")]
        [SerializeField]
        public bool autoScale;   // 自动缩放

        private void SetText(string text)
        {
            board.text = text;
        }

        // Start is called before the first frame update
        void Start()
        {
            SetText(text);
        }

        // Update is called once per frame
        void Update()
        {
            if (autoScale)
            {
                var cam = Camera.main;
                if (cam)
                {
                    var d = cam.transform.InverseTransformPoint(board.transform.position).z / 80;
                    board.GetComponent<RectTransform>().localScale = new Vector3(d, d, d);
                }
            }
        }
    }
}
