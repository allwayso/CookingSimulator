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

        private Action onCookAgain;
        private Action<DishData> onDishSelected;

        public void Show(List<DishData> dishes, Action cookAgainAction, Action<DishData> dishSelectedAction)
        {
            onCookAgain = cookAgainAction;
            onDishSelected = dishSelectedAction;
            gameObject.SetActive(true);
            ClearDishButtons();

            if (dishes.Count == 0)
            {
                dishesText.text = "食单为空";
                return;
            }

            dishesText.text = "选择一道菜，查看 AI 老八评价";
            foreach (var dish in dishes)
            {
                CreateDishButton(dish);
            }
        }

        public void CookAgain()
        {
            onCookAgain?.Invoke();
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
    }
}
