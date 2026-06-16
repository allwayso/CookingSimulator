using System;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class ModeSelectUI : MonoBehaviour
    {
        [SerializeField] private Text userInfoText;
        [SerializeField] private Text lockedText;

        private Action onChefMode;

        public void Show(UserData user, Action chefModeAction)
        {
            onChefMode = chefModeAction;
            gameObject.SetActive(true);
            userInfoText.text = $"{user.username}  声望：{user.reputation}";
            lockedText.text = "老八模式：MVP 暂未开放";
        }

        public void EnterChefMode()
        {
            onChefMode?.Invoke();
        }
    }
}
