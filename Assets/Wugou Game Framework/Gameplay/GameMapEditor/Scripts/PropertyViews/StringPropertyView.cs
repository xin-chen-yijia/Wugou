using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.Editor.UI
{
    public class StringPropertyView : PropertyView
    {
        public TMPro.TMP_InputField inputField;
        string value_;

        public override object GetValue()
        {
            return value_;
        }

        public override void SetValue(object value)
        {
            SetValue((string)value);
        }

        public void SetValue(string value)
        {
            value_ = value;
            inputField.SetTextWithoutNotify(value);
        }

        public override void SetValueChangedCallback(Action<object> action)
        {
            inputField.onValueChanged.RemoveAllListeners();
            inputField.onValueChanged.AddListener((val) =>
            {
                // undo
                var tmp = value_;

                value_ = val;
                action?.Invoke(val);

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
            });
        }
    }
}
