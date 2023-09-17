using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Wugou.Examples
{
    public class LoginPage : MonoBehaviour
    {
        public TMP_InputField nameInput;
        public TMP_InputField pwdInput;
        public TMP_Text tipsLabel;

        public Button loginButton;

        // Start is called before the first frame update
        void Start()
        {
            loginButton.onClick.AddListener(() =>
            {
                if (string.IsNullOrEmpty(nameInput.text))
                {
                    tipsLabel.text = "«ÎÃÓ–¥ÕÍ’˚£°";
                    tipsLabel.gameObject.SetActive(true);
                    return;
                }

                var user = Authorization.Find(nameInput.text, pwdInput.text);
                if (user == null)
                {
                    tipsLabel.gameObject.SetActive(true);
                }
                else
                {
                    //Hide();

                    GamePlay.loginInfo = new Authorization.User()
                    {
                        name = nameInput.text,
                        role = Authorization.Role.kRoleAdmin,
                    };
                    SceneManager.LoadScene(GamePlay.kMainSceneName);
                }
            });
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
