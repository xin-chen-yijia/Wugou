using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wugou.UI;

namespace Wugou.Editor.UI
{
    public class ColorPropertyView : PropertyView
    {
        public Button button;

        Color value_;

        public override object GetValue()
        {
            return value_;
        }

        public override void SetValue(object value)
        {
            SetValue((Color)value);
        }

        /// <summary>
        /// 为某些使用提供更高效的版本
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(Color value)
        {
            value_ = value;
            button.GetComponent<Image>().color = value;
        }

        public override void SetValueChangedCallback(Action<object> action)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                var palette = UIRootWindow.GetOrAddWindow<Palette>();
                palette.SetCurColor(value_);
                palette.onColorChanged.RemoveAllListeners();
                palette.onColorChanged.AddListener((color) =>
                {
                    //var tmp = value_;
                    action?.Invoke(color);
                    SetValue(color);

                    //var tmp2 = color;
                    //using (var transaction = new GameMapEditor.TransactionScope())
                    //{
                    //    transaction.Record(new CommonObjectRecord(() =>
                    //    {
                    //        value_ = tmp2;
                    //        action?.Invoke(tmp2);
                    //    }, () =>
                    //    {
                    //        value_ = tmp;
                    //        action?.Invoke(tmp);
                    //    }));
                    //}
                });
                palette.Show();
            });
        }
    }
}
