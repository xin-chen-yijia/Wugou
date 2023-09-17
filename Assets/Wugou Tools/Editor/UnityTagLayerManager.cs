using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Wugou.Editor
{
    /// <summary>
    /// UnityEditor中的tag和layer管理
    /// </summary>
    public static class UnityTagLayerManager
    {
        private static SerializedObject tagLayerManager_;
        private static SerializedObject tagLayerManager
        {
            get
            {
                if(tagLayerManager_ == null)
                {
                    tagLayerManager_ = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                }

                if (tagLayerManager_ == null)
                {
                    Debug.LogError("Could not load asset 'ProjectSettings/TagManager.asset'.");
                }

                return tagLayerManager_;
            }
        }

        public static void AddTag(string tag)
        {
            if (!HasTag(tag))
            {
                //SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                //SerializedProperty it = tagManager.GetIterator();
                //while (it.NextVisible(true))
                //{
                //    if (it.name == "tags")
                //    {
                //        for (int i = 0; i < it.arraySize; i++)
                //        {
                //            SerializedProperty dataPoint = it.GetArrayElementAtIndex(i);
                //            if (string.IsNullOrEmpty(dataPoint.stringValue))
                //            {
                //                dataPoint.stringValue = tag;
                //                tagManager.ApplyModifiedProperties();
                //                return;
                //            }
                //        }
                //    }
                //}

                UnityEditorInternal.InternalEditorUtility.AddTag(tag);
            }
        }

        public static void AddTags(string[] tags)
        {
            SerializedProperty it = tagLayerManager.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.name == "tags")
                {
                    if(tags.Length > it.arraySize)
                    {
                        int dif = tags.Length - it.arraySize;
                        for(int i=0;i < dif; ++i)
                        {
                            it.InsertArrayElementAtIndex(it.arraySize);
                        }
                    }
                    for (int i = 0; i < it.arraySize; i++)
                    {
                        SerializedProperty dataPoint = it.GetArrayElementAtIndex(i);
                        dataPoint.stringValue = tags[i];
                        tagLayerManager.ApplyModifiedProperties();
                    }

                    break;
                }
            }
        }
        public static bool HasTag(string tag)
        {
            for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.tags.Length; i++)
            {
                if (UnityEditorInternal.InternalEditorUtility.tags[i].Contains(tag))
                    return true;
            }
            return false;
        }


        /// <summary>
        /// 创建Layer
        /// </summary>
        /// <param name="layerName"></param>
        public static void CreateLayer(string layerName)
        {
            if (tagLayerManager_ == null)
                tagLayerManager_ = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            if (tagLayerManager_ == null)
            {
                Debug.Log("Could not load asset 'ProjectSettings/TagManager.asset'.");
                return;
            }

            SerializedProperty layersProp = tagLayerManager_.FindProperty("layers");
            for (int i = 8; i <= 31; i++)
            {
                SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
                if (sp != null && String.IsNullOrEmpty(sp.stringValue))  // not override
                {
                    sp.stringValue = layerName;
                    break;
                }
            }

            tagLayerManager_.ApplyModifiedProperties();
        }

        public static void SetLayer(int index, string layer)
        {
            if (!HasLayer(layer))
            {
                SerializedProperty it = tagLayerManager.GetIterator();
                while (it.NextVisible(true))
                {
                    if (it.name == "layers")
                    {
                        SerializedProperty p = it.GetArrayElementAtIndex(index);
                        p.stringValue = layer;
                        tagLayerManager.ApplyModifiedProperties();
                    }
                }
            }
        }

        public static bool HasLayer(string layer)
        {
            for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.layers.Length; i++)
            {
                if (UnityEditorInternal.InternalEditorUtility.layers[i].Contains(layer))
                    return true;
            }
            return false;
        }
    }
}

/// <summary>
/// 资产导入回调
/// </summary>
public class TagLayerImporter: AssetPostprocessor
{
    //static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    //{
    //    foreach (string s in importedAssets) 
    //    {
    //        string aname = s.Substring(s.LastIndexOf('/') + 1);
    //        if (aname.Equals("FireTagLayerImporter.cs"))
    //        {
    //            AddTag("abc");
    //            foreach(var v in tags)
    //            {
    //                //AddTag(v);
    //            }

    //            break;
    //        }
    //    }
    //}

}