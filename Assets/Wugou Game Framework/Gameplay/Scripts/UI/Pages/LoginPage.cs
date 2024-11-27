using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Wugou.UI
{
    public class LoginPage : MonoBehaviour
    {
        public TMP_InputField nameInput;
        public TMP_InputField pwdInput;
        public TMP_Text tipsLabel;

        public Button loginButton;
        public Button quitButton;

        public UnityEvent onLogin = new UnityEvent();

        // Start is called before the first frame update
        void Start()
        {
            loginButton.onClick.AddListener(async () =>
            {
                if (string.IsNullOrEmpty(nameInput.text))
                {
                    tipsLabel.text = "ÇëÌîÐ´ÓÃ»§Ãû£¡";
                    tipsLabel.gameObject.SetActive(true);
                    return;
                }

                var succ = await Authorization.Login(nameInput.text, pwdInput.text);
                if (!succ)
                {
                    tipsLabel.gameObject.SetActive(true);
                }
                else
                {
                    onLogin.Invoke();
                    SceneManager.LoadScene(GameConsole.settings.clienScene);
                }
            });


            quitButton.onClick.AddListener(() => {
                Application.Quit(); 
            });
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
