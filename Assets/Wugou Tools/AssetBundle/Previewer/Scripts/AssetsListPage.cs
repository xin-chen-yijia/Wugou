#if !UNITY_WEBGL
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

using System;
using System.IO;

using Wugou.UI;
using Wugou;

using UnityEngine.EventSystems;

namespace Wugou.Examples.AssetbundlePreviewer
{
    public class AssetsListPage : UIBaseWindow
    {
        public GameObject assetItemContainer;
        public GameObject assetItemPrefab;

        public Sprite sceneSprite;
        public Sprite perfabSprite;

        private GameObject newPerfab;
        // Start is called before the first frame update
        void Start()
        {
            assetItemPrefab.SetActive(false);
        }


        // Update is called once per frame
        void Update()
        {
            if(newPerfab)
            {
                newPerfab.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10));

                //RaycastHit hit;
                //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                //if (Physics.Raycast(ray, out hit))
                //{
                //    newPerfab.transform.position = hit.point;
                //}

            }
        }

        public void SetAssets(List<string> assets)
        {
            float lastClickTime = -1;
            Utils.FillContent(assetItemContainer, assetItemPrefab, assets, (go, item) =>
            {
                go.GetComponentInChildren<Text>().text = Path.GetFileNameWithoutExtension(item);

                if (item.EndsWith(".unity"))
                {
                    go.transform.Find("Image").GetComponent<Image>().sprite = sceneSprite;
                    go.GetComponentInChildren<Button>().onClick.AddListener(() =>
                    {
                        if (Time.realtimeSinceStartup - lastClickTime < Utils.doubleClickMaxInterval)
                        {
                            AssetbundlePreviewer.instance.LoadScene(item);
                        }
                        lastClickTime = Time.realtimeSinceStartup;
                    });
                }
                else if (item.EndsWith(".prefab"))
                {
                    go.transform.Find("Image").GetComponent<Image>().sprite = perfabSprite;
                    //EventTrigger.Entry hoverEntry = new EventTrigger.Entry();
                    //hoverEntry.eventID = EventTriggerType.PointerDown;
                    //hoverEntry.callback = new EventTrigger.TriggerEvent();
                    //UnityEngine.Events.UnityAction<BaseEventData> CreatePerfabCallBack =
                    //    new UnityEngine.Events.UnityAction<BaseEventData>((eventData) => { CreatePrefab(item); });
                    //hoverEntry.callback.AddListener(CreatePerfabCallBack);
                    ////go.GetComponentInChildren<EventTrigger>().name = item;
                    //go.GetComponentInChildren<EventTrigger>().triggers.Add(hoverEntry);

                    go.GetComponentInChildren<Button>().onClick.AddListener(() =>
                    {
                        _ = AssetbundlePreviewer.instance.LoadAsset(item);
                    });
                }

                //go.GetComponentInChildren<Button>().onClick.AddListener(() =>
                //{

                    //    if(Time.realtimeSinceStartup - lastClickTime < Utils.doubleClickMaxInterval)
                    //    {
                    //        if (item.EndsWith(".unity"))
                    //        {
                    //            AssetbundlePreviewer.instance.LoadScene(item);
                    //        }
                    //        else if (item.EndsWith(".prefab"))
                    //        {
                    //            AssetbundlePreviewer.instance.LoadAsset(item);
                    //        }
                    //    }
                    //    lastClickTime = Time.realtimeSinceStartup;
                    //});

            });

        }


        //public async void CreatePrefab(string perfab)
        //{
        //    GameObject obj = await AssetbundlePreviewer.instance.LoadAsset(perfab);
        //    newPerfab = obj;
        //    Camera.main.GetComponent<FlyCamera>().enabled = false;
        //}

        //public void CompleteCreatePrefab()
        //{
        //    Camera.main.GetComponent<FlyCamera>().enabled = true;
        //    newPerfab = null;
        //}

    }
}
#endif
