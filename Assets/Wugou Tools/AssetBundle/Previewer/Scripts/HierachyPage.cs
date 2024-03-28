using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;
using Wugou.UI;
using Wugou;

namespace Wugou.Examples.AssetbundlePreviewer
{

    public class HierachyPage : UIBaseWindow
    {
        public GameObject itemContainer;
        public GameObject itemPrefab;

        private GameObject checkedUI;
        private GameObject checkedObj;

        private List<GameObject> createdObjs;
        

        public void AddObject(GameObject obj)
        {
            var go = GameObject.Instantiate<GameObject>(itemPrefab, itemContainer.transform);
            go.SetActive(true);
            go.GetComponentInChildren<TMP_Text>().text = obj.name;
            CheckObject(go, obj);
            createdObjs.Add(obj);
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                CheckObject(go, obj);
            });
        }
        public void ClearObjs()
        {
            UnCheckObject();
            for (int i=0;i<itemContainer.transform.childCount;++i)
            {
                Destroy( itemContainer.transform.GetChild(i).gameObject);
            }
            for(int i=0;i<createdObjs.Count;++i)
            {
                Destroy(createdObjs[i]);
            }
            createdObjs.Clear();
        }
        public void CheckObject(GameObject button, GameObject obj)
        {
            if (checkedObj)
            {
                SetOutlineEnabled(checkedObj, false);
                checkedUI.SetActive(false);
            }

            checkedObj = obj;
            checkedUI = button.transform.Find("Checked").gameObject;
            checkedUI.SetActive(true);
            SetOutlineEnabled(checkedObj, true);
        }
        private void UnCheckObject()
        {
            if (checkedObj)
            {
                SetOutlineEnabled(checkedObj, false);
                checkedUI.SetActive(false);
            }
            checkedObj = null;
            checkedUI = null;
        }
        
        private void SetOutlineEnabled(GameObject go, bool enable)
        {
            //if (enable)
            //{
            //    foreach (var v in go.GetComponentsInChildren<Renderer>())
            //    {
            //        var outlineComp = v.GetComponent<cakeslice.Outline>();
            //        if (outlineComp)
            //        {
            //            outlineComp.enabled = true;
            //        }
            //        else
            //        {
            //            outlineComp = v.gameObject.AddComponent<cakeslice.Outline>();
            //            outlineComp.color = 0;
            //        }
            //    }
            //}
            //else
            //{
            //    if (go)
            //    {
            //        foreach (var v in go.GetComponentsInChildren<cakeslice.Outline>())
            //        {
            //            v.enabled = false;
            //        }
            //    }
            //}
        }
        // Start is called before the first frame update
        void Start()
        {
            itemPrefab.SetActive(false);
            createdObjs = new List<GameObject>();
        }

        //// Update is called once per frame
        void Update()
        {
        }
    }
}

