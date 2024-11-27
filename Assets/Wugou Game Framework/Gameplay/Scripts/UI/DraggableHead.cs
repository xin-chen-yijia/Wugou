using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 可拖动功能
/// </summary>
public class DraggableHead : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
{
    public RectTransform targetRect;

    private bool isDrag;
    private Vector3 posOffset;

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDrag = false;
        SetDragObjPostion(eventData);
        //targetRect.SetSiblingIndex(1);
        targetRect.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        isDrag = true;
        SetDragObjPostion(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDrag=false;
    }

    void SetDragObjPostion(PointerEventData eventData)
    {
        Vector3 mouseWorldPosition;
        if(RectTransformUtility.ScreenPointToWorldPointInRectangle(targetRect,eventData.position,eventData.pressEventCamera,out mouseWorldPosition))
        {
            if (isDrag)
            {
                targetRect.position = mouseWorldPosition + posOffset;
            }
            else
            {
                posOffset = targetRect.position - mouseWorldPosition;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if(targetRect == null)
        {
            targetRect = transform.parent as RectTransform;
        }
    }

    //// Update is called once per frame
    //void Update()
    //{

    //}
}
