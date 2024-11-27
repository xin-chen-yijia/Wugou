using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou
{
    public class FadeInOutEffect : MonoBehaviour
    {
        public static FadeInOutEffect instance { get; private set; } = null;

        public Image fadeImage_;

        public byte stepRate = 10;

        private float step = 0;
        
        // Start is called before the first frame update
        void Start()
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            step = stepRate * 0.001f;
        }

        // Update is called once per frame
        //void Update()
        //{

        //}

        public IEnumerator FadeIn(bool force = false)
        {
            if (force)
            {
                fadeImage_.canvasRenderer.SetAlpha(0.0f);
            }

            float a = fadeImage_.canvasRenderer.GetAlpha();
            fadeImage_.gameObject.SetActive(true);
            while(a < 1.0f)
            {
                yield return null;

                a += step;
                fadeImage_.canvasRenderer.SetAlpha(a);
            }
        }

        public IEnumerator FadeOut(bool force = false)
        {
            if (force)
            {
                fadeImage_.canvasRenderer.SetAlpha(1.0f);
            }

            float a = fadeImage_.canvasRenderer.GetAlpha();
            fadeImage_.gameObject.SetActive(true);
            while (a > 0.0f)
            {
                yield return null;

                a -= step;
                fadeImage_.canvasRenderer.SetAlpha(a);
            }
            fadeImage_.gameObject.SetActive(false);
        }
    }
}
