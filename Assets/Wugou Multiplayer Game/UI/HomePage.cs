using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou;
using UnityEngine.UI;
using System;
using TMPro;

namespace Wugou.UI
{
    public class HomePage : UIBaseWindow
    {
        public GameObject roomPrefab;
        public GameObject roomRowContainer;

        [Serializable]
        public class TabDetail
        {
            public string name;
            public UIBaseWindow page;
        }

        public List<TabDetail> tabs = new List<TabDetail>();

        private List<Toggle> toggles_ = new List<Toggle>();

        public const int StatisticPageId = 1;

        private void Awake()
        {
            //
            Utils.FillContent<TabDetail>(roomRowContainer, roomPrefab, tabs, (GameObject item, TabDetail tabDetail) =>
            {
                item.transform.Find("Label").GetComponent<Text>().text = tabDetail.name;

                var toggle = item.GetComponentInChildren<Toggle>();
                toggle.transform.Find("Label").GetComponent<Text>().color = toggle.isOn ? new Color(1.0f, 0.76f, 0.0f, 0.8f) : Color.white;

                toggle.onValueChanged.AddListener((ison) =>
                {
                    toggle.transform.Find("Label").GetComponent<Text>().color = ison ? new Color(1.0f, 0.76f, 0.0f, 0.8f) : Color.white;
                    if (toggle.isOn)
                    {
                        tabDetail.page.Show();
                    }
                    else
                    {
                        tabDetail.page.Hide();
                    }
                });

                toggles_.Add(toggle);
            });

            toggles_[0].isOn = true;
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        //void Update()
        //{

        //}

        public void Toggle(int id)
        {
            Debug.Assert(id >= 0 && id < toggles_.Count);
            toggles_[id].isOn = true;
        }
    }
}
