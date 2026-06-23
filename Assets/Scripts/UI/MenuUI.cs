using System;
using System.Collections.Generic;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class MenuUI : MonoBehaviour
    {
        [SerializeField] private Text dishesText;
        [SerializeField] private Transform dishesButtonRoot;
        [SerializeField] private Button dishButtonTemplate;
        [SerializeField] private Button backButton;

        private Action onCookAgain;

        public void ShowDishes(List<DishData> dishes, Action cookAgainAction)
        {
            onCookAgain = cookAgainAction;
            gameObject.SetActive(true);
            ClearDishButtons();
            dishesText.text = "厨神菜单";

            if (dishes.Count == 0)
                return;

            foreach (var dish in dishes)
                CreateDishButton(dish);
        }

        public void CookAgain()
        {
            onCookAgain?.Invoke();
        }

        public void BackToDishes()
        {
            // 无操作，保留用于预制件兼容
        }

        private void CreateDishButton(DishData dish)
        {
            var button = Instantiate(dishButtonTemplate, dishesButtonRoot);
            button.gameObject.SetActive(true);
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = $"{dish.name}  ￥{dish.price}  评分：{dish.score}";
            }
        }

        private void ClearDishButtons()
        {
            if (dishButtonTemplate != null)
            {
                dishButtonTemplate.gameObject.SetActive(false);
            }

            if (dishesButtonRoot == null)
            {
                return;
            }

            for (var index = dishesButtonRoot.childCount - 1; index >= 0; index--)
            {
                var child = dishesButtonRoot.GetChild(index);
                if (dishButtonTemplate != null && child == dishButtonTemplate.transform)
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
        }

        private void SetBackButton(bool visible)
        {
            if (backButton != null)
            {
                backButton.gameObject.SetActive(visible);
            }
        }
    }
}
