using Wugou.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssetbundlePreviewer
{
    public class PluginInfoPage : UIBaseWindow
    {
        public GameObject pluginRowPrefab;

        // Start is called before the first frame update
        void Start()
        {
            SetPlugins(new List<string>
            {
                "Uniform Weather System",
                "Gaia Pro 2021 - Terrain Scene Generator [3.3.7]",
                "steamvr_2_7_3",
            });
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetPlugins(List<string> plugins)
        {
            for (int i = 0; i < plugins.Count; i++)
            {
                var pfb = GameObject.Instantiate<GameObject>(pluginRowPrefab, pluginRowPrefab.transform.parent);

                pfb.transform.Find("Name").GetComponent<Text>().text = $"{i}        {plugins[i]}";
                pfb.GetComponentInChildren<Toggle>().isOn = true;
            }

            pluginRowPrefab.SetActive(false);
        }
    }
}

