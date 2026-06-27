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
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text pageText;

        private const int PageSize = 5;

        private Action onCookAgain;
        private Action<DishData> onDishClicked;
        private Action onBack;
        private List<DishData> currentDishes;
        private int currentPage;
        private int totalPages;

        public void ShowDishes(List<DishData> dishes, Action cookAgainAction, Action<DishData> dishClickedAction,
            string title = "厨神菜单", Action backAction = null)
        {
            onCookAgain = cookAgainAction;
            onDishClicked = dishClickedAction;
            onBack = backAction;
            currentDishes = dishes;
            currentPage = 0;
            gameObject.SetActive(true);
            dishesText.text = title;

            if (dishes.Count == 0)
            {
                if (pageText != null) pageText.text = "0 / 0";
                if (prevButton != null) prevButton.interactable = false;
                if (nextButton != null) nextButton.interactable = false;
                return;
            }

            RefreshPage();
        }

        public void PrevPage()
        {
            if (currentPage > 0)
            {
                currentPage--;
                RefreshPage();
            }
        }

        public void NextPage()
        {
            if (currentPage < totalPages - 1)
            {
                currentPage++;
                RefreshPage();
            }
        }

        private void RefreshPage()
        {
            ClearDishButtons();

            totalPages = Mathf.Max(1, Mathf.CeilToInt((float)currentDishes.Count / PageSize));
            if (currentPage >= totalPages) currentPage = totalPages - 1;

            int start = currentPage * PageSize;
            int end = Mathf.Min(start + PageSize, currentDishes.Count);

            for (var i = start; i < end; i++)
                CreateDishButton(currentDishes[i]);

            if (pageText != null)
                pageText.text = $"{currentPage + 1} / {totalPages}";
            if (prevButton != null)
                prevButton.interactable = currentPage > 0;
            if (nextButton != null)
                nextButton.interactable = currentPage < totalPages - 1;
        }

        public void CookAgain()
        {
            onCookAgain?.Invoke();
        }

        public void BackToDishes()
        {
            onBack?.Invoke();
        }

        private void CreateDishButton(DishData dish)
        {
            var button = Instantiate(dishButtonTemplate, dishesButtonRoot);
            button.gameObject.SetActive(true);
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                var dateStr = "";
                if (!string.IsNullOrWhiteSpace(dish.createdAt) && DateTime.TryParse(dish.createdAt, out var dt))
                    dateStr = dt.ToString("MM/dd HH:mm");
                label.text = $"{dish.name}  ￥{dish.price}  评分：{dish.score}  {dateStr}";
            }

            button.onClick.AddListener(() => onDishClicked?.Invoke(dish));
        }

        private void ClearDishButtons()
        {
            if (dishButtonTemplate != null)
                dishButtonTemplate.gameObject.SetActive(false);

            if (dishesButtonRoot == null)
                return;

            for (var index = dishesButtonRoot.childCount - 1; index >= 0; index--)
            {
                var child = dishesButtonRoot.GetChild(index);
                if (dishButtonTemplate != null && child == dishButtonTemplate.transform)
                    continue;

                Destroy(child.gameObject);
            }
        }
    }
}
