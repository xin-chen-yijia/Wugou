using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.UI
{
    /// <summary>
    /// 绘制渐变色
    /// </summary>
    public class ColoredTape : MaskableGraphic
    {
        Vector2 _rectSize;

        [SerializeField]
        public bool Outline = false;
        [SerializeField]
        public float OuelineWidth = 1.0f;
        [SerializeField]
        public Color OutlineColor = Color.black;

        public enum E_DrawDirection
        {
            Horizontal = 0,
            Vertical
        }

        public E_DrawDirection tapeDirection = E_DrawDirection.Vertical; 

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            //调用父类GetPixelAdjusteRect方法获取组件尺寸，如果是从RectTransform直接获取将不具备自适应的功能，因为调整锚点，组件尺寸会改变，从而不能达到自适应效果
            _rectSize = GetPixelAdjustedRect().size;
            //清除顶点流数据，如果不操作这一步会导致自己编写的组件在绘制时出现bug，因为上一个组件的顶点流没有清除掉，遗留到当前组件，当前组件就会多对一部分的顶点进行绘制导致bug出现
            vh.Clear();
            //绘制方向分为两种垂直绘制和水平绘制，如果不这样做，可直接旋转图片
            if (tapeDirection == E_DrawDirection.Vertical)
            {
                DrawVerticalColoredTape(vh);
            }
            else
            {
                DrawHorizontalColoredTape(vh);
            }

            //绘制外边框
            if (Outline)
            {
                DrawOutline(vh);
            }
        }

        [SerializeField]
        private List<Color> m_Colors = new List<Color>() {Color.red,Color.magenta};
        private void DrawVerticalColoredTape(VertexHelper vh)
        {
            Vector2 pivot = GetComponent<RectTransform>().pivot;

            int colorNumber = m_Colors.Count;
            //原理图竖直方向一格的长度就是offset
            float offset = _rectSize.y / (colorNumber - 1);
            //获取四个第一个参加绘制的矩形的四个顶点
            Vector2 topLeftPos = new Vector2(-pivot.x *_rectSize.x, (1.0f - pivot.y) * _rectSize.y);
            Vector2 topRightPos = new Vector2((1.0f - pivot.x) * _rectSize.x, (1.0f - pivot.y) * _rectSize.y);
            Vector2 bottomLeftPos = topLeftPos - new Vector2(0, offset);
            Vector2 bottomRightPos = topRightPos - new Vector2(0, offset);
            for (int i = 0; i < colorNumber - 1; i++)
            {
                Color startColor = m_Colors[i];
                Color endColor = m_Colors[i + 1];
                var first = GetUIVertex(topLeftPos, startColor);
                var second = GetUIVertex(topRightPos, startColor);
                var third = GetUIVertex(bottomRightPos, endColor);
                var four = GetUIVertex(bottomLeftPos, endColor);
                //传入四个顶点进行绘制
                vh.AddUIVertexQuad(new UIVertex[] { first, second, third, four });
                //然后向下迭代顶点
                topLeftPos = bottomLeftPos;
                topRightPos = bottomRightPos;
                bottomLeftPos = topLeftPos - new Vector2(0, offset);
                bottomRightPos = topRightPos - new Vector2(0, offset);
            }
        }

        private void DrawHorizontalColoredTape(VertexHelper vh)
        {
            Vector2 pivot = GetComponent<RectTransform>().pivot;

            int colorNumber = m_Colors.Count;
            //原理图横向方向一格的长度就是offset
            float offset = _rectSize.x / (colorNumber - 1);
            //获取四个第一个参加绘制的矩形的四个顶点
            Vector2 topLeftPos = new Vector2(-pivot.x * _rectSize.x, (1.0f - pivot.y) * _rectSize.y);
            Vector2 bottomLeftPos = new Vector2(-pivot.x * _rectSize.x, (-pivot.y) * _rectSize.y);
            Vector2 topRightPos = topLeftPos + new Vector2(offset, 0);
            Vector2 bottomRightPos = bottomLeftPos + new Vector2(offset, 0);
            for (int i = 0; i < colorNumber - 1; i++)
            {
                Color startColor = m_Colors[i];
                Color endColor = m_Colors[i + 1];
                var first = GetUIVertex(topLeftPos, startColor);
                var second = GetUIVertex(topRightPos, endColor);
                var third = GetUIVertex(bottomRightPos, endColor);
                var four = GetUIVertex(bottomLeftPos, startColor);
                //传入四个顶点进行绘制
                vh.AddUIVertexQuad(new UIVertex[] { first, second, third, four });
                //然后向右迭代顶点
                topLeftPos = topRightPos;;
                bottomLeftPos = bottomRightPos;
                topRightPos = topLeftPos + new Vector2(offset, 0);
                bottomRightPos = bottomLeftPos + new Vector2(offset, 0);
            }
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
            vh.AddUIVertexQuad(GetQuad(a, b, OutlineColor, OuelineWidth));
            vh.AddUIVertexQuad(GetQuad(c, d, OutlineColor, OuelineWidth));
            vh.AddUIVertexQuad(GetQuad(e, f, OutlineColor, OuelineWidth));
            vh.AddUIVertexQuad(GetQuad(g, h, OutlineColor, OuelineWidth));
        }

        #region Helper methods
        public virtual void SetColors(Color[] colors)
        {
            m_Colors.Clear();
            foreach (Color color1 in colors)
                m_Colors.Add(color1);
            OnEnable();
        }

        public void SetColors(float value, ColorPicker.E_PaletteMode mode)
        {
            List<Color> colors = new List<Color>();
            switch (mode)
            {
                case ColorPicker.E_PaletteMode.Red:
                    foreach (Color mColor in m_Colors)
                        colors.Add(new Color(value, mColor.g, mColor.b, mColor.a));
                    break;
                case ColorPicker.E_PaletteMode.Green:
                    foreach (Color mColor in m_Colors)
                        colors.Add(new Color(mColor.r, value, mColor.b, mColor.a));
                    break;
                case ColorPicker.E_PaletteMode.Blue:
                    foreach (Color mColor in m_Colors)
                        colors.Add(new Color(mColor.r, mColor.g, value, mColor.a));
                    break;
            }
            m_Colors.Clear();
            m_Colors = colors;
            Rebuild();
        }

        public virtual Color GetColor(Vector2 position)
        {
            int colorCount = m_Colors.Count;
            switch (tapeDirection)
            {
                case E_DrawDirection.Horizontal:
                    var perX = _rectSize.x / ((colorCount - 1) * 2);
                    var doubelPer = perX * 2;
                    var lenght0 = position.x + _rectSize.x / 2.0f;
                    int index0 = (int)(lenght0 / doubelPer);
                    var temp0 = lenght0 % doubelPer / doubelPer;
                    if (lenght0.Equals(_rectSize.x))
                    {
                        index0--;
                        temp0++;
                    }
                    Color start = m_Colors[index0];
                    Color end = m_Colors[index0 + 1];
                    return start * (1 - temp0) + end * temp0;
                case E_DrawDirection.Vertical:
                    var perY = _rectSize.y / ((colorCount - 1) * 2);
                    var doublePer = perY * 2;
                    var lenght1 = _rectSize.y / 2.0f - position.y;
                    var index1 = (int)(lenght1 / doublePer);
                    var temp1 = lenght1 % doublePer / doublePer;
                    if (lenght1.Equals(_rectSize.y))
                    {
                        index1--;
                        temp1++;
                    }
                    Color start1 = m_Colors[index1];
                    Color end1 = m_Colors[index1 + 1];
                    return start1 * (1 - temp1) + end1 * temp1;
                default:
                    return Color.white;
            }
        }
        public Color GetColor(int index)
        {
            return m_Colors[index];
        }

        public virtual Vector2 GetPosition(Color color0)
        {
            var red = color0.r;
            var green = color0.g;
            var blue = color0.b;
            var color1 = color0;
            var color2 = color0;
            var offset = 0.0f;
            ArrayList array = new ArrayList() { red, green, blue };
            array.Sort();

            if (array[2].Equals(red))
                color1 = Color.red;
            else if (array[2].Equals(green))
                color1 = Color.green;
            else if (array[2].Equals(blue))
                color1 = Color.blue;


            if (array[1].Equals(red))
            {
                color2 = Color.red;
                offset = color0.r;
            }
            if (array[1].Equals(green))
            {
                color2 = Color.green;
                offset = color0.g;
            }
            if (array[1].Equals(blue))
            {
                color2 = Color.blue;
                offset = color0.b;
            }
            var pos1 = returnIndex(color1);
            var pos2 = returnIndex(color2);
            if (color1 == Color.red && color2 == Color.green)
                pos1 = m_Colors.Count - 1;
            if (color2 == Color.red && color1 == Color.green)
                pos2 = m_Colors.Count - 1;
            switch (tapeDirection)
            {
                case E_DrawDirection.Vertical:
                    var position1 = new Vector2(0, _rectSize.y / 2.0f - pos1 * _rectSize.y / (m_Colors.Count - 1));
                    var position2 = new Vector2(0, _rectSize.y / 2.0f - pos2 * _rectSize.y / (m_Colors.Count - 1));
                    int sign1 = 1;
                    if (position1.y > position2.y)
                        sign1 = -1;
                    else
                        sign1 = 1;
                    return position1 + new Vector2(0, (_rectSize.y / (m_Colors.Count - 1)) * offset) * sign1;
                case E_DrawDirection.Horizontal:
                    var hp1 = new Vector2(-_rectSize.x / 2.0f + pos1 * _rectSize.x / (m_Colors.Count - 1), 0);
                    var hp2 = new Vector2(-_rectSize.x / 2.0f + pos2 * _rectSize.x / (m_Colors.Count - 1), 0);
                    int sign2 = 1;
                    if (hp1.x > hp2.x)
                        sign2 = -1;
                    else
                        sign2 = 1;
                    return hp1 + new Vector2(_rectSize.x / (m_Colors.Count - 1) * offset * sign2, 0);
            }
            return Vector2.zero;
        }
        public virtual float GetScale(Color color0)
        {
            Vector2 pos = GetPosition(color0);
            switch (tapeDirection)
            {
                case E_DrawDirection.Vertical:
                    return Mathf.Abs((_rectSize.y / 2.0f - pos.y) / _rectSize.y);
                case E_DrawDirection.Horizontal:
                    return Mathf.Abs((pos.x - _rectSize.x / 2.0f) / _rectSize.x);
            }
            return 0;
        }
        private int returnIndex(Color color0)
        {
            for (int i = 0; i < m_Colors.Count; i++)
            {
                if (m_Colors[i].Equals(color0))
                    return i;
            }
            return 0;
        }

        public virtual void Rebuild()
        {
            OnEnable();
        }
        public UIVertex GetUIVertex(Vector2 point, Color color0)
        {
            UIVertex vertex = new UIVertex
            {
                position = point,
                color = color0,
            };
            return vertex;
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

        #endregion
    }
}
