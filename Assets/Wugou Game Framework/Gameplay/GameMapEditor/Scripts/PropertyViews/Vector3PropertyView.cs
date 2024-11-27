using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace Wugou.Editor.UI
{

    public class Vector3PropertyView : PropertyView
    {
        Vector3 value_;

        public TMP_InputField xInput;
        public TMP_InputField yInput;
        public TMP_InputField zInput;

        public override object GetValue()
        {
            return value_;
        }

        public override void SetValue(object value)
        {
            SetValue((Vector3)value);
        }

        private string FloatToString(float value)
        {
            return value.ToString("0.####");
        }

        public void SetValue(Vector3 value)
        {
            value_ = value;
            if (!xInput.isFocused)
            {
                xInput.SetTextWithoutNotify(FloatToString(value_.x));
            }
            if (!yInput.isFocused)
            {
                yInput.SetTextWithoutNotify(FloatToString(value_.y));
            }
            if (!zInput.isFocused)
            {
                zInput.SetTextWithoutNotify(FloatToString(value_.z));
            }
        }

        void UndoRecord(Vector3 begin, Vector3 end, Action<object> action)
        {
            using (var transaction = new GameMapEditor.TransactionScope())
            {
                transaction.Record(new CommonObjectRecord(() =>
                {
                    action(end);
                },
                () =>
                {
                    action(begin);
                }));
            }
        }

        public void SetInteractable(bool able)
        {
            if (able)
            {
                xInput.interactable = true;
                yInput.interactable = true;
                zInput.interactable = true;
                xInput.ActivateInputField();
                yInput.ActivateInputField();
                zInput.ActivateInputField();
            }
            else
            {
                xInput.interactable = false;
                yInput.interactable = false;
                zInput.interactable = false;
                xInput.DeactivateInputField();
                yInput.DeactivateInputField();
                zInput.DeactivateInputField();
            }
        }

        public override void SetValueChangedCallback(Action<object> action)
        {
            // do nothing
            UnityAction<string> handXInput = (val) =>
            {
                //
                float v = value_.x;
                if (float.TryParse(val, out v))
                {
                    var tmp = value_;

                    value_.x = v;
                    action.Invoke(value_);

                    var tmp2 = value_;

                    UndoRecord(tmp, tmp2, action);
                }

                xInput.SetTextWithoutNotify(FloatToString(value_.x));
            };
            UnityAction<string> handYInput = (val) =>
            {
                //
                float v = value_.y;
                if (float.TryParse(val, out v))
                {
                    var tmp = value_;

                    value_.y = v;
                    action.Invoke(value_);

                    var tmp2 = value_;

                    UndoRecord(tmp, tmp2, action);
                }

                yInput.SetTextWithoutNotify(FloatToString(value_.y));
            };
            UnityAction<string> handZInput = (val) =>
            {
                //
                float v = value_.z;
                if (float.TryParse(val, out v))
                {
                    var tmp = value_;

                    value_.z = v;
                    action.Invoke(value_);

                    var tmp2 = value_;

                    UndoRecord(tmp, tmp2, action);
                }

                zInput.SetTextWithoutNotify(FloatToString(value_.z));
            };

            xInput.onSubmit.RemoveAllListeners();
            xInput.onSubmit.AddListener(handXInput);

            yInput.onSubmit.RemoveAllListeners();
            yInput.onSubmit.AddListener(handYInput);

            zInput.onSubmit.RemoveAllListeners();
            zInput.onSubmit.AddListener(handZInput);

            xInput.onDeselect.RemoveAllListeners();
            xInput.onDeselect.AddListener(handXInput);

            yInput.onDeselect.RemoveAllListeners();
            yInput.onDeselect.AddListener(handYInput);

            zInput.onDeselect.RemoveAllListeners();
            zInput.onDeselect.AddListener(handZInput);
        }
    }
}
