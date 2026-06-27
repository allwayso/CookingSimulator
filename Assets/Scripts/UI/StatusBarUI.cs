using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class StatusBarUI : MonoBehaviour
    {
        [SerializeField] private Text statusText;

        public void Show(UserData user)
        {
            gameObject.SetActive(true);
            Refresh(user);
        }

        public void Refresh(UserData user)
        {
            if (statusText == null || user == null)
            {
                return;
            }

            statusText.text = $"厨神：{user.username}  声望：{user.reputation}";
        }

        public void ShowFoodie(UserData user)
        {
            gameObject.SetActive(true);
            RefreshFoodie(user);
        }

        public void RefreshFoodie(UserData user)
        {
            if (statusText == null || user == null)
            {
                return;
            }

            statusText.text = $"美食家：{user.username}  生命值：{user.lifeValue}";
        }
    }
}
