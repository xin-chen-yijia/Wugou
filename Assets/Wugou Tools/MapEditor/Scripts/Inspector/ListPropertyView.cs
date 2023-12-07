using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;
using System.Data;

namespace Wugou.MapEditor
{
    public class ListPropertyView : PropertyView
    {
        public GameObject content;
        public TMP_InputField countInput;
        public Button addButton;
        public Button removeButton;

        public Type elementType;

        List<GameObject> items = new List<GameObject>();

        public override string head
        {
            get { return gameObject.transform.Find("Head/Name").GetComponent<TMP_Text>().text; }
            set { gameObject.transform.Find("Head/Name").GetComponent<TMP_Text>().text = value; }
        }

        public override void SetValue(object target)
        {
            Type elmType = Utils.GetCollectionElementType(target.GetType());    // 不搞嵌入和复杂类型，就支持int,float,string
            Type targetType = target.GetType();

            var getCount = targetType.GetMethod("get_Count");
            countInput.text = getCount.Invoke(target, null).ToString();

            System.Action addOne = () =>
            {
                var addMethod = targetType.GetMethod("Add");
                object elm = null;
                if (elmType == typeof(string))
                {
                    elm = Activator.CreateInstance(elmType,"".ToCharArray());
                }
                else
                {
                    elm = Activator.CreateInstance(elmType);
                }
                addMethod.Invoke(target, new object[] { elm });


                int count = (int)getCount.Invoke(target, null);

                var getItem = targetType.GetMethod("get_Item");
                var setItem = targetType.GetMethod("set_Item");
                print(getItem);
                print(setItem);

                var viewObj = PropertyViewManager.instance.CreateView($"element{(count - 1)}", elmType, (val) => { setItem.Invoke(target, new object[] { count - 1, val }); }, () => { return getItem.Invoke(target, new object[] { count - 1 }); });
                if (viewObj)
                {
                    viewObj.transform.SetParent(content.transform);
                    items.Add(viewObj);

                    countInput.text = (items.Count).ToString();
                }
            };

            addButton.onClick.RemoveAllListeners();
            addButton.onClick.AddListener(() =>
            {
                addOne();
            });

            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(() =>
            {
                if(items.Count == 0)
                {
                    return;
                }

                // TODO: 支持选择要删除的行
                int ind = items.Count - 1;  // 删除最后一个
                var removeAt =targetType.GetMethod("RemoveAt");
                removeAt.Invoke(target, new object[] { ind });

                GameObject.Destroy(items[ind]);
                items.RemoveAt(ind);
                countInput.text = (items.Count).ToString();
            });

            countInput.onSubmit.RemoveAllListeners();
            countInput.onSubmit.AddListener((val) =>
            {
                int cc = items.Count;
                if(int.TryParse(val, out cc))
                {
                    if(cc > items.Count)
                    {
                        int toAddCount = cc - items.Count;
                        for (int i = 0; i < toAddCount; i++)
                        {
                            addOne();
                        }
                    }
                    else if(cc < items.Count)
                    {
                        var removeRange = targetType.GetMethod("RemoveRange");
                        removeRange.Invoke(target, new object[] {cc,items.Count - cc });

                        for(int i=cc; i < items.Count; ++i)
                        {
                            GameObject.Destroy(items[i]);
                        }
                        items.RemoveRange(cc,items.Count - cc);
                    }
                }
            });
        }

        public override void AddUpdateEvent(Action action)
        {
        }
    } 

}
