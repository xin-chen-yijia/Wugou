using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.Editor.UI;

namespace Wugou
{
    [EntitySerializable]
    [DefaultGameComponentView("文字面板")]
    public class TextBoard : GameComponent
    {
        public TMPro.TMP_Text board;

        [EditorProperty("文本", callback: nameof(SetText))]
        [EntitySerializeField]
        public string text = "";

        [EditorProperty("自动缩放")]
        [EntitySerializeField]
        public bool autoScale;   // 自动缩放

        private bool _isBillboard = true;

        [EditorProperty("朝向摄像机")]
        [EntitySerializeField]
        public bool isBillboard
        {
            get
            {
                return _isBillboard;
            }

            set
            {
                _isBillboard = value;
                GetComponentInChildren<Billboard>().enabled = value;
                if (!value)
                {
                    var rig = transform.GetChild(0);
                    rig.transform.localPosition = Vector3.zero;
                    rig.transform.localRotation = Quaternion.identity;
                }
            }
        }

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
