using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.Editor.UI
{
    public class BoolPropertyView : PropertyView
    {
        public Toggle toggle;

        bool value_ = false;

        public override object GetValue()
        {
            return value_;
        }

        public override void SetValue(object value)
        {
            SetValue((bool)value);
        }

        public void SetValue(bool value)
        {
            value_ = value;
            toggle.SetIsOnWithoutNotify(value);
        }

        public override void SetValueChangedCallback(Action<object> action)
        {
            toggle.onValueChanged.AddListener((val) =>
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
