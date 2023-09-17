using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if WUGOU_XR
using Valve.VR.Extras;

[RequireComponent(typeof(SteamVR_LaserPointer))]
public class LaserInteractUI : MonoBehaviour
{
    private SteamVR_LaserPointer laserPointer_;
    // Start is called before the first frame update
    void Start()
    {
        laserPointer_ = GetComponent<SteamVR_LaserPointer>();

        if(laserPointer_ != null)
        {
            laserPointer_.PointerIn += LaserPointerIn;
            laserPointer_.PointerOut += LaserPointerOut;
            laserPointer_.PointerClick += LaserOnPointerClick;
        }
        else
        {
            Debug.LogError($"SteamVR_LaserPointer not found...");
        }
    }

    private void LaserPointerIn(object sender, PointerEventArgs e)
    {
        IPointerEnterHandler enter = e.target.parent.gameObject.GetComponent<IPointerEnterHandler>();
        if(enter != null )
        {
            enter.OnPointerEnter(new PointerEventData(EventSystem.current));
        }
    }

    private void LaserPointerOut(object sender, PointerEventArgs e)
    {
        IPointerExitHandler enter = e.target.parent.gameObject.GetComponent<IPointerExitHandler>();
        if(enter != null )
        {
            enter.OnPointerExit(new PointerEventData(EventSystem.current));
        }   
    }

    void LaserOnPointerClick(object sender, PointerEventArgs e)
    {
        IPointerClickHandler enter = e.target.parent.gameObject.GetComponent<IPointerClickHandler>();
        if(enter != null )
        {
            enter.OnPointerClick(new PointerEventData(EventSystem.current));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
#endif
