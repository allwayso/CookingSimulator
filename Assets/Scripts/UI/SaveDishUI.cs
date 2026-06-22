using System;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class SaveDishUI : MonoBehaviour
    {
        [SerializeField] private InputField dishNameInput;
        [SerializeField] private InputField priceInput;
        [SerializeField] private Text messageText;

        private Action<string, float> onSave;

        public void Show(ReviewData review, Action<string, float> saveAction)
        {
            onSave = saveAction;
            gameObject.SetActive(true);
            dishNameInput.text = "我的番茄炒蛋";
            priceInput.text = "18";
            messageText.text = string.Empty;
        }

        public void Submit()
        {
            var dishName = dishNameInput.text.Trim();
            if (string.IsNullOrWhiteSpace(dishName))
            {
                messageText.text = "请输入菜名";
                return;
            }

            if (!float.TryParse(priceInput.text, out var price) || price < 0)
            {
                messageText.text = "请输入合法价格";
                return;
            }

            onSave?.Invoke(dishName, price);
        }
    }
}
