using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Wugou.Verify
{
    public class DongleEditor
    {
        [MenuItem("Window/Wugou Tools/Verify/RSA Generate")]
        public static void GenerateKeys()
        {
            string folder = EditorUtility.OpenFolderPanel("…˙≥…√‹‘ø", "./", "");
            if(!string.IsNullOrEmpty(folder) )
            {
                RSA.GenerateKeys(folder);
            }
        }
    }
}
