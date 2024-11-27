using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Text;
using System.Linq;

namespace Wugou
{
    /// <summary>
    /// 窗口设置
    /// 1. 设置窗口大小；
    /// 2. 设置窗口样式
    /// </summary>
    public static class ScreenHelper
    {
#if UNITY_STANDALONE_WIN
        /// <summary>
        /// 窗口样式
        /// https://learn.microsoft.com/zh-cn/windows/win32/winmsg/window-styles?redirectedfrom=MSDN
        /// https://learn.microsoft.com/zh-cn/previous-versions/czada357(v=vs.120)
        /// WS_BORDER创建具有边框的窗口。
        /// WS_CAPTION 用于创建具有标题栏的窗口（即表示 WS_BORDER 样式）。 不能与 WS_DLGFRAME 样式一起使用。
        /// WS_DLGFRAME 用于创建没有标题的双边框窗口。
        /// WS_SIZEBOX	0x00040000L	窗口具有大小调整边框。 与 WS_THICKFRAME 样式相同。
        /// ......
        /// 
        /// https://learn.microsoft.com/zh-cn/previous-versions/visualstudio/visual-studio-2012/61fe4bte(v=vs.110)
        /// 扩展窗口样式
        /// WS_EX_TOOLWINDOW 创建一个工具窗口，是预期的窗口用作浮动工具栏。工具窗口具有使用较小的字体，比普通标题栏短的标题栏，并且，窗口标题绘制。工具窗口未显示在任务栏或于显示的窗口在用户按 ALT+TAB。
        /// ......
        /// </summary>
        public const long WS_BORDER = 0x00800000L;
        public const long WS_POPUP = 0x80000000L;
        public const long WS_DLGFRAME = 0x00400000L;
        public const long WS_MINIMIZEBOX = 0x00020000L;
        public const long WS_MAXIMIZEBOX = 0x00010000L;
        public const long WS_THICKFRAME = 0x00040000L;
        public const long WS_CHILD = 0x40000000L;
        public const long WS_CAPTION = 0x00C00000L;
        //public const long WS_CAPTION = WS_BORDER | WS_DLGFRAME;
        public const long WS_SYSMENU = 0x00080000L;
        public const long WS_SIZEBOX = 0x00040000L;
        public const long WS_EX_TOOLWINDOW = 0x00000080L;

        public const int SW_HIDE = 0;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_MAXIMIZE = 3;
        public const int SW_SHOW = 5;
        public const int SW_SHOWNA = 8;
        public const int SW_RESTORE = 9;
        const int GWL_STYLE = -16;
        const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr SetForegroundWindow(IntPtr hwd);

        [DllImport("user32.dll")]
        public static extern long GetWindowLong(IntPtr hwd, int nIndex);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll")]
        public static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr SetActiveWindow(IntPtr hwd);

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowLong(IntPtr hwnd, int _nIndex, long dwNewLong);

        private const string UnityWindowClassName = "UnityWndClass";

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);


        private static IntPtr _wndHandle = IntPtr.Zero;
        private static IntPtr wndHandle
        {
            get
            {
                if (_wndHandle == IntPtr.Zero)
                {
                    //_wndHandle = GetForegroundWindow();
                    uint threadId = GetCurrentThreadId();
                    EnumThreadWindows(threadId, (hWnd, lParam) =>
                    {
                        var classText = new StringBuilder(UnityWindowClassName.Length + 1);
                        GetClassName(hWnd, classText, classText.Capacity);
                        if (classText.ToString() == UnityWindowClassName)
                        {
                            _wndHandle = hWnd;
                            return false;
                        }
                        return true;
                    }, IntPtr.Zero);
                }
                return _wndHandle;
            }
        }

        /// <summary>
        /// 显示/隐藏标题栏
        /// 参考：https://blog.csdn.net/qq_39162826/article/details/119927311
        /// </summary>
        public static void SetTitleBarHide(bool isHide)
        {
#if !UNITY_EDITOR
            if (wndHandle == IntPtr.Zero)
            {
                Logger.Error("get window handle fail...");
                return;
            }

            // 隐藏标题栏
            var windowStyle = GetWindowLong(wndHandle, GWL_STYLE);
            if (isHide)
            {
                windowStyle &= ~(WS_CAPTION | WS_SIZEBOX);
            }
            else
            {
                windowStyle |= (WS_CAPTION | WS_SIZEBOX);
            }

            SetWindowLong(wndHandle, GWL_STYLE, windowStyle);
            Utils.DoAsync(async () =>
            {
                await new YieldInstructionAwaiter(new WaitForSeconds(0.1f));
                // 刷一下，不然Unity的UI会失效
                // 窗口可以无边框，但Unity的UI系统会失灵，需要手动切换一下窗口
                ShowWindow(wndHandle, SW_SHOWMINIMIZED);
                ShowWindow(wndHandle, SW_RESTORE);
            });
            //SetForegroundWindow(wndHandle);
#endif
        }
#else
        public static void SetTitleBarHide(bool isHide)
        {
            throw new NotImplementedException();
        }
#endif

        /// <summary>
        /// 全屏
        /// </summary>
        public static void SetFullScreen(bool fullScreen = true)
        {
#if !UNITY_EDITOR
            Resolution[] resolutions = Screen.resolutions;
            var maxRes = resolutions.OrderByDescending((r) => r.width).First();
            Screen.SetResolution(maxRes.width, maxRes.height, fullScreen);
            Screen.fullScreen = fullScreen;
#endif
        }

        /// <summary>
        /// 设置分辨率
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="fullscreen"></param>
        public static void SetResolution(int width, int height, bool fullscreen)
        {
#if !UNITY_EDITOR
            Screen.SetResolution(width, height, fullscreen);
#endif
        }
    }
}
