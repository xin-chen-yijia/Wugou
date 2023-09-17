using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Wugou.Editor
{
    public class UtilsEditor
    {
        [MenuItem(ConstDefines.MenuName + "/ReplaceFont", priority = 12000)]
        public static void ReplaceFont()
        {
            //var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            //var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/SourceHanSansCN-Bold SDF.asset");
            //var curObj = Selection.activeGameObject;
            //if (curObj)
            //{
            //    foreach (var v in curObj.GetComponentsInChildren<TMP_Text>(true))
            //    {
            //        v.font = asset;
            //        EditorUtility.SetDirty(v);
            //    }
            //}
            //else
            //{
            //    Debug.LogError("Not select object.");
            //}

            EditorUtility.DisplayDialog("提示", "Please Implement...", "确定");


        }
    }
}
