using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.AssetbundlePreviewer
{
    public class LogWindow : MonoBehaviour
    {
        public Text logLabel;

        public static LogWindow instance;

        private void Awake()
        {
            gameObject.SetActive(false);

            instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private int count_ = 0;
        public void Log(string message)
        {
            count_++;
            if (count_ > 120)  
            {
                count_ = 0;
                logLabel.text = "loggin...." + System.Environment.NewLine;
            }

            logLabel.text += message + System.Environment.NewLine;
        }
    }
}

