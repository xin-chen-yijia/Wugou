using Wugou;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.UI
{
    public class LoadingScenePage : UIBaseWindow
    {
        public Slider progressBar;
        public TMP_Text progressText;

        //// Start is called before the first frame update
        //void Start()
        //{
        //}

        //// Update is called once per frame
        //void Update()
        //{

        //}

        public void SetProgress(float progress)
        {
            progressBar.value = progress;
            progressText.text = string.Format("{0:F0}%", progress * 100);
        }

        public float GetProgress()
        {
            return progressBar.value;
        }

        public void SetText(string text)
        {
            progressText.text = text;
        }
    }

}
