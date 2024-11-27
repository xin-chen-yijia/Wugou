using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.UI
{
    [RequireComponent(typeof(ToggleGroup))]
    public class ToggleTabPages : MonoBehaviour
    {
        [Serializable]
        public class TabDetail
        {
            public Toggle toggle;
            public UIBaseWindow page;
        }

        public List<TabDetail> tabs = new List<TabDetail>();

        public bool isTextTransition = false;
        public Color textNormalColor = Color.white;
        public Color textHighLightColor = new Color(1.0f, 0.76f, 0.0f, 0.8f);

        // Start is called before the first frame update
        void Start()
        {
            foreach (var v in tabs)
            {
                var toggle = v.toggle;
                toggle.group = GetComponent<ToggleGroup>();

                ApplyTextColor(toggle);

                toggle.onValueChanged.AddListener((isOn) =>
                {
                    ApplyTextColor(toggle);

                    toggle.transform.Find("Checked")?.gameObject.SetActive(isOn);
                    if (isOn)
                    {
                        v.page.Show();
                    }
                    else
                    {
                        v.page.Hide();
                    }
                });
            }

            if(tabs.Count > 0)
            {
                tabs[0].toggle.isOn = true;
            }

        }

        private void ApplyTextColor(Toggle toggle)
        {
            if (isTextTransition)
            {
                var label = toggle.transform.Find("Name").GetComponent<TMP_Text>();
                label.color = toggle.isOn ? textHighLightColor : textNormalColor;
            }
        }

        public void Toggle(int id)
        {
            Debug.Assert(id >= 0 && id < tabs.Count);
            tabs[id].toggle.isOn = true;
        }
    }
}
