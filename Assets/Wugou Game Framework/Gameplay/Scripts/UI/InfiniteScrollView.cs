using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.UI
{
    /// <summary>
    /// 无限滚动区域，主要用于大滚动区域的情况，因为可视区域是有限的，所以只需要不断的更新可视区域即可
    /// 注意： 所用的Prefab锚点要是一个点
    /// TODO: 完善删除元素，添加一个OnValueDelete函数处理有数据被删除的情况，只需要更新数据
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

        // 间隔
        public float space = 0;

        // 多出的可复用组件
        private const int kExtraItemsCount = 1;

        // 所有的项
        private int itemCountOfReuse_ = 0;

        // 记录当前数据项的数量
        private int lastItemsCount_ = -1;

        // 标识当前看到的数据项的索引
        private int curStartIndex_ = 0;
        private int curEndIndex_ = 0;

        // 更新子项内容的回调函数
        private Action<GameObject,int> onItemUpdateCallback_ = null;

        // 获取数据的数量
        private Func<int> GetItemsCount_ = null;

        /// <summary>
        /// 创建可复用物体
        /// </summary>
        public void CreateItems()
        {
            var scrollRect = GetComponent<ScrollRect>();
            var scrollRectTrans = GetComponent<RectTransform>();
            var contentTrans = scrollRect.content;

            itemPrefab.SetActive(false);
            if (axis == Axis.Horizontal)
            {
                // 元素大小
                var iSize = itemPrefab.GetComponent<RectTransform>().rect.width;

                if (contentTrans.childCount == 0)
                {
                    //可视物体数量
                    itemCountOfReuse_ = (int)Mathf.Ceil(scrollRectTrans.rect.width / (iSize + space));
                    itemCountOfReuse_ += kExtraItemsCount;  // 避免穿帮

                    // 实例化可复用物体
                    for (int i = 0; i < itemCountOfReuse_; ++i)
                    {
                        GameObject obj = GameObject.Instantiate<GameObject>(itemPrefab, contentTrans);
                        var trans = obj.GetComponent<RectTransform>();
                        trans.anchorMin = new Vector2(0, 1);
                        trans.anchorMax = new Vector2(0, 1);
                        trans.pivot = new Vector2(0, 1);

                        trans.anchoredPosition = new Vector3(i * (iSize + space), 0, 0);
                    }
                }
            }
            else
            {
                // 元素大小
                var iSize = itemPrefab.GetComponent<RectTransform>().rect.height;

                // 实例化可复用物体
                if (contentTrans.childCount == 0)    // 只创建一次
                {
                    //可视物体数量
                    itemCountOfReuse_ = (int)Mathf.Ceil(scrollRectTrans.rect.height / (iSize + space));
                    itemCountOfReuse_ += kExtraItemsCount;  // 避免穿帮

                    for (int i = 0; i < itemCountOfReuse_; ++i)
                    {
                        GameObject obj = GameObject.Instantiate<GameObject>(itemPrefab, contentTrans);
                        var trans = obj.GetComponent<RectTransform>();
                        trans.anchorMin = new Vector2(0, 1);
                        trans.anchorMax = new Vector2(0, 1);
                        trans.pivot = new Vector2(0, 1);

                        trans.anchoredPosition = new Vector3(0, -i * (iSize + space), 0);
                    }
                }
            }
        }

        /// <summary>
        /// 横向调整大小
        /// </summary>
        /// <param name="itemsCount"></param>
        private void ResizeHorizontal(int itemsCount)
        {
            // 元素大小
            var iSize = itemPrefab.GetComponent<RectTransform>().rect.width;

            // 调整滚动区域大小
            GetComponent<ScrollRect>().content.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, itemsCount * (iSize + space) - space);

        }

        /// <summary>
        /// 纵向调整大小
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        private void ResizeVertical(int itemsCount)
        {
            // 元素大小
            var iSize = itemPrefab.GetComponent<RectTransform>().rect.height;

            // 调整滚动区域大小
            GetComponent<ScrollRect>().content.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, itemsCount * (iSize + space) - space);
        }

        /// <summary>
        /// 横向滚动更新事件
        /// </summary>
        /// <param name="onItemUpdateCallback"></param>
        private void SetScrollEventHorizontal(Action<GameObject, int> onItemUpdateCallback)
        {
            var scrollRect = GetComponent<ScrollRect>();
            var scrollRectTrans = GetComponent<RectTransform>();
            var contentTrans = scrollRect.content;

            // 元素大小
            var iSize = itemPrefab.GetComponent<RectTransform>().rect.width;

            // 可复用元素对应的数据索引
            int curStartIndex = 0;
            int curEndIndex = contentTrans.childCount - 1;

            int reuseItemCount = contentTrans.childCount;

            // 滚动实现
            scrollRect.onValueChanged.RemoveAllListeners();
            scrollRect.onValueChanged.AddListener((pos) =>
            {
                // 计算可视区域在整个区域所处位置
                float startLine = Mathf.Max(0.0f, pos.x * (contentTrans.rect.width - scrollRectTrans.rect.width));
                float endLine = Mathf.Min(startLine + contentTrans.rect.width, startLine + scrollRectTrans.rect.width);

                // 滑块往右，元素向左走
                // 不看最左面的元素，改为看最右面的元素，有需要才从左面拿，这样能保证最右面的元素是有具体数据的
                var right_tt = contentTrans.GetChild(contentTrans.childCount - 1) as RectTransform;
                while (right_tt.anchoredPosition.x + iSize + space + 1 < endLine)   // +1 避免浮点数精度问题
                {
                    var trans0 = contentTrans.GetChild(0) as RectTransform;
                    var tmp = right_tt.anchoredPosition;
                    tmp.x += iSize + space;
                    trans0.anchoredPosition = tmp;
                    trans0.SetAsLastSibling();

                    curStartIndex++;
                    curEndIndex++;

                    // update ui
                    onItemUpdateCallback?.Invoke(trans0.gameObject, curEndIndex);

                    right_tt = trans0; // contentTrans.GetChild(contentTrans.childCount - 1) as RectTransform;
                }

                var left_tt = contentTrans.GetChild(0) as RectTransform;
                while (left_tt.anchoredPosition.x - space - 1 > startLine)
                {
                    var transE = contentTrans.GetChild(contentTrans.childCount - 1) as RectTransform;
                    var tmp = left_tt.anchoredPosition;
                    tmp.x -= iSize + space;
                    transE.anchoredPosition = tmp;
                    transE.SetAsFirstSibling();

                    curStartIndex--;
                    curEndIndex--;

                    // update ui
                    onItemUpdateCallback?.Invoke(transE.gameObject, curStartIndex);

                    left_tt = transE;// contentTrans.GetChild(0) as RectTransform;
                }
 

            });
        }

        /// <summary>
        /// 纵向滚动更新事件
        /// </summary>
        /// <param name="onItemUpdateCallback"></param>
        private void SetScrollEventVertical(Action<GameObject, int> onItemUpdateCallback)
        {
            var scrollRect = GetComponent<ScrollRect>();
            var scrollRectTrans = GetComponent<RectTransform>();
            var contentTrans = scrollRect.content;

            // 元素大小
            var iSize = itemPrefab.GetComponent<RectTransform>().rect.height;

            // 可复用元素对应的数据索引
            curStartIndex_ = 0;
            curEndIndex_ = contentTrans.childCount - 1;

            var reuseItemCount = contentTrans.childCount;

            // 滚动实现
            scrollRect.onValueChanged.RemoveAllListeners();
            scrollRect.onValueChanged.AddListener((pos) =>
            {
                // 计算可视区域在整个区域所处位置
                float startLine = Mathf.Min(0.0f, -(1.0f - pos.y) * (contentTrans.rect.height - scrollRectTrans.rect.height));
                float endLine = Mathf.Max(startLine - contentTrans.rect.height,  startLine - scrollRectTrans.rect.height);

                // 滑块往下，元素向上走
                // 不看上面的元素，改为看最下面的元素，有需要才从上面拿，这样能保证最下面的元素是有具体数据的
                var bottom_tt = contentTrans.GetChild(contentTrans.childCount - 1) as RectTransform;
                while (bottom_tt.anchoredPosition.y - iSize - space - 1 > endLine)  // -1因为浮点数是不精确的。。 可能出现-3000 > -3000 为true的情况
                {
                    var trans0 = contentTrans.GetChild(0) as RectTransform;
                    var tmp = bottom_tt.anchoredPosition;
                    tmp.y -= iSize + space;
                    trans0.anchoredPosition = tmp;
                    trans0.SetAsLastSibling();

                    curStartIndex_++;
                    curEndIndex_++;

                    Debug.Assert(GetDataItemIndex(trans0) == curEndIndex_);
                    // update ui
                    onItemUpdateCallback?.Invoke(trans0.gameObject, curEndIndex_);

                    bottom_tt = trans0;// contentTrans.GetChild(contentTrans.childCount - 1) as RectTransform;
                }

                var top_tt = contentTrans.GetChild(0) as RectTransform;
                //print($"{top_tt} {top_tt.anchoredPosition.y + space + 1} < {startLine} = {top_tt.anchoredPosition.y + space + 1 < startLine}");
                while (top_tt.anchoredPosition.y + space + 1 < startLine)
                {
                    var transE = contentTrans.GetChild(contentTrans.childCount - 1) as RectTransform;
                    var tmp = top_tt.anchoredPosition;
                    tmp.y += iSize + space;
                    transE.anchoredPosition = tmp;
                    transE.SetAsFirstSibling();

                    curEndIndex_--;
                    curStartIndex_--;

                    Debug.Assert(GetDataItemIndex(transE) == curStartIndex_);
                    // update ui
                    onItemUpdateCallback?.Invoke(transE.gameObject, curStartIndex_);

                    top_tt = transE;// contentTrans.GetChild(0) as RectTransform;
                    //print($"{top_tt.anchoredPosition.y + space + 1} < {startLine} = {top_tt.anchoredPosition.y + space + 1 < startLine}");
                }
            });
        }

        /// <summary>
        /// 设置当新数据应用于可复用的组件上时的回调函数
        /// </summary>
        /// <param name="onItemUpdateCallback"></param>
        public void SetData(System.Func<int> getItemCount, System.Action<GameObject, int> onItemUpdateCallback)
        {
            onItemUpdateCallback_ = onItemUpdateCallback;

            //
            GetItemsCount_ = getItemCount;

            // 
            var itemsCount = GetItemsCount_();

            //
            var scrollRect = GetComponent<ScrollRect>();
            var contentTrans = scrollRect.content;

            // 记录之前的位置，后面用于复原，主要是针对滚动后更换数据源的情况
            var prePos = scrollRect.normalizedPosition;
            //
            if (axis == Axis.Horizontal)
            {
                // 先滚动到初始位置
                scrollRect.horizontalNormalizedPosition = 0;
                scrollRect.onValueChanged.Invoke(new Vector2(0, 0));

                SetScrollEventHorizontal(onItemUpdateCallback);
            }
            else
            {
                // 先滚动到初始位置
                scrollRect.verticalNormalizedPosition = 1;
                scrollRect.onValueChanged.Invoke(new Vector2(0, 1));

                SetScrollEventVertical(onItemUpdateCallback);
            }

            //
            OnUpdateData();

            // 恢复
            scrollRect.normalizedPosition = prePos;
        }

        /// <summary>
        /// 根据位置获取索引
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        private int GetDataItemIndex(RectTransform child)
        {
            if(axis == Axis.Horizontal)
            {
                // 元素大小
                var iSize = itemPrefab.GetComponent<RectTransform>().rect.width;
                var posX = child.anchoredPosition.x;
                return (int)(posX / (iSize + space));
            }
            else
            {
                // 元素大小
                var iSize = itemPrefab.GetComponent<RectTransform>().rect.height;
                var posY = child.anchoredPosition.y;
                return Mathf.Abs((int)(posY / (iSize + space)));
            }
        }

        /// <summary>
        /// 更新某一项
        /// </summary>
        /// <param name="itemIndex"></param>
        public void UpdateItem(int itemIndex)
        {
            var scrollRect = GetComponent<ScrollRect>();
            var contentTrans = scrollRect.content;
            for (int i = 0; i < contentTrans.childCount; ++i)
            {
                var child = contentTrans.GetChild(i);
                var index = GetDataItemIndex(child as RectTransform);
                if(index == itemIndex)
                {
                    onItemUpdateCallback_?.Invoke(child.gameObject, itemIndex);
                    break;
                }
            }
        }

        /// <summary>
        /// 用于数据更新，如添加、删除数据等
        /// </summary>
        public void OnUpdateData()
        {
            var itemsCount = GetItemsCount_();
            var scrollRect = GetComponent<ScrollRect>();
            var scrollPos = scrollRect.normalizedPosition;

            // 数量变化,需要更新容器的大小
            if(itemsCount != lastItemsCount_)
            {
                lastItemsCount_ = itemsCount;
                if (axis == Axis.Horizontal)
                {
                    ResizeHorizontal(itemsCount);
                }
                else
                {
                    ResizeVertical(itemsCount);
                }
            }

            // 重新应用一遍数据
            var contentTrans = scrollRect.content;
            Debug.Assert(contentTrans.childCount > 0);
            int tmpIndex = GetDataItemIndex(contentTrans.GetChild(0) as RectTransform);
            for (int i = 0; i < contentTrans.childCount; ++i)
            {
                var n = tmpIndex + i;
                var child = contentTrans.GetChild(i).gameObject;
                if(n >= 0 && n < itemsCount)
                {
                    child.SetActive(true);
                    onItemUpdateCallback_.Invoke(child, n);
                }
                else
                {
                    child.SetActive(false);
                }
            }

            //
            scrollRect.normalizedPosition = scrollPos;
        }
    }
}
