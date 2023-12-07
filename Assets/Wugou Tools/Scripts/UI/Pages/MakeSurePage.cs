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
        public Button okButton1_;
        public Button cancelButton1_;
        public TMP_Text content_;

        public Button okButton2_;

        private bool initialized_ = false;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        //void Update()
        //{

        //}

        public void ShowOptions(string content, UnityAction okAction, UnityAction cancelAction = null, string okLabel = "确定", string cancelLabel = "取消")
        {
             content_.text = content;

            okButton1_.transform.parent.gameObject.SetActive(true);
            okButton2_.transform.parent.gameObject.SetActive(false);

            okButton1_.onClick.RemoveAllListeners();
            okButton1_.GetComponentInChildren<TMP_Text>().text = okLabel;
            okButton1_.onClick.AddListener(() => { okAction?.Invoke(); Hide(); });
            okButton1_.gameObject.SetActive(true);

            cancelButton1_.onClick.RemoveAllListeners();
            cancelButton1_.GetComponentInChildren<TMP_Text>().text = cancelLabel;
            cancelButton1_.onClick.AddListener(() => { cancelAction?.Invoke(); Hide(); });
            cancelButton1_.gameObject.SetActive(true);

            Show(true);
        }

        public void ShowTips(string content, System.Action onOk = null)
        {
            content_.text = content;

            okButton1_.transform.parent.gameObject.SetActive(false);
            okButton2_.transform.parent.gameObject.SetActive(true);

            okButton2_.onClick.RemoveAllListeners();
            okButton2_.GetComponentInChildren<TMP_Text>().text = "确定";
            okButton2_.onClick.AddListener(() => { onOk?.Invoke(); Hide(); });
            okButton2_.gameObject.SetActive(true);

            Show(true);
        }
    }
}
