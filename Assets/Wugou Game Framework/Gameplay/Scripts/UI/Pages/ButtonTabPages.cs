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
        public class TabPageItem
        {
            public GameObject tab;
            public GameObject page;
            public bool isChecked;
        }

        public List<TabPageItem> items = new List<TabPageItem>();
        public bool allowSwitchOff = false; // 必须选一个

        public bool isTextTransition = false;
        public Color textNormalColor = Color.white;
        public Color textHighLightColor = new Color(1.0f, 0.76f, 0.0f, 0.8f);

        public bool initialized { get; private set; }
        private TabPageItem activeTab_;

        public string activeTabName => activeTab_!=null ? activeTab_.tab.name : "";

        // Start is called before the first frame update
        void Start()
        {
            if (!initialized)
            {
                Init();

                if (items.Count > 0)
                {
                    Toggle(items[0]);
                }
            }

        }

        public void Init(bool force = false)
        {
            if(!force && initialized)
            {
                return;
            }

            initialized = true;
            for(int i=0;i<items.Count;++i)
            {
                SetToggle(items[i], false);

                AddEventToTab(items[i]);
            }
        }

        private void AddEventToTab(TabPageItem item)
        {
            var button = item.tab.GetComponentInChildren<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (allowSwitchOff)
                {
                    if (item.isChecked)
                    {
                        SetToggle(item, false);
                        activeTab_ = null;
                    }
                    else
                    {
                        Toggle(item);
                    }
                }
                else
                {
                    Toggle(item);
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

        public void Toggle(TabPageItem item)
        {
            var newTab = item;
            if(activeTab_ != newTab)
            {
                if (activeTab_ != null)
                {
                    SetToggle(activeTab_, false);
                }
                SetToggle(newTab, true);
                activeTab_ = newTab;
            }
        }

        public void Toggle(int index)
        {
            if(index < 0 || index >= items.Count)
            {
                return;
            }

            Toggle(items[index]);
        }

        private void SetToggle(TabPageItem tab, bool isChecked)
        {
            tab.isChecked = isChecked;
            tab.page.SetActive(isChecked);
            ApplyTextColor(tab.tab.gameObject, isChecked);

            // checked
            tab.tab.transform.Find("Checked")?.gameObject.SetActive(isChecked);
        }

        public void Add(TabPageItem item)
        {
            items.Add(item);
            AddEventToTab(item);
        }

        public void Remove(TabPageItem item)
        {
            if (activeTab_ == item)
            {
                activeTab_ = null;
            }

            Destroy(item.tab);
            Destroy(item.page);
            items.Remove(item);

            if(activeTab_ == null)
            {
                if(items.Count > 0)
                {
                    Toggle(items[0]);
                }

            }
        }

        public void Remove(string name)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].tab.name == name)
                {
                    Remove(items[i]);
                    break;
                }
            }

            Logger.Warning($"No tab '{name}' in {gameObject.name}");
        }
    }
}
