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

        public void UpdateProgressBar(System.Func<float> getProgress)
        {
            StartCoroutine(UpdateProgressBarInternal(getProgress));
        }

        IEnumerator UpdateProgressBarInternal(System.Func<float> getProgress)
        {
            bool bUpdateProgress = true;
            while (bUpdateProgress)
            {
                float p = getProgress.Invoke();
                DaemonUI.loadingPage.SetProgress(p);

                if (p > 0.9999999f)
                {
                    bUpdateProgress = false;
                }

                yield return null;
            }
        }
    }

}
