using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Wugou.UI
{
    /// <summary>
    /// Ω•“˛Ã· æ
    /// </summary>
    public class FadeOutTipsPage : UIBaseWindow
    {
        public TMP_Text contentLabel;

        public void Show(string content, float duration = 3.0f)
        {
            Show();

            contentLabel.text = content;
            foreach(var v in GetComponentsInChildren<Graphic>())
            {
                v.canvasRenderer.SetAlpha(1.0f);
                v.CrossFadeAlpha(0.0f, duration, true);
            }


            Utils.DoAsync(async() => {
                await new YieldInstructionAwaiter(new WaitForSeconds(duration));
                Hide();
            });
        }

        //// Start is called before the first frame update
        //void Start()
        //{

        //}

        //// Update is called once per frame
        //void Update()
        //{

        //}
    }
}
