using System;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class ReviewUI : MonoBehaviour
    {
        [SerializeField] private Text reviewText;

        private Action onContinue;

        public void Show(ReviewData review, Action continueAction)
        {
            onContinue = continueAction;
            gameObject.SetActive(true);
            reviewText.text = $"评分：{review.score}\n{review.summary}\n建议：{review.suggestion}\n声望变化：{review.reputationDelta}";
        }

        public void Continue()
        {
            onContinue?.Invoke();
        }
    }
}
