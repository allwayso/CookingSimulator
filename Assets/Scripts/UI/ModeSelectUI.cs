using System;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class ModeSelectUI : MonoBehaviour
    {
        [SerializeField] private Text userInfoText;

        private Action onChefMode;
        private Action onFoodieMode;

        public void Show(UserData user, Action chefModeAction, Action foodieModeAction = null)
        {
            onChefMode = chefModeAction;
            onFoodieMode = foodieModeAction;
            gameObject.SetActive(true);
            userInfoText.text = $"{user.username}, you've still got {user.reputation} reputation points to squander";
        }

        public void EnterChefMode()
        {
            onChefMode?.Invoke();
        }

        public void EnterFoodieMode()
        {
            onFoodieMode?.Invoke();
        }
    }
}
