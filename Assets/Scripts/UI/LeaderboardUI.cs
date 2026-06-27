using System;
using System.Collections.Generic;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class LeaderboardUI : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Transform entryRoot;
        [SerializeField] private Button entryTemplate;
        [SerializeField] private Button chefTabButton;
        [SerializeField] private Button foodieTabButton;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text pageText;

        private const int PageSize = 5;

        private List<UserData> allUsers;
        private Action onBack;
        private int currentPage;
        private int totalPages;
        private Func<UserData, int> currentScoreSelector;

        public void Show(List<UserData> users, Action backAction)
        {
            allUsers = users;
            onBack = backAction;
            gameObject.SetActive(true);
            SwitchToChef();
        }

        public void SwitchToChef()
        {
            currentPage = 0;
            DisplayRanking("厨神声望榜", u => u.reputation);
        }

        public void SwitchToFoodie()
        {
            currentPage = 0;
            DisplayRanking("美食家血量榜", u => u.lifeValue);
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

        public void Back()
        {
            onBack?.Invoke();
        }

        private void DisplayRanking(string title, Func<UserData, int> scoreSelector)
        {
            titleText.text = title;
            currentScoreSelector = scoreSelector;
            RefreshPage();
        }

        private void RefreshPage()
        {
            ClearEntries();

            var sorted = new List<UserData>(allUsers);
            sorted.Sort((a, b) => currentScoreSelector(b).CompareTo(currentScoreSelector(a)));

            totalPages = Mathf.Max(1, Mathf.CeilToInt((float)sorted.Count / PageSize));
            if (currentPage >= totalPages) currentPage = totalPages - 1;

            int start = currentPage * PageSize;
            int end = Mathf.Min(start + PageSize, sorted.Count);

            for (var i = start; i < end; i++)
            {
                var entry = Instantiate(entryTemplate, entryRoot);
                entry.gameObject.SetActive(true);
                var label = entry.GetComponentInChildren<Text>();
                if (label != null)
                {
                    var score = currentScoreSelector(sorted[i]);
                    label.text = $"#{i + 1}  {sorted[i].username}  —  {score}";
                }
            }

            pageText.text = $"{currentPage + 1} / {totalPages}";
            prevButton.interactable = currentPage > 0;
            nextButton.interactable = currentPage < totalPages - 1;
        }

        private void ClearEntries()
        {
            if (entryRoot == null)
                return;

            for (var i = entryRoot.childCount - 1; i >= 0; i--)
            {
                var child = entryRoot.GetChild(i);
                if (entryTemplate != null && child == entryTemplate.transform)
                    continue;
                Destroy(child.gameObject);
            }
        }
    }
}
