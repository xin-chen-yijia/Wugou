using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.Editor;

namespace Wugou
{
    [DefaultGameComponentView]
    [System.Serializable]
    public class ColorBox : MonoBehaviour
    {
        [CustomProperty("ÑÕÉ«£º", nameof(SetBoxColor))]
        [SerializeField]
        public string color = "#00FF0077";

        private void SetBoxColor(string colorStr)
        {
            Color c = new Color(0, 1.0f, 0.0f, 0.5f);
            if (ColorUtility.TryParseHtmlString(colorStr, out c))
            {
                var render = GetComponentInChildren<MeshRenderer>();
                render.material.color = c;
            }
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
