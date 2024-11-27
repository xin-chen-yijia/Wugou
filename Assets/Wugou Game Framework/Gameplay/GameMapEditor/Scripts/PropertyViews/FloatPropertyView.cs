using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Wugou.Editor.UI
{
    /// <summary>
    /// float数值，使用文本框输入
    /// </summary>
    public class FloatPropertyView : PropertyView
    {
        public TMPro.TMP_InputField inputField;
        float value_;

        public override object GetValue()
        {
            return value_;
        }

        public override void SetValue(object value)
        {
            SetValue((float)value);
        }

        public void SetValue(float value)
        {
            value_ = value;
            inputField.SetTextWithoutNotify(value_.ToString("F4"));
        }

        public override void SetValueChangedCallback(Action<object> action)
        {
            inputField.onValueChanged.RemoveAllListeners();
            inputField.onValueChanged.AddListener((val) =>
            {
                // undo
                var tmp = value_;

                if (float.TryParse(val, out float t))
                {
                    value_ = t;
                    action?.Invoke(t);

                    var tmp2 = value_;
                    using (var transaction = new GameMapEditor.TransactionScope())
                    {
                        transaction.Record(new CommonObjectRecord(() =>
                        {
                            value_ = tmp2;
                            action?.Invoke(tmp2);
                        }, () =>
                        {
                            value_ = tmp;
                            action?.Invoke(tmp);
                        }));
                    }
                }

            });
        }
    }
}
