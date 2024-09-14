using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.Verify
{
    public class DongleDaemonPage : MonoBehaviour
    {
        public GameObject tipPage;
        public TMP_Text content;
        public Button okButton;

        public GameObject optionPage;
        public TMP_Text optionContent;
        public Button optionOkButton;
        public Button optionCancelButton;

        // Start is called before the first frame update
        void Start()
        {
            okButton.onClick.AddListener(() =>
            {
                Hide();
            });

            optionOkButton.onClick.AddListener(() =>
            {
                Hide();
            });

            optionCancelButton.onClick.AddListener(() =>
            {
                Hide();
            });
        }

        // Update is called once per frame
        //void Update()
        //{

        //}

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void ShowTips(string text)
        {
            tipPage.SetActive(true);
            optionPage.SetActive(false);

            content.text = text;

            Show();
        }

        public void ShowOption(string text, System.Action okAction)
        {
            tipPage.SetActive(false);
            optionPage.SetActive(true);

            optionContent.text = text;
            optionOkButton.onClick.RemoveAllListeners();
            optionOkButton.onClick.AddListener(() =>
            {
                okAction?.Invoke();
                Hide();
            });

            Show();
        }
    }
}
