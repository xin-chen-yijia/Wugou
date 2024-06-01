using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.Editor
{
    public class BoolPropertyView : PropertyView
    {
        public Toggle toggle;
        public override void ParseFromString(string value)
        {
            value_ = bool.Parse(value);
        }

        public override void SetValue(object value)
        {
            value_ = value;
            toggle.SetIsOnWithoutNotify((bool)value);
        }

        public override void AddUpdateEvent(Action action)
        {
            toggle.onValueChanged.AddListener((val) =>
            {
                // undo
                var tmp = value_;
                value_ = val;

                action?.Invoke();

                var tmp2 = value_;
                using (var transaction = new GameMapEditor.TransactionScope())
                {
                    transaction.Record(new CommonObjectRecord(() =>
                    {
                        value_ = tmp2;
                        action?.Invoke();
                    }, () =>
                    {
                        value_ = tmp;
                        action?.Invoke();
                    }));
                }
            });
        }
    }
}
