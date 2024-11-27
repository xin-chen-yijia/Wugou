using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.Editor.UI
{
    /// <summary>
    /// 枚举类型
    /// </summary>
    public class EnumPropertyView : PropertyView
    {
        public TMPro.TMP_Dropdown dropdown;
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
            dropdown.SetValueWithoutNotify(value_);
        }

        public override void SetValueChangedCallback(Action<object> action)
        {
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener((val) =>
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

        public override void OnCustomUI(Type propertyType, object param)
        {
            var items = param as string[];
            if (items != null)
            {
                var options = new List<TMPro.TMP_Dropdown.OptionData>();
                foreach(var v in items)
                {
                    options.Add(new TMPro.TMP_Dropdown.OptionData(v));
                }

                dropdown.options = options;
            }
            else
            {
                var options = new List<TMPro.TMP_Dropdown.OptionData>();
                foreach (var v in Enum.GetNames(propertyType))
                {
                    options.Add(new TMPro.TMP_Dropdown.OptionData(v));
                }

                dropdown.options = options;
            }
        }
    }
}
