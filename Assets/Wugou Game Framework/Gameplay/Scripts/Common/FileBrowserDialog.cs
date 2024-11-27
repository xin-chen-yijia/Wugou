using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Windows.Forms;
using Ookii.Dialogs;

namespace Wugou
{
    public static class FileBrowserDialog
    {
        public static string Open(string path, string filter)
        {
            var fd = new VistaOpenFileDialog();
            fd.Filter = filter;
            //fd.Filter = "Supported Key Files (*.snk, *.pfx)|*.snk;*.pfx|All Files (*.*)|*.*"; // "Supported Key Files (*.png, *.jpg, *.jpeg)|*.png;*.jpg;*.jpeg"

            var res = fd.ShowDialog();
            if (res == DialogResult.OK)
            {
                fd.Dispose();

                return fd.FileName;
            }

            return "";
        }

        //public string[] GetFileOpenPath(string title, string filter)
        //{
        //    if (VistaOpenFileDialog.IsVistaFileDialogSupported)
        //    {
        //        VistaOpenFileDialog openFileDialog = new VistaOpenFileDialog();
        //        openFileDialog.Title = title;
        //        openFileDialog.CheckFileExists = true;
        //        openFileDialog.RestoreDirectory = true;
        //        openFileDialog.Multiselect = true;
        //        openFileDialog.Filter = filter;

        //        if (openFileDialog.ShowDialog() == true)
        //            return openFileDialog.FileNames;
        //    }
        //    else
        //    {
        //        Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
        //        ofd.Title = title;
        //        ofd.CheckFileExists = true;
        //        ofd.RestoreDirectory = true;
        //        ofd.Multiselect = true;
        //        ofd.Filter = filter;

        //        if (ofd.ShowDialog() == true)
        //            return ofd.FileNames;
        //    }

        //    return null;
        //}
    }
}
