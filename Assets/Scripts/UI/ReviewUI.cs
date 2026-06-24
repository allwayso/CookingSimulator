using System;
using System.Collections.Generic;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class ReviewUI : MonoBehaviour
    {
        [SerializeField] private Text npcNameText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Text suggestionText;
        [SerializeField] private Text reputationText;
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
            if (reviews.Count == 0) return;

            var review = reviews[currentIndex];

            if (npcNameText != null)
                npcNameText.text = review.reviewerName ?? "评价者";
            if (scoreText != null)
                scoreText.text = $"评分：{review.score}";
            if (summaryText != null)
                summaryText.text = review.summary;
            if (suggestionText != null)
                suggestionText.text = $"建议：{review.suggestion}";
            if (reputationText != null)
                reputationText.text = $"声望 {review.reputationDelta:+0;-#}";

            if (pageIndicator != null)
                pageIndicator.text = $"{currentIndex + 1}/{reviews.Count}";

            if (prevButton != null) prevButton.gameObject.SetActive(reviews.Count > 1);
            if (nextButton != null) nextButton.gameObject.SetActive(reviews.Count > 1);
        }
    }
}
