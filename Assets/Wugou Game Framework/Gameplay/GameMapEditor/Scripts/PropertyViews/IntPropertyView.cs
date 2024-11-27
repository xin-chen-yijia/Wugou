using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.Editor.UI
{
    public class IntPropertyView : PropertyView
    {
        public TMPro.TMP_InputField inputField;
        int value_;

        public override object GetValue()
        {
            return value_;
        }

        public override void SetValue(object value)
        {
            SetValue((int)value);
        }

        /// <summary>
        /// 为某些使用提供更高效的版本
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(int value)
        {
            value_ = value;
            inputField.SetTextWithoutNotify(value_.ToString());
        }

        public override void SetValueChangedCallback(Action<object> action)
        {
            inputField.onValueChanged.RemoveAllListeners();
            inputField.onValueChanged.AddListener((val) =>
            {
                // undo
                var tmp = value_;

                if(int.TryParse(val, out int t))
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
