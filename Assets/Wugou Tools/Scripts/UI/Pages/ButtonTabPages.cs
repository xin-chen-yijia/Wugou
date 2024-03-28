using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.UI
{
    public class ButtonTabPages : MonoBehaviour
    {
        [Serializable]
        public class TabDetail
        {
            public GameObject tab;
            public GameObject page;
            public bool isChecked;
        }

        public List<TabDetail> tabs = new List<TabDetail>();

        public bool isTextTransition = false;
        public Color textNormalColor = Color.white;
        public Color textHighLightColor = new Color(1.0f, 0.76f, 0.0f, 0.8f);

        private bool isInited = false;
        private TabDetail activeTab_;

        public string activeTabName => activeTab_!=null ? activeTab_.tab.name : "";

        // Start is called before the first frame update
        void Start()
        {
            Init();

            if (tabs.Count > 0)
            {
                Toggle(0);
            }
        }

        public void Init(bool force = false)
        {
            if(!force && isInited)
            {
                return;
            }

            isInited = true;
            for(int i=0;i<tabs.Count;++i)
            {
                ToggleTab(tabs[i], false);

                int tmp = i;
                tabs[i].tab.GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    Toggle(tmp);
                });
            }
        }

        private void ApplyTextColor(GameObject tab, bool isChecked)
        {
            if (isTextTransition)
            {
                var label = tab.transform.Find("Name").GetComponent<TMP_Text>();
                label.color = isChecked ? textHighLightColor : textNormalColor;
            }
        }

        public void Toggle(int index)
        {
            if(index < 0 ||  index >= tabs.Count)
            {
                return;
            }

            if(activeTab_ != null)
            {
                ToggleTab(activeTab_, false);
            }
            ToggleTab(tabs[index], true);
            activeTab_ = tabs[index];
        }

        private GameObject lastCheckObj_;
        private void ToggleTab(TabDetail tab, bool isChecked)
        {
            tab.isChecked = isChecked;
            tab.page.SetActive(isChecked);
            ApplyTextColor(tab.tab.gameObject, isChecked);

            // checked
            lastCheckObj_?.SetActive(false);
            lastCheckObj_ = tab.tab.transform.Find("Checked")?.gameObject;
            lastCheckObj_?.SetActive(true);
        }

        public void AddTab(GameObject tab, GameObject page)
        {
            var detail = new TabDetail()
            {
                tab = tab,
                page = page,
            };
            tabs.Add(detail);

            int tmp = tabs.Count - 1;
            tab.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
                Toggle(tmp);
            });
        }
    }
}
