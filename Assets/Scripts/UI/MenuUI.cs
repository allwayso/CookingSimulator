using System;
using System.Collections.Generic;
using System.Text;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class MenuUI : MonoBehaviour
    {
        [SerializeField] private Text dishesText;

        private Action onCookAgain;

        public void Show(List<DishData> dishes, Action cookAgainAction)
        {
            onCookAgain = cookAgainAction;
            gameObject.SetActive(true);

            if (dishes.Count == 0)
            {
                dishesText.text = "食单为空";
                return;
            }

            var builder = new StringBuilder();
            foreach (var dish in dishes)
            {
                builder.AppendLine($"{dish.name}  ￥{dish.price}  评分：{dish.score}  状态：{dish.finalState}");
            }

            dishesText.text = builder.ToString();
        }

        public void CookAgain()
        {
            onCookAgain?.Invoke();
        }
    }
}
