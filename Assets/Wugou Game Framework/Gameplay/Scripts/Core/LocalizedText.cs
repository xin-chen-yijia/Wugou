using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Wugou {
    public class LocalizedText : MonoBehaviour
    {
        public string id;
        // Start is called before the first frame update
        void Start()
        {
            var text = Localization.GetString(id);
            print(text);

            var textComp = GetComponent<Text>();
            if (textComp != null)
            {
                GetComponent<Text>().text = text;
            }

            var tmpTextComp = GetComponent<TMP_Text>();
            if(tmpTextComp != null)
            {
                tmpTextComp.text = text;
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
