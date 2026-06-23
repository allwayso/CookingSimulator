using System;
using System.Collections.Generic;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class ReviewUI : MonoBehaviour
    {
        [SerializeField] private Text reviewText;
        [SerializeField] private Text continueButtonText;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text pageIndicator;

        private Action onContinue;
        private List<ReviewData> reviews = new List<ReviewData>();
        private int currentIndex;

        public void Show(ReviewData review, Action continueAction)
        {
            ShowMultiple(new List<ReviewData> { review }, continueAction, "保存菜品");
        }

        public void Show(ReviewData review, Action continueAction, string continueText)
        {
            ShowMultiple(new List<ReviewData> { review }, continueAction, continueText);
        }

        public void ShowMultiple(List<ReviewData> allReviews, Action continueAction, string continueText = "返回食单")
        {
            onContinue = continueAction;
            reviews = allReviews ?? new List<ReviewData>();
            currentIndex = 0;
            gameObject.SetActive(true);

            if (continueButtonText != null)
                continueButtonText.text = continueText;

            DisplayCurrent();
        }

        public void PrevReview()
        {
            if (reviews.Count <= 1) return;
            currentIndex = (currentIndex - 1 + reviews.Count) % reviews.Count;
            DisplayCurrent();
        }

        public void NextReview()
        {
            if (reviews.Count <= 1) return;
            currentIndex = (currentIndex + 1) % reviews.Count;
            DisplayCurrent();
        }

        public void Continue()
        {
            onContinue?.Invoke();
        }

        private void DisplayCurrent()
        {
            if (reviews.Count == 0 || reviewText == null) return;

            var review = reviews[currentIndex];
            reviewText.text =
                $"评分：{review.score}\n\n" +
                $"评价：\n{review.summary}\n\n" +
                $"建议：\n{review.suggestion}\n\n" +
                $"声望变化：{review.reputationDelta}";

            if (pageIndicator != null)
                pageIndicator.text = $"{currentIndex + 1}/{reviews.Count}";

            if (prevButton != null) prevButton.gameObject.SetActive(reviews.Count > 1);
            if (nextButton != null) nextButton.gameObject.SetActive(reviews.Count > 1);
        }
    }
}
