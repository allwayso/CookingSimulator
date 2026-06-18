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
        private Action onBackToDishes;
        private Action<DishData> onDishSelected;
        private Action<string> onReviewerSelected;
        private DishData selectedDish;

        public void ShowDishes(List<DishData> dishes, Action cookAgainAction, Action<DishData> dishSelectedAction)
        {
            onCookAgain = cookAgainAction;
            onDishSelected = dishSelectedAction;
            gameObject.SetActive(true);
            ClearDishButtons();
            SetBackButton(false);

            if (dishes.Count == 0)
            {
                dishesText.text = "食单为空";
                return;
            }

            dishesText.text = "选择一道菜";
            foreach (var dish in dishes)
            {
                CreateDishButton(dish);
            }
        }

        public void ShowReviewers(DishData dish, Action backToDishesAction, Action<string> reviewerSelectedAction)
        {
            selectedDish = dish;
            onBackToDishes = backToDishesAction;
            onReviewerSelected = reviewerSelectedAction;
            gameObject.SetActive(true);
            ClearDishButtons();
            SetBackButton(true);
            dishesText.text = $"选择评价者：{dish.name}";
            CreateReviewerButton("AI 老八");
        }

        public void CookAgain()
        {
            onCookAgain?.Invoke();
        }

        public void BackToDishes()
        {
            onBackToDishes?.Invoke();
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

            button.onClick.AddListener(() => onDishSelected?.Invoke(dish));
        }

        private void CreateReviewerButton(string reviewerName)
        {
            var button = Instantiate(dishButtonTemplate, dishesButtonRoot);
            button.gameObject.SetActive(true);
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = reviewerName;
            }

            button.onClick.AddListener(() => onReviewerSelected?.Invoke(reviewerName));
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
