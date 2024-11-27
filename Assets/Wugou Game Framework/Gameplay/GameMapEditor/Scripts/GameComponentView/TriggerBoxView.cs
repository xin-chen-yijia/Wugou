using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Wugou.Editor.UI
{
    public class TriggerBoxView : GameComponentView<TriggerBox>
    {
        public TMP_Dropdown actionDropdown;
        public TMP_Dropdown responseDropdown;
        public TMP_InputField targetNameInput;
        public TMP_InputField targetMethodInput;
        public TMP_InputField targetParameterInput;
        public TMP_InputField messageInput;

        public GameObject popupWnd;
        public GameObject methodWnd;

        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();

            actionDropdown.onValueChanged.AddListener((val) =>
            {
                gameComponent.triggerType = (TriggerBox.TriggerType)val;
            });

            responseDropdown.onValueChanged.AddListener((val) =>
            {
                gameComponent.responseType = (TriggerBox.ResponseType)val;

                HandleTriggerPage();
            });

            targetNameInput.onValueChanged.AddListener((val) =>
            {
                gameComponent.targetName = val;
            });

            targetMethodInput.onValueChanged.AddListener((val) =>
            {
                gameComponent.targetMethod = val;
            });

            targetParameterInput.onValueChanged.AddListener((val) =>
            {
                gameComponent.targetParameter = val;
            });

            messageInput.onValueChanged.AddListener((val) =>
            {
                gameComponent.message = val;
            });

        }

        public override void OnNewTarget(GameObject target)
        {
            base.OnNewTarget(target);
            actionDropdown.SetValueWithoutNotify((int)gameComponent.triggerType);
            responseDropdown.SetValueWithoutNotify((int)gameComponent.responseType);
            targetNameInput.SetTextWithoutNotify(gameComponent.targetName);
            targetMethodInput.SetTextWithoutNotify(gameComponent.targetMethod);
            targetParameterInput.SetTextWithoutNotify(gameComponent.targetParameter);
            messageInput.SetTextWithoutNotify(gameComponent.message);

            HandleTriggerPage();
        }

        void HandleTriggerPage()
        {
            popupWnd.SetActive(gameComponent.responseType == TriggerBox.ResponseType.kPopup);
            methodWnd.SetActive(gameComponent.responseType == TriggerBox.ResponseType.kMethod);
        }
    }
}
