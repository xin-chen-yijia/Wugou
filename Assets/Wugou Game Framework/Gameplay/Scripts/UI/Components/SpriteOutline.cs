using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.UI
{
    /// <summary>
    /// 给UI画一个外框
    /// </summary>
    public class SpriteOutline : MaskableGraphic
    {
        Vector2 _rectSize;

        [SerializeField]
        public float OuelineWidth = 1.0f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            //调用父类GetPixelAdjusteRect方法获取组件尺寸，如果是从RectTransform直接获取将不具备自适应的功能，因为调整锚点，组件尺寸会改变，从而不能达到自适应效果
            _rectSize = GetPixelAdjustedRect().size;

            vh.Clear(); //很关键
            //绘制外边框
            DrawOutline(vh);
        }

        private void DrawOutline(VertexHelper vh)
        {
            Vector2 pivot = GetComponent<RectTransform>().pivot;

            var topLeft = new Vector2(-pivot.x * _rectSize.x, (1.0f - pivot.y) * _rectSize.y);
            var a = topLeft + new Vector2(-OuelineWidth / 2.0f, OuelineWidth);
            var b = a - new Vector2(0, _rectSize.y + OuelineWidth * 2);
            var c = a + new Vector2(_rectSize.x + OuelineWidth, 0);
            var d = c - new Vector2(0, _rectSize.y + OuelineWidth * 2);
            var e = topLeft + new Vector2(0, OuelineWidth / 2.0f);
            var f = e + new Vector2(_rectSize.x, 0);
            var g = e - new Vector2(0, _rectSize.y + OuelineWidth);
            var h = f - new Vector2(0, _rectSize.y + OuelineWidth);
            vh.AddUIVertexQuad(GetQuad(a, b, color, OuelineWidth));
            vh.AddUIVertexQuad(GetQuad(c, d, color, OuelineWidth));
            vh.AddUIVertexQuad(GetQuad(e, f, color, OuelineWidth));
            vh.AddUIVertexQuad(GetQuad(g, h, color, OuelineWidth));
        }

        public UIVertex[] GetQuad(Vector2 startPos, Vector2 endPos, Color color0, float LineWidth = 2.0f)
        {
            float dis = Vector2.Distance(startPos, endPos);
            float y = LineWidth * 0.5f * (endPos.x - startPos.x) / dis;
            float x = LineWidth * 0.5f * (endPos.y - startPos.y) / dis;
            if (y <= 0)
                y = -y;
            else
                x = -x;
            UIVertex[] vertex = new UIVertex[4];
            vertex[0].position = new Vector3(startPos.x + x, startPos.y + y);
            vertex[1].position = new Vector3(endPos.x + x, endPos.y + y);
            vertex[2].position = new Vector3(endPos.x - x, endPos.y - y);
            vertex[3].position = new Vector3(startPos.x - x, startPos.y - y);
            for (int i = 0; i < vertex.Length; i++)
                vertex[i].color = color0;
            return vertex;
        }
    }
}

