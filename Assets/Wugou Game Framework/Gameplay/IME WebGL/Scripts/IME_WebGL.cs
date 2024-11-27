using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 引用js函数
/// </summary>
public class IME_WebGL : MonoBehaviour {
#if UNITY_WEBGL && !UNITY_EDITOR

    [DllImport("__Internal")]
	public static extern void InputShow (string v, int selectionStart, int selectionEnd);

    [DllImport("__Internal")]
	public static extern void Inputing ();

    [DllImport("__Internal")]
	public static extern void InputEnd ();

    private System.Func<string> getter_ = null;
    private System.Action<string> setter_ = null;

    public void SetInputFiled(System.Func<string> getter,System.Action<string> setter, int selectionStart, int selectionEnd)
    {
        getter_ = getter;
        setter_ = setter;

        WebGLInput.captureAllKeyboardInput = false;
        IME_WebGL.InputShow(getter_?.Invoke(), selectionStart, selectionEnd);
    }

    public void OnReceiveMessage(string message)
    {
        setter_?.Invoke(message);
    }

    public void OnInputEnd()
    {
        WebGLInput.captureAllKeyboardInput = true;
    }

#else
    public static void InputShow(string v, int selectionStart, int selectionEnd) { }
    public static void InputEnd() { }

    public void SetInputFiled(System.Func<string> getter, System.Action<string> setter, int selectionStart, int selectionEnd) { }
#endif

    private static IME_WebGL instance_ = null;
    public static IME_WebGL Instance { 
        get 
        {
            if (instance_ == null)
            {
                var go = new GameObject("IME_WEBGL_RECEIVER");
                DontDestroyOnLoad(go);

                instance_ = go.AddComponent<IME_WebGL>();
            }
            return instance_;  
        } 
    }



}
