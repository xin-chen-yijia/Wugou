using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou;
using Wugou.UI;
using UnityEngine.UI;
using System;
using TMPro;

namespace Wugou.Examples.UI
{
    public class SimpleHomePage : UIBaseWindow
    {
        public Button trainingButton;

        [Serializable]
        public class TabDetail
        {
            public Toggle tab;
            public UIBaseWindow page;
        }

        public List<TabDetail> tabs = new List<TabDetail>();

        /// <summary>
        /// ÑµÁ·¼ÇÂ¼Ò³Ãæ
        /// </summary>
        public const int StatisticPageId = 1;

        private void Awake()
        {
    
        }

        // Start is called before the first frame update
        void Start()
        {
            trainingButton.onClick.AddListener(() =>
            {
                SimpleNonGamingSystem.instance.EnterLobby();

            });

            foreach (var v in tabs)
            {
                var toggle = v.tab;
                var label = toggle.transform.Find("Name").GetComponent<Text>();
                label.color = toggle.isOn ? new Color(1.0f, 0.76f, 0.0f, 0.8f) : Color.white;

                var page = v.page;
                toggle.onValueChanged.AddListener((ison) =>
                {
                    label.color = ison ? new Color(1.0f, 0.76f, 0.0f, 0.8f) : Color.white;
                    if (ison)
                    {
                        page.Show();
                    }
                    else
                    {
                        page.Hide();
                    }
                });
            }

            tabs[0].tab.isOn = true;
        }

        // Update is called once per frame
        //void Update()
        //{

        //}

        public void Toggle(int id)
        {
            Debug.Assert(id >= 0 && id < tabs.Count);
            tabs[id].tab.isOn = true;
        }
    }
}
