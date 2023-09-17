using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.UI;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

namespace Wugou.UI
{
    public class MakeSurePage : UIBaseWindow
    {
        private Button okButton_;
        private Button cancelButton_;
        private TMP_Text content_;

        private bool initialized_ = false;

        // Start is called before the first frame update
        void Start()
        {

        }

        private void Init()
        {
            if (initialized_)
            {
                return;
            }

            initialized_ = true;
            content_ = transform.Find("Content").GetComponent<TMP_Text>();
            okButton_ = transform.Find("Buttons/Ok").GetComponent<Button>();
            cancelButton_ = transform.Find("Buttons/Cancel").GetComponent<Button>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void ShowOptions(string content, UnityAction okAction, UnityAction cancelAction = null)
        {
            Init();
            content_.text = content;

            okButton_.onClick.AddListener(() => { okAction?.Invoke(); Hide(); });
            okButton_.gameObject.SetActive(true);
            cancelButton_.onClick.AddListener(() => { cancelAction?.Invoke(); Hide(); });
            cancelButton_.gameObject.SetActive(true);

            Show(true);
        }

        public void ShowTips(string content)
        {
            Init();
            content_.text = content;

            okButton_.onClick.AddListener(() => { Hide(); });
            okButton_.gameObject.SetActive(true);
            cancelButton_.gameObject.SetActive(false);

            Show(true);
        }
    }
}
