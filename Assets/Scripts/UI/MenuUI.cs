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
            dishesText.text = "厨神菜单";

            if (dishes.Count == 0)
            {
                return;
            }

            foreach (var dish in dishes)
            {
                CreateDishButton(dish);
            }
        }

        public void ShowReviewers(DishData dish, List<(string profileKey, string displayName)> reviewers, Action backToDishesAction, Action<string> reviewerSelectedAction)
        {
            selectedDish = dish;
            onBackToDishes = backToDishesAction;
            onReviewerSelected = reviewerSelectedAction;
            gameObject.SetActive(true);
            ClearDishButtons();
            SetBackButton(true);
            dishesText.text = $"{dish.name}品鉴名录";

            if (reviewers == null || reviewers.Count == 0)
            {
                dishesText.text = "暂无评审可选";
                return;
            }

            foreach (var (profileKey, displayName) in reviewers)
            {
                CreateReviewerButton(displayName, profileKey);
            }
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

        private void CreateReviewerButton(string displayName, string profileKey)
        {
            var button = Instantiate(dishButtonTemplate, dishesButtonRoot);
            button.gameObject.SetActive(true);
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = displayName;
            }

            button.onClick.AddListener(() => onReviewerSelected?.Invoke(profileKey));
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
