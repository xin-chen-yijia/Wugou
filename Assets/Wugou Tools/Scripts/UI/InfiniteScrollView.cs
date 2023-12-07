using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.UI
{
    /// <summary>
    /// 无限滚动区域，主要用于大滚动区域的情况，因为可视区域是有限的，所以只需要不断的更新可视区域即可
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class InfiniteScrollView : MonoBehaviour
    {
        public GameObject itemPrefab;

        public enum Axis
        {
            Horizontal=0, 
            Vertical=1,
        }

        public Axis axis = Axis.Horizontal;
        public float space = 0;

        public void SetItems<T>(List<T> items, System.Action<GameObject, int> itemInstantiateFunc)
        {
            if(items.Count == 0)
            {
                return;
            }

            var scrollRect = GetComponent<ScrollRect>();
            var scrollRectTrans = GetComponent<RectTransform>();
            var contentTrans = scrollRect.content;

            while(contentTrans.childCount > 0)
            {
                GameObject.DestroyImmediate(contentTrans.GetChild(0).gameObject);
            }

            //元素大小
            float iSize = 0;

            // 计算滚动区域大小，看是否需要滚动
            if(axis == Axis.Horizontal)
            {
                iSize = itemPrefab.GetComponent<RectTransform>().sizeDelta.x;
                contentTrans.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, items.Count * (iSize + space) - space);
            }
            else
            {
                iSize = itemPrefab.GetComponent<RectTransform>().sizeDelta.y;
                contentTrans.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, items.Count * (iSize + space) - space);
            }

            //可视物体数量
            int itemCountOfReuse = 0;
            if (axis == Axis.Horizontal)
            {
                itemCountOfReuse = (int)Mathf.Ceil(scrollRectTrans.sizeDelta.x / (iSize + space));
            }
            else
            {
                itemCountOfReuse = (int)Mathf.Ceil(scrollRectTrans.sizeDelta.y / (iSize + space));
            }
            itemCountOfReuse += 2;  //上下各多两个，避免穿帮
            itemCountOfReuse = Mathf.Clamp(itemCountOfReuse, 0, items.Count);   // 注意，这就是不需要滚动的情况


            // 实例化物体
            itemPrefab.SetActive(true);
            for (int i = 0; i < itemCountOfReuse; ++i)
            {
                GameObject obj = GameObject.Instantiate<GameObject>(itemPrefab, contentTrans);
                obj.SetActive(i < items.Count);

                var trans = obj.GetComponent<RectTransform>();
                trans.anchorMin = new Vector2(0, 1);
                trans.anchorMax = new Vector2(0, 1);
                trans.pivot = new Vector2(0, 1);
                if (axis == Axis.Horizontal)
                {
                    //
                    trans.anchoredPosition = new Vector3(i * (iSize + space), 0, 0);
                }
                else
                {
                    obj.transform.localPosition = new Vector3(0, -i * (iSize + space), 0);
                }

                obj.name = i.ToString();

                itemInstantiateFunc?.Invoke(obj, i);

            }
            itemPrefab.SetActive(false);

            //滚动实现
            Vector2 lastPos = Vector2.zero;
            scrollRect.onValueChanged.AddListener((pos) =>
            {
                if(axis == Axis.Horizontal)
                {
                    // 计算可视区域在整个区域所处位置
                    float startLine = pos.x * (contentTrans.sizeDelta.x - scrollRectTrans.sizeDelta.x);
                    float endLine = startLine + scrollRectTrans.sizeDelta.x;

                    // 先处理极端的两种看不见的情况，即全部在左边和全部在右边,再处理部分看不见的情况
                    if ((contentTrans.GetChild(contentTrans.childCount - 1) as RectTransform).anchoredPosition.x + iSize <= startLine                     // 全在左边
                        || (contentTrans.GetChild(0) as RectTransform).anchoredPosition.x > endLine)      //全在右边
                    {
                        int startIndex = Mathf.FloorToInt(startLine / (iSize + space));
                        for (int i = 0, j = startIndex; i < contentTrans.childCount; i++, j++)
                        {
                            var tt = contentTrans.GetChild(i) as RectTransform;
                            var tmp = tt.anchoredPosition; 
                            tmp.x = j * (iSize + space); 
                            tt.anchoredPosition = tmp;
                            tt.name = j.ToString();

                            // update ui
                            if(j < items.Count && j > -1)
                            {
                                itemInstantiateFunc?.Invoke(tt.gameObject, j);
                            }
                        }

                        return;
                    }

                    // 一些元素超出左侧看不见,发生在向左滑动的时候
                    if (pos.x > lastPos.x)
                    {
                        var left_tt = contentTrans.GetChild(0) as RectTransform;
                        while (left_tt.anchoredPosition.x + iSize <= startLine)
                        {
                            var transE = contentTrans.GetChild(contentTrans.childCount - 1) as RectTransform;
                            var tmp = transE.anchoredPosition;
                            tmp.x += iSize + space;
                            left_tt.anchoredPosition = tmp;
                            left_tt.SetAsLastSibling();
                            int index = int.Parse(transE.name) + 1;
                            left_tt.name = index.ToString();

                            // update ui
                            if (index < items.Count && index > -1)
                            {
                                itemInstantiateFunc?.Invoke(left_tt.gameObject, index);
                            }

                            left_tt = contentTrans.GetChild(0) as RectTransform;
                        }
                    }
                    else                 // 一些元素超出右侧看不见，发生在向右滑动的时候
                    {
                        var right_tt = contentTrans.GetChild(contentTrans.childCount - 1) as RectTransform;
                        while ((right_tt).anchoredPosition.x > endLine)
                        {
                            var trans0 = contentTrans.GetChild(0) as RectTransform;
                            var tmp = trans0.anchoredPosition;
                            tmp.x -= iSize + space;
                            right_tt.anchoredPosition = tmp;
                            right_tt.SetAsFirstSibling();
                            int index = int.Parse(trans0.name) - 1;
                            right_tt.name = index.ToString();

                            // update ui
                            if (index < items.Count && index > -1)
                            {
                                itemInstantiateFunc?.Invoke(right_tt.gameObject, index);
                            }

                            right_tt = contentTrans.GetChild(contentTrans.childCount - 1) as RectTransform;
                        }
                    }

                }
                else
                {
                    // 计算可视区域在整个区域所处位置
                    float startLine = -(1.0f -pos.y) * (contentTrans.sizeDelta.y - scrollRectTrans.sizeDelta.y);
                    float endLine = startLine - scrollRectTrans.sizeDelta.y;

                    // 先处理极端的两种看不见的情况，即全部在左边和全部在右边,再处理部分看不见的情况
                    if ((contentTrans.GetChild(contentTrans.childCount - 1) as RectTransform).anchoredPosition.y - iSize > startLine                     // 全在左边
                        || (contentTrans.GetChild(0) as RectTransform).anchoredPosition.y <= endLine)      //全在右边
                    {
                        int startIndex = Mathf.FloorToInt(-startLine / (iSize + space));
                        for (int i = 0, j = startIndex; i < contentTrans.childCount; i++, j++)
                        {
                            var tt = contentTrans.GetChild(i) as RectTransform;
                            var tmp = tt.anchoredPosition;
                            tmp.y = -j * (iSize + space);
                            tt.anchoredPosition = tmp;
                            tt.name = j.ToString();

                            // update ui
                            if (j < items.Count && j > -1)
                            {
                                itemInstantiateFunc?.Invoke(tt.gameObject, j);
                            }
                        }

                        return;
                    }

                    // 一些元素超出上方看不见,发生在向左滑动的时候
                    if (pos.y < lastPos.y)
                    {
                        var top_tt = contentTrans.GetChild(0) as RectTransform;
                        while (top_tt.anchoredPosition.y - iSize > startLine)
                        {
                            var transE = contentTrans.GetChild(contentTrans.childCount - 1) as RectTransform;
                            var tmp = transE.anchoredPosition;
                            tmp.y -= iSize + space;
                            top_tt.anchoredPosition = tmp;
                            top_tt.SetAsLastSibling();
                            int index = int.Parse(transE.name) + 1;
                            top_tt.name = index.ToString();

                            // update ui
                            if (index < items.Count && index > -1)
                            {
                                itemInstantiateFunc?.Invoke(top_tt.gameObject, index);
                            }

                            top_tt = contentTrans.GetChild(0) as RectTransform;
                        }
                    }
                    else                 // 一些元素超出下部看不见，发生在向右滑动的时候
                    {
                        var bottom_tt = contentTrans.GetChild(contentTrans.childCount - 1) as RectTransform;
                        while ((bottom_tt).anchoredPosition.y <= endLine)
                        {
                            var trans0 = contentTrans.GetChild(0) as RectTransform;
                            var tmp = trans0.anchoredPosition;
                            tmp.y += iSize + space;
                            bottom_tt.anchoredPosition = tmp;
                            bottom_tt.SetAsFirstSibling();
                            int index = int.Parse(trans0.name) - 1;
                            bottom_tt.name = index.ToString();

                            // update ui
                            if (index < items.Count && index > -1)
                            {
                                itemInstantiateFunc?.Invoke(bottom_tt.gameObject, index);
                            }

                            bottom_tt = contentTrans.GetChild(contentTrans.childCount - 1) as RectTransform;
                        }
                    }
                }



                lastPos = pos;
            });
        }

        IEnumerator WaitAndDo(float time, System.Action action)
        {
            yield return new WaitForSeconds(time);

            action?.Invoke();
        }


        //public static InfiniteScrollView CreateVertical(ScrollRect scrollRect, GameObject itemPrefab, int contentCount, System.Action<int, GameObject> updateFunc)
        //{
        //    InfiniteScrollView isv = new InfiniteScrollView();
        //    isv.scrollRect_ = scrollRect;
        //    isv.itemPrefab = itemPrefab;
        //    isv.updateItemContentFunc_ = updateFunc;
        //    isv.contentCount_ = contentCount;

        //    var scrollTrans = scrollRect.GetComponent<RectTransform>();
        //    var contentTrans = scrollRect.content.GetComponent<RectTransform>();

        //    //元素大小
        //    float iHeight = itemPrefab.GetComponent<RectTransform>().sizeDelta.y;
        //    isv.itemHeight_ = iHeight;

        //    //复用数量
        //    int itemCountOfReuse = (int)Mathf.Ceil(scrollTrans.sizeDelta.y / iHeight);
        //    itemCountOfReuse += 2;  //上下各多两个，避免穿帮

        //    //滚动区域大小
        //    float viewHeight = Mathf.Max(iHeight * contentCount, scrollTrans.sizeDelta.y);
        //    contentTrans.sizeDelta = new Vector2(contentTrans.sizeDelta.x, viewHeight); //大小

        //    //位置更新
        //    isv.updateItemPosFunc_ = (GameObject obj, int index) =>
        //    {
        //        obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, 0.5f * contentTrans.sizeDelta.y - 0.5f * iHeight - index * iHeight);
        //    };

        //    isv.setAsSiblingFunc_ = (GameObject obj, GameObject target, bool afterTarget) =>
        //    {
        //        obj.GetComponent<RectTransform>().anchoredPosition = target.GetComponent<RectTransform>().anchoredPosition + new Vector2(0.0f, afterTarget ? -iHeight : iHeight);
        //    };

        //    //初始化元素
        //    for (int i = 0; i < itemCountOfReuse; ++i)
        //    {
        //        GameObject obj = GameObject.Instantiate<GameObject>(itemPrefab, contentTrans);
        //        obj.SetActive(i < contentCount);
        //        if ()
        //        {

        //            //obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, 0.5f * viewHeight - 0.5f * iHeight - i * iHeight);
        //            isv.updateItemPosFunc_(obj, i);
        //            isv.updateItemContentFunc_(i, obj);
        //            linkList.AddLast(new ScrollItem(i, obj));
        //        }
        //        else
        //        {
        //            obj.SetActive(false);
        //        }
        //    }

        //    //滚动实现

        //    UnityEngine.Events.UnityAction<Vector2> onScroll = (Vector2 pos) =>
        //    {
        //        float scrollHeight = contentTrans.sizeDelta.y - scrollTrans.sizeDelta.y;
        //        float h = (1.0f - pos.y) * scrollHeight;
        //        int start = Mathf.FloorToInt(h / iHeight);
        //        start += (h - start * iHeight) > 0.001f ? 1 : 0;
        //        start = Mathf.Min(start, isv.contentCount_ - linkList.Count);
        //        if (start > linkList.Last.Value.index || start + linkList.Count < linkList.First.Value.index) //幅度较大
        //        {
        //            var tmp = linkList.First;
        //            for (int i = 0; i < linkList.Count; ++i, tmp = tmp.Next)
        //            {
        //                //tmp.Value.itemObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, 0.5f * iHeight * contentCount - 0.5f * iHeight - (start + i) * iHeight);
        //                isv.updateItemPosFunc_(tmp.Value.itemObj, start + i);
        //                isv.updateItemContentFunc_(start + i, tmp.Value.itemObj);
        //                tmp.Value.index = start + i;
        //            }
        //        }
        //        else
        //        {
        //            while (linkList.First.Value.index < start)
        //            {
        //                var tmp = linkList.First;
        //                int tmpIndex = linkList.Last.Value.index + 1;
        //                tmp.Value.index = tmpIndex;
        //                isv.updateItemContentFunc_(tmpIndex, tmp.Value.itemObj);
        //                //tmp.Value.itemObj.GetComponent<RectTransform>().anchoredPosition = linkList.Last.Value.itemObj.GetComponent<RectTransform>().anchoredPosition - new Vector2(0.0f, iHeight);
        //                isv.setAsSiblingFunc_(tmp.Value.itemObj, linkList.Last.Value.itemObj, true);
        //                linkList.RemoveFirst();
        //                linkList.AddLast(new ScrollItem(tmpIndex, tmp.Value.itemObj));

        //            }
        //            var tail = linkList.Last;
        //            while (linkList.Last.Value.index > start + linkList.Count - 1)
        //            {
        //                var tmp = linkList.Last;
        //                int tmpIndex = linkList.First.Value.index - 1;
        //                tmp.Value.index = tmpIndex;
        //                isv.updateItemContentFunc_(tmpIndex, tmp.Value.itemObj);
        //                //tmp.Value.itemObj.GetComponent<RectTransform>().anchoredPosition = linkList.First.Value.itemObj.GetComponent<RectTransform>().anchoredPosition + new Vector2(0.0f, iHeight);
        //                isv.setAsSiblingFunc_(tmp.Value.itemObj, linkList.First.Value.itemObj, false);
        //                linkList.RemoveLast();
        //                linkList.AddFirst(new ScrollItem(tmpIndex, tmp.Value.itemObj));
        //            }
        //        }

        //    };

        //    scrollRect.onValueChanged.AddListener(onScroll);
        //    isv.onScroll_ = onScroll;

        //    isv.setItemFunc_ = (int count, System.Action<int, GameObject> func) =>
        //    {
        //        //滚动区域大小
        //        float vHeight = Mathf.Max(iHeight * count, scrollTrans.sizeDelta.y);
        //        contentTrans.sizeDelta = new Vector2(contentTrans.sizeDelta.x, vHeight); //大小

        //        isv.updateItemContentFunc_ = func;
        //        isv.contentCount_ = count;
        //        var tmp = isv.linkList_.First;
        //        for (int i = 0; i < isv.linkList_.Count; ++i, tmp = tmp.Next)
        //        {
        //            if (i < count)
        //            {
        //                isv.updateItemPosFunc_(tmp.Value.itemObj, i);
        //                isv.updateItemContentFunc_(i, tmp.Value.itemObj);
        //                tmp.Value.index = i;
        //                tmp.Value.itemObj.SetActive(true);
        //            }
        //            else
        //            {
        //                tmp.Value.itemObj.SetActive(false);
        //            }
        //        }
        //    };

        //    return isv;
        //}

        //public static InfiniteScrollView CreateHorizontal(ScrollRect scrollRect, GameObject itemPrefab, int contentCount, System.Action<int, GameObject> updateFunc)
        //{
        //    InfiniteScrollView isv = new InfiniteScrollView();
        //    isv.scrollRect_ = scrollRect;
        //    isv.itemPrefab = itemPrefab;
        //    isv.updateItemContentFunc_ = updateFunc;
        //    isv.contentCount_ = contentCount;

        //    var scrollTrans = scrollRect.GetComponent<RectTransform>();
        //    var contentTrans = scrollRect.content.GetComponent<RectTransform>();

        //    //元素大小
        //    float iHeight = itemPrefab.GetComponent<RectTransform>().sizeDelta.x;
        //    isv.itemHeight_ = iHeight;

        //    //复用数量
        //    int itemCountOfReuse = (int)Mathf.Ceil(scrollRect.GetComponent<RectTransform>().sizeDelta.x / iHeight);
        //    itemCountOfReuse += 2;  //上下各多两个，避免穿帮

        //    //滚动区域大小
        //    float viewHeight = Mathf.Max(iHeight * contentCount, scrollTrans.sizeDelta.x);
        //    contentTrans.sizeDelta = new Vector2(viewHeight, contentTrans.sizeDelta.y); //大小

        //    //位置更新
        //    isv.updateItemPosFunc_ = (GameObject obj, int index) =>
        //    {
        //        obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-0.5f * contentTrans.sizeDelta.x + 0.5f * iHeight + index * iHeight, 0.0f);
        //    };

        //    isv.setAsSiblingFunc_ = (GameObject obj, GameObject target, bool afterTarget) =>
        //    {
        //        obj.GetComponent<RectTransform>().anchoredPosition = target.GetComponent<RectTransform>().anchoredPosition + new Vector2(afterTarget ? iHeight : -iHeight, 0.0f);
        //    };

        //    //初始化元素
        //    LinkedList<ScrollItem> linkList = new LinkedList<ScrollItem>();
        //    isv.linkList_ = linkList;
        //    for (int i = 0; i < itemCountOfReuse; ++i)
        //    {
        //        GameObject obj = GameObject.Instantiate<GameObject>(itemPrefab, contentTrans);
        //        if (i < contentCount)
        //        {
        //            obj.SetActive(true);
        //            //obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-0.5f * iHeight * contentCount + 0.5f * iHeight + i * iHeight, 0.0f);
        //            isv.updateItemContentFunc_(i, obj);
        //            isv.updateItemPosFunc_(obj, i);
        //            linkList.AddLast(new ScrollItem(i, obj));
        //        }
        //        else
        //        {
        //            obj.SetActive(false);
        //        }
        //    }

        //    //滚动实现
        //    UnityEngine.Events.UnityAction<Vector2> onScroll = (Vector2 pos) =>
        //    {
        //        float scrollHeight = contentTrans.sizeDelta.x - scrollTrans.sizeDelta.x;
        //        float h = pos.x * scrollHeight;
        //        int start = Mathf.FloorToInt(h / iHeight);
        //        start += (h - start * iHeight) > 0.001f ? 1 : 0;
        //        start = Mathf.Min(start, isv.contentCount_ - linkList.Count);
        //        if (start > linkList.Last.Value.index || start + linkList.Count < linkList.First.Value.index) //幅度较大
        //        {
        //            var tmp = linkList.First;
        //            for (int i = 0; i < linkList.Count; ++i, tmp = tmp.Next)
        //            {
        //                isv.updateItemPosFunc_(tmp.Value.itemObj, start + i);
        //                isv.updateItemContentFunc_(start + i, tmp.Value.itemObj);
        //                tmp.Value.index = start + i;
        //            }
        //        }
        //        else
        //        {
        //            while (linkList.First.Value.index < start)
        //            {
        //                var tmp = linkList.First;
        //                int tmpIndex = linkList.Last.Value.index + 1;
        //                isv.updateItemContentFunc_(tmpIndex, tmp.Value.itemObj);
        //                isv.setAsSiblingFunc_(tmp.Value.itemObj, linkList.Last.Value.itemObj, true);
        //                tmp.Value.index = tmpIndex;
        //                linkList.RemoveFirst();
        //                linkList.AddLast(new ScrollItem(tmpIndex, tmp.Value.itemObj));

        //            }
        //            var tail = linkList.Last;
        //            while (linkList.Last.Value.index > start + linkList.Count - 1)
        //            {
        //                var tmp = linkList.Last;
        //                int tmpIndex = linkList.First.Value.index - 1;
        //                isv.updateItemContentFunc_(tmpIndex, tmp.Value.itemObj);
        //                isv.setAsSiblingFunc_(tmp.Value.itemObj, linkList.First.Value.itemObj, false);
        //                tmp.Value.index = tmpIndex;
        //                linkList.RemoveLast();
        //                linkList.AddFirst(new ScrollItem(tmpIndex, tmp.Value.itemObj));
        //            }
        //        }

        //    };
        //    scrollRect.onValueChanged.AddListener(onScroll);
        //    isv.onScroll_ = onScroll;

        //    isv.setItemFunc_ = (int count, System.Action<int, GameObject> func) =>
        //    {
        //        //滚动区域大小
        //        float vHeight = Mathf.Max(iHeight * count, scrollTrans.sizeDelta.x);
        //        contentTrans.sizeDelta = new Vector2(vHeight, contentTrans.sizeDelta.y); //大小

        //        isv.updateItemContentFunc_ = func;
        //        isv.contentCount_ = count;
        //        var tmp = isv.linkList_.First;
        //        for (int i = 0; i < isv.linkList_.Count; ++i, tmp = tmp.Next)
        //        {
        //            if (i < count)
        //            {
        //                isv.updateItemPosFunc_(tmp.Value.itemObj, i);
        //                isv.updateItemContentFunc_(i, tmp.Value.itemObj);
        //                tmp.Value.index = i;
        //                tmp.Value.itemObj.SetActive(true);
        //            }
        //            else
        //            {
        //                tmp.Value.itemObj.SetActive(false);
        //            }
        //        }
        //    };

        //    return isv;
        //}
    }
}
