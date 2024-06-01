using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.UI
{
    /// <summary>
    /// 模拟tab页
    /// </summary>
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
        public bool allowSwitchOff = false; // 必须选一个

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
                Toggle(tabs[i], false);

                InitTab(i);
            }
        }

        private void InitTab(int index)
        {
            var button = tabs[index].tab.GetComponentInChildren<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (allowSwitchOff)
                {
                    if (tabs[index].isChecked)
                    {
                        Toggle(tabs[index],false);
                        activeTab_ = null;
                    }
                    else
                    {
                        Toggle(index);
                    }
                }
                else
                {
                    Toggle(index);
                }
            });
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
                Toggle(activeTab_, false);
            }
            Toggle(tabs[index], true);
            activeTab_ = tabs[index];
        }

        private void Toggle(TabDetail tab, bool isChecked)
        {
            tab.isChecked = isChecked;
            tab.page.SetActive(isChecked);
            ApplyTextColor(tab.tab.gameObject, isChecked);

            // checked
            tab.tab.transform.Find("Checked")?.gameObject.SetActive(isChecked);
        }

        public void AddTab(GameObject tab, GameObject page)
        {
            var detail = new TabDetail()
            {
                tab = tab,
                page = page,
            };
            tabs.Add(detail);

            InitTab(tabs.Count - 1);
        }

        public void RemoveTab(string name)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].tab.name == name)
                {
                    if(activeTab_ == tabs[i])
                    {
                        activeTab_ = null;
                    }

                    Destroy(tabs[i].tab);
                    Destroy(tabs[i].page);
                    tabs.RemoveAt(i);

                    Init(true);
                    if (activeTab_ != null)
                    {
                        Toggle(activeTab_, true);
                    }
                    break;
                }
            }
        }
    }
}
