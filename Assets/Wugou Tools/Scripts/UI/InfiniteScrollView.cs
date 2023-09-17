using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.UI
{
    public class InfiniteScrollView
    {
        private GameObject itemPrefab_;
        private ScrollRect scrollRect_;
        private LinkedList<ScrollItem> linkList_ = null;
        private int contentCount_ = 0;
        private float itemHeight_ = 0.0f;

        private UnityEngine.Events.UnityAction<Vector2> onScroll_;

        private System.Action<int, GameObject> updateItemContentFunc_; //如何更新元素
        private System.Action<GameObject, int> updateItemPosFunc_; //更新位置
        private System.Action<GameObject, GameObject, bool> setAsSiblingFunc_; //跟在后面
        private System.Action<int, System.Action<int, GameObject>> setItemFunc_; //

        public class ScrollItem
        {
            public int index;
            public GameObject itemObj;

            public ScrollItem(int index, GameObject obj)
            {
                this.index = index;
                this.itemObj = obj;
            }
        }

        public void SetItems(int contentCount, System.Action<int, GameObject> updateFunc)
        {
            setItemFunc_(contentCount, updateFunc);
        }

        public void Release()
        {
            var tmp = linkList_.First;
            for (int i = 0; i < linkList_.Count; ++i, tmp = tmp.Next)
            {
                GameObject.Destroy(tmp.Value.itemObj);
            }

            linkList_.Clear();
            scrollRect_.onValueChanged.RemoveListener(onScroll_);
        }

        public static InfiniteScrollView CreateVertical(ScrollRect scrollRect, GameObject itemPrefab, int contentCount, System.Action<int, GameObject> updateFunc)
        {
            InfiniteScrollView isv = new InfiniteScrollView();
            isv.scrollRect_ = scrollRect;
            isv.itemPrefab_ = itemPrefab;
            isv.updateItemContentFunc_ = updateFunc;
            isv.contentCount_ = contentCount;

            var scrollTrans = scrollRect.GetComponent<RectTransform>();
            var contentTrans = scrollRect.content.GetComponent<RectTransform>();

            //元素大小
            float iHeight = itemPrefab.GetComponent<RectTransform>().sizeDelta.y;
            isv.itemHeight_ = iHeight;

            //复用数量
            int itemCountOfReuse = (int)Mathf.Ceil(scrollTrans.sizeDelta.y / iHeight);
            itemCountOfReuse += 2;  //上下各多两个，避免穿帮

            //滚动区域大小
            float viewHeight = Mathf.Max(iHeight * contentCount, scrollTrans.sizeDelta.y);
            contentTrans.sizeDelta = new Vector2(contentTrans.sizeDelta.x, viewHeight); //大小

            //位置更新
            isv.updateItemPosFunc_ = (GameObject obj, int index) =>
            {
                obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, 0.5f * contentTrans.sizeDelta.y - 0.5f * iHeight - index * iHeight);
            };

            isv.setAsSiblingFunc_ = (GameObject obj, GameObject target, bool afterTarget) =>
            {
                obj.GetComponent<RectTransform>().anchoredPosition = target.GetComponent<RectTransform>().anchoredPosition + new Vector2(0.0f, afterTarget ? -iHeight : iHeight);
            };

            //初始化元素
            LinkedList<ScrollItem> linkList = new LinkedList<ScrollItem>();
            isv.linkList_ = linkList;
            for (int i = 0; i < itemCountOfReuse; ++i)
            {
                GameObject obj = GameObject.Instantiate<GameObject>(itemPrefab, contentTrans);
                if (i < contentCount)
                {
                    obj.SetActive(true);
                    //obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, 0.5f * viewHeight - 0.5f * iHeight - i * iHeight);
                    isv.updateItemPosFunc_(obj, i);
                    isv.updateItemContentFunc_(i, obj);
                    linkList.AddLast(new ScrollItem(i, obj));
                }
                else
                {
                    obj.SetActive(false);
                }
            }

            //滚动实现

            UnityEngine.Events.UnityAction<Vector2> onScroll = (Vector2 pos) =>
            {
                float scrollHeight = contentTrans.sizeDelta.y - scrollTrans.sizeDelta.y;
                float h = (1.0f - pos.y) * scrollHeight;
                int start = Mathf.FloorToInt(h / iHeight);
                start += (h - start * iHeight) > 0.001f ? 1 : 0;
                start = Mathf.Min(start, isv.contentCount_ - linkList.Count);
                if (start > linkList.Last.Value.index || start + linkList.Count < linkList.First.Value.index) //幅度较大
                {
                    var tmp = linkList.First;
                    for (int i = 0; i < linkList.Count; ++i, tmp = tmp.Next)
                    {
                        //tmp.Value.itemObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, 0.5f * iHeight * contentCount - 0.5f * iHeight - (start + i) * iHeight);
                        isv.updateItemPosFunc_(tmp.Value.itemObj, start + i);
                        isv.updateItemContentFunc_(start + i, tmp.Value.itemObj);
                        tmp.Value.index = start + i;
                    }
                }
                else
                {
                    while (linkList.First.Value.index < start)
                    {
                        var tmp = linkList.First;
                        int tmpIndex = linkList.Last.Value.index + 1;
                        tmp.Value.index = tmpIndex;
                        isv.updateItemContentFunc_(tmpIndex, tmp.Value.itemObj);
                        //tmp.Value.itemObj.GetComponent<RectTransform>().anchoredPosition = linkList.Last.Value.itemObj.GetComponent<RectTransform>().anchoredPosition - new Vector2(0.0f, iHeight);
                        isv.setAsSiblingFunc_(tmp.Value.itemObj, linkList.Last.Value.itemObj, true);
                        linkList.RemoveFirst();
                        linkList.AddLast(new ScrollItem(tmpIndex, tmp.Value.itemObj));

                    }
                    var tail = linkList.Last;
                    while (linkList.Last.Value.index > start + linkList.Count - 1)
                    {
                        var tmp = linkList.Last;
                        int tmpIndex = linkList.First.Value.index - 1;
                        tmp.Value.index = tmpIndex;
                        isv.updateItemContentFunc_(tmpIndex, tmp.Value.itemObj);
                        //tmp.Value.itemObj.GetComponent<RectTransform>().anchoredPosition = linkList.First.Value.itemObj.GetComponent<RectTransform>().anchoredPosition + new Vector2(0.0f, iHeight);
                        isv.setAsSiblingFunc_(tmp.Value.itemObj, linkList.First.Value.itemObj, false);
                        linkList.RemoveLast();
                        linkList.AddFirst(new ScrollItem(tmpIndex, tmp.Value.itemObj));
                    }
                }

            };

            scrollRect.onValueChanged.AddListener(onScroll);
            isv.onScroll_ = onScroll;

            isv.setItemFunc_ = (int count, System.Action<int, GameObject> func) =>
            {
                //滚动区域大小
                float vHeight = Mathf.Max(iHeight * count, scrollTrans.sizeDelta.y);
                contentTrans.sizeDelta = new Vector2(contentTrans.sizeDelta.x, vHeight); //大小

                isv.updateItemContentFunc_ = func;
                isv.contentCount_ = count;
                var tmp = isv.linkList_.First;
                for (int i = 0; i < isv.linkList_.Count; ++i, tmp = tmp.Next)
                {
                    if (i < count)
                    {
                        isv.updateItemPosFunc_(tmp.Value.itemObj, i);
                        isv.updateItemContentFunc_(i, tmp.Value.itemObj);
                        tmp.Value.index = i;
                        tmp.Value.itemObj.SetActive(true);
                    }
                    else
                    {
                        tmp.Value.itemObj.SetActive(false);
                    }
                }
            };

            return isv;
        }

        public static InfiniteScrollView CreateHorizontal(ScrollRect scrollRect, GameObject itemPrefab, int contentCount, System.Action<int, GameObject> updateFunc)
        {
            InfiniteScrollView isv = new InfiniteScrollView();
            isv.scrollRect_ = scrollRect;
            isv.itemPrefab_ = itemPrefab;
            isv.updateItemContentFunc_ = updateFunc;
            isv.contentCount_ = contentCount;

            var scrollTrans = scrollRect.GetComponent<RectTransform>();
            var contentTrans = scrollRect.content.GetComponent<RectTransform>();

            //元素大小
            float iHeight = itemPrefab.GetComponent<RectTransform>().sizeDelta.x;
            isv.itemHeight_ = iHeight;

            //复用数量
            int itemCountOfReuse = (int)Mathf.Ceil(scrollRect.GetComponent<RectTransform>().sizeDelta.x / iHeight);
            itemCountOfReuse += 2;  //上下各多两个，避免穿帮

            //滚动区域大小
            float viewHeight = Mathf.Max(iHeight * contentCount, scrollTrans.sizeDelta.x);
            contentTrans.sizeDelta = new Vector2(viewHeight, contentTrans.sizeDelta.y); //大小

            //位置更新
            isv.updateItemPosFunc_ = (GameObject obj, int index) =>
            {
                obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-0.5f * contentTrans.sizeDelta.x + 0.5f * iHeight + index * iHeight, 0.0f);
            };

            isv.setAsSiblingFunc_ = (GameObject obj, GameObject target, bool afterTarget) =>
            {
                obj.GetComponent<RectTransform>().anchoredPosition = target.GetComponent<RectTransform>().anchoredPosition + new Vector2(afterTarget ? iHeight : -iHeight, 0.0f);
            };

            //初始化元素
            LinkedList<ScrollItem> linkList = new LinkedList<ScrollItem>();
            isv.linkList_ = linkList;
            for (int i = 0; i < itemCountOfReuse; ++i)
            {
                GameObject obj = GameObject.Instantiate<GameObject>(itemPrefab, contentTrans);
                if (i < contentCount)
                {
                    obj.SetActive(true);
                    //obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-0.5f * iHeight * contentCount + 0.5f * iHeight + i * iHeight, 0.0f);
                    isv.updateItemContentFunc_(i, obj);
                    isv.updateItemPosFunc_(obj, i);
                    linkList.AddLast(new ScrollItem(i, obj));
                }
                else
                {
                    obj.SetActive(false);
                }
            }

            //滚动实现
            UnityEngine.Events.UnityAction<Vector2> onScroll = (Vector2 pos) =>
            {
                float scrollHeight = contentTrans.sizeDelta.x - scrollTrans.sizeDelta.x;
                float h = pos.x * scrollHeight;
                int start = Mathf.FloorToInt(h / iHeight);
                start += (h - start * iHeight) > 0.001f ? 1 : 0;
                start = Mathf.Min(start, isv.contentCount_ - linkList.Count);
                if (start > linkList.Last.Value.index || start + linkList.Count < linkList.First.Value.index) //幅度较大
                {
                    var tmp = linkList.First;
                    for (int i = 0; i < linkList.Count; ++i, tmp = tmp.Next)
                    {
                        isv.updateItemPosFunc_(tmp.Value.itemObj, start + i);
                        isv.updateItemContentFunc_(start + i, tmp.Value.itemObj);
                        tmp.Value.index = start + i;
                    }
                }
                else
                {
                    while (linkList.First.Value.index < start)
                    {
                        var tmp = linkList.First;
                        int tmpIndex = linkList.Last.Value.index + 1;
                        isv.updateItemContentFunc_(tmpIndex, tmp.Value.itemObj);
                        isv.setAsSiblingFunc_(tmp.Value.itemObj, linkList.Last.Value.itemObj, true);
                        tmp.Value.index = tmpIndex;
                        linkList.RemoveFirst();
                        linkList.AddLast(new ScrollItem(tmpIndex, tmp.Value.itemObj));

                    }
                    var tail = linkList.Last;
                    while (linkList.Last.Value.index > start + linkList.Count - 1)
                    {
                        var tmp = linkList.Last;
                        int tmpIndex = linkList.First.Value.index - 1;
                        isv.updateItemContentFunc_(tmpIndex, tmp.Value.itemObj);
                        isv.setAsSiblingFunc_(tmp.Value.itemObj, linkList.First.Value.itemObj, false);
                        tmp.Value.index = tmpIndex;
                        linkList.RemoveLast();
                        linkList.AddFirst(new ScrollItem(tmpIndex, tmp.Value.itemObj));
                    }
                }

            };
            scrollRect.onValueChanged.AddListener(onScroll);
            isv.onScroll_ = onScroll;

            isv.setItemFunc_ = (int count, System.Action<int, GameObject> func) =>
            {
                //滚动区域大小
                float vHeight = Mathf.Max(iHeight * count, scrollTrans.sizeDelta.x);
                contentTrans.sizeDelta = new Vector2(vHeight, contentTrans.sizeDelta.y); //大小

                isv.updateItemContentFunc_ = func;
                isv.contentCount_ = count;
                var tmp = isv.linkList_.First;
                for (int i = 0; i < isv.linkList_.Count; ++i, tmp = tmp.Next)
                {
                    if (i < count)
                    {
                        isv.updateItemPosFunc_(tmp.Value.itemObj, i);
                        isv.updateItemContentFunc_(i, tmp.Value.itemObj);
                        tmp.Value.index = i;
                        tmp.Value.itemObj.SetActive(true);
                    }
                    else
                    {
                        tmp.Value.itemObj.SetActive(false);
                    }
                }
            };

            return isv;
        }
    }
}
