using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Wugou.Editor
{
    using Text = TMPro.TMP_Text;
    using InputField = TMPro.TMP_InputField;

    public class Vector3PropertyView : PropertyView
    {
        private Vector3 vec3_;
        public Vector3 vec3Val => vec3_;

        private InputField xInput => transform.Find("Content/XInput").GetComponent<InputField>();
        private InputField yInput => transform.Find("Content/YInput").GetComponent<InputField>();
        private InputField zInput => transform.Find("Content/ZInput").GetComponent<InputField>();
        public override void SetValue(object value)
        {
            value_ = value;
            vec3_ = (Vector3)value;
            if (!xInput.isFocused)
            {
                xInput.SetTextWithoutNotify(vec3_.x.ToString("F4"));
            }
            if(!yInput.isFocused)
            {
                yInput.SetTextWithoutNotify(vec3_.y.ToString("F4"));
            }
            if (!zInput.isFocused)
            {
                zInput.SetTextWithoutNotify(vec3_.z.ToString("F4"));
            }
        }

        void UndoRecord(Vector3 begin, Vector3 end)
        {
            using (var transaction = new GameMapEditor.TransactionScope())
            {
                transaction.Record(new CommonObjectRecord(() =>
                {
                    updateEvent_.Invoke(end);
                },
                () =>
                {
                    updateEvent_.Invoke(begin);
                }));
            }
        }

        protected void Start()
        {


            UnityAction<string> handXInput = (val) =>
            {
                //
                float v = vec3_.x;
                if (float.TryParse(val, out v))
                {
                    var tmp = vec3_;

                    vec3_.x = v;
                    updateEvent_.Invoke(vec3_);

                    var tmp2 = vec3_;

                    UndoRecord(tmp, tmp2);

                }
                else
                {
                    xInput.SetTextWithoutNotify(vec3_.x.ToString("F4"));
                }
            };
            UnityAction<string> handYInput = (val) =>
            {
                //
                float v = vec3_.y;
                if (float.TryParse(val, out v))
                {
                    var tmp = vec3_;

                    vec3_.y = v;
                    updateEvent_.Invoke(vec3_);

                    var tmp2 = vec3_;

                    UndoRecord(tmp, tmp2);
                }
                else
                {
                    yInput.SetTextWithoutNotify(vec3_.y.ToString("F4"));
                }
            };
            UnityAction<string> handZInput = (val) =>
            {
                //
                float v = vec3_.z;
                if (float.TryParse(val, out v))
                {
                    var tmp = vec3_;

                    vec3_.z = v;
                    updateEvent_.Invoke(vec3_);

                    var tmp2 = vec3_;

                    UndoRecord(tmp, tmp2);
                }
                else
                {
                    zInput.SetTextWithoutNotify(vec3_.z.ToString("F4"));
                }
            };

            xInput.onSubmit.AddListener(handXInput);
            yInput.onSubmit.AddListener(handYInput);
            zInput.onSubmit.AddListener(handZInput);

            xInput.onDeselect.AddListener(handXInput);
            yInput.onDeselect.AddListener(handYInput);
            zInput.onDeselect.AddListener(handZInput);
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

        Action<Vector3> updateEvent_ = null;
        public void AddUpdateEvent(Action<Vector3> action)
        {
            updateEvent_ = action;
        }
    }
}
