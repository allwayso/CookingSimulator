using System;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class ReviewUI : MonoBehaviour
    {
        [SerializeField] private Text reviewText;
        [SerializeField] private Text continueButtonText;

        private Action onContinue;

        public void Show(ReviewData review, Action continueAction)
        {
            Show(review, continueAction, "保存菜品");
        }

        public void Show(ReviewData review, Action continueAction, string continueText)
        {
            onContinue = continueAction;
            gameObject.SetActive(true);
            reviewText.text = $"评分：{review.score}\n{review.summary}\n建议：{review.suggestion}\n声望变化：{review.reputationDelta}";
            if (continueButtonText != null)
            {
                continueButtonText.text = continueText;
            }
        }

        public void Continue()
        {
            onContinue?.Invoke();
        }
    }
}
