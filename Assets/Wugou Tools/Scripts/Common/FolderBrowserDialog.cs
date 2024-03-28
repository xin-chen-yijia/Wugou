using Ookii.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Wugou
{
    /// <summary>
    /// 用于打开windows的浏览器浏览文件夹
    /// </summary>
    public static class FolderBrowserDialog
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        public class WindowWrapper : IWin32Window
        {
            private IntPtr _hwnd;
            public WindowWrapper(IntPtr handle) { _hwnd = handle; }
            public IntPtr Handle { get { return _hwnd; } }
        }

        /// <summary>
        /// 浏览文件夹
        /// </summary>
        /// <param name="path"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static string Open(string path, string description)
        {
            var fd = new VistaFolderBrowserDialog();
            fd.Description = description;
            fd.SelectedPath = path;

            var res = fd.ShowDialog(new WindowWrapper(GetActiveWindow()));
            if (res == DialogResult.OK)
            {
                fd.Dispose();

                return fd.SelectedPath;
            }

            return "";
        }
    }
}

