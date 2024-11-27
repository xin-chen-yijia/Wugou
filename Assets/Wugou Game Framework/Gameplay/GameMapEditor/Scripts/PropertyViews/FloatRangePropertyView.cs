using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.Editor.UI
{
    /// <summary>
    /// 使用滑杆更新float类型数值
    /// </summary>
    public class FloatRangePropertyView : PropertyView
    {
        public Slider slider;
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
            slider.SetValueWithoutNotify(value);
        }

        public override void SetValueChangedCallback(Action<object> action)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener((val) =>
            {
                action?.Invoke(val);
            });
        }

        public override void OnCustomUI(Type propertyType, object param)
        {
            var range = param as float[];
            Debug.Assert(range.Length >= 2 && range[0] < range[1]);
            if (range != null)
            {
                // 为了避免设置minValue时触发OnValueChanged事件，因为此时属性界面的源对象还未设置
                var tmp = slider.onValueChanged;
                slider.onValueChanged = new Slider.SliderEvent();

                slider.minValue = range[0];
                slider.maxValue = range[1];

                slider.onValueChanged = tmp;
            }
        }
    }
}
