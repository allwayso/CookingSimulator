using System;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class LoginUI : MonoBehaviour
    {
        [SerializeField] private InputField usernameInput;
        [SerializeField] private Text messageText;

        private Action<string> onLogin;

        public void Show(Action<string> loginAction)
        {
            onLogin = loginAction;
            gameObject.SetActive(true);
            messageText.text = string.Empty;
        }

        public void Submit()
        {
            var username = usernameInput.text.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                messageText.text = "请输入用户名";
                return;
            }

            onLogin?.Invoke(username);
        }
    }
}
