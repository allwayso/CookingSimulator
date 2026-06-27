using System;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class ReviewWriteUI : MonoBehaviour
    {
        [SerializeField] private Text dishNameText;
        [SerializeField] private InputField scoreInput;
        [SerializeField] private InputField commentInput;
        [SerializeField] private Text messageText;

        private Action<int, string> onSubmit;
        private Action onCancel;

        public void Show(DishData dish, Action<int, string> submitAction, Action cancelAction)
        {
            onSubmit = submitAction;
            onCancel = cancelAction;
            gameObject.SetActive(true);
            dishNameText.text = $"品尝：{dish.name}";
            scoreInput.text = "";
            commentInput.text = "";
            messageText.text = "";
        }

        public void Submit()
        {
            if (!int.TryParse(scoreInput.text, out var score) || score < 0 || score > 100)
            {
                messageText.text = "请输入 0-100 的评分";
                return;
            }

            if (string.IsNullOrWhiteSpace(commentInput.text))
            {
                messageText.text = "请输入评价内容";
                return;
            }

            onSubmit?.Invoke(score, commentInput.text);
        }

        public void Cancel()
        {
            onCancel?.Invoke();
        }
    }
}
