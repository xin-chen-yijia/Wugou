using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 实现了中文输入，但光标的移动暂不支持
/// </summary>
[RequireComponent(typeof(InputField))]
public class IME_WebGLInputFiled : MonoBehaviour {

#if UNITY_WEBGL && !UNITY_EDITOR  //只在WebGl下生效

    private InputField inputField = null;
    void Start () {
        inputField = GetComponent<InputField>();

        //添加unity输入框回调
        inputField.onValueChanged.AddListener(OnValueChanged);

        //添加获得焦点回调
        EventTrigger trigger = inputField.gameObject.GetComponent<EventTrigger>();
        if (null == trigger)
            trigger = inputField.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry e = new EventTrigger.Entry();
        e.eventID = EventTriggerType.PointerUp;
        e.callback.AddListener((data) => { OnFocus((PointerEventData)data); });
        trigger.triggers.Add(e);
    }

    private void OnValueChanged(string arg0)
    {
        //暂时没用
    }

    private void OnFocus(PointerEventData pointerEventData)
    {
        IME_WebGL.Instance.SetInputFiled(() =>
        {
            return inputField.text;
        },
        (val) =>
        {
            inputField.text = val;
            inputField.caretPosition = inputField.text.Length;
        }, inputField.selectionFocusPosition, inputField.selectionAnchorPosition);
    }

#endif
}
