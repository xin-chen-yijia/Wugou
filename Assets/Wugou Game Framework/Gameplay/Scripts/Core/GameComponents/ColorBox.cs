using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.Editor.UI;

namespace Wugou
{
    [DefaultGameComponentView("°üÎ§ºÐ")]
    [EntitySerializable]
    public class ColorBox : MonoBehaviour
    {
        [EditorProperty("ÑÕÉ«£º", nameof(SetBoxColor))]
        [EntitySerializeField]
        //public string color = "#00FF0077";
        public Color color = new Color(0, 1, 1, 77f / 255);

        private void SetBoxColor(string colorStr)
        {
            if (ColorUtility.TryParseHtmlString(colorStr, out Color c))
            {
                SetBoxColor(c);
            }
        }

        private void SetBoxColor(Color color)
        {
            var render = GetComponentInChildren<MeshRenderer>();
            render.material.color = color;
        }

        //// Start is called before the first frame update
        void Start()
        {
            SetBoxColor(color);
        }

        //// Update is called once per frame
        //void Update()
        //{

        //}
    }
}
