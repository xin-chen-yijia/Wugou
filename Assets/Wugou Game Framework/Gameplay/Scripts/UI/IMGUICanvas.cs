using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.UI
{
    /// <summary>
    /// 用于分辨率自适应
    /// </summary>
    public class IMGUICanvas
    {
        private Rect rect_;
        public IMGUICanvas(Rect rect)
        {
            rect_ = rect;
        }

        public Rect AdjustRect(Rect rect)
        {
            var x = rect.x / rect_.width;
            var y = rect.y / rect_.height;
            var w = rect.width / rect_.width;
            var h = rect.height / rect_.height;

            return new Rect(x * Screen.width, y * Screen.height, w * Screen.width, h * Screen.height);
        }

        public void DrawTexture(Rect area, Texture2D texture)
        {
            GUI.DrawTexture(AdjustRect(area), texture);
        }

        public string DrawTextField(Rect area, string text, GUIStyle style)
        {
            style.fontSize = (int)(14 * Screen.width / rect_.width);
            return GUI.TextField(AdjustRect(area), text, style);
        }

        public bool DrawButton(Rect area, string text, GUIStyle style)
        {
            style.fontSize = (int)(14 * Screen.width / rect_.width);
            return GUI.Button(AdjustRect(area), text, style);
        }
    }
}
