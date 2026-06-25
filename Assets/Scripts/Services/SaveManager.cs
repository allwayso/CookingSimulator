using System;
using System.Collections.Generic;
using System.IO;
using CookingSimulator.Models;
using UnityEngine;

namespace CookingSimulator.Services
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private string RootPath => Path.Combine(Application.persistentDataPath, "Saves");
        private string UsersPath => Path.Combine(RootPath, "Users");
        private string LogsPath => Path.Combine(RootPath, "Logs");
        private string DishesPath => Path.Combine(RootPath, "Dishes");
        private string ReviewsPath => Path.Combine(RootPath, "Reviews");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureDirectories();
        }

        public UserData LoadOrCreateUser(string username)
        {
            EnsureDirectories();
            var userId = SanitizeId(username);
            var path = Path.Combine(UsersPath, userId + ".json");
            if (File.Exists(path))
            {
                var existing = JsonUtility.FromJson<UserData>(File.ReadAllText(path));
                existing.lastLoginAt = DateTime.UtcNow.ToString("O");
                SaveUser(existing);
                return existing;
            }

            var user = new UserData
            {
                userId = userId,
                username = username,
                passwordHash = string.Empty,
                reputation = 0,
                createdAt = DateTime.UtcNow.ToString("O"),
                lastLoginAt = DateTime.UtcNow.ToString("O")
            };
            SaveUser(user);
            return user;
        }

        public void SaveUser(UserData user)
        {
            EnsureDirectories();
            WriteJson(Path.Combine(UsersPath, user.userId + ".json"), user);
        }

        public string SaveLog(CookingLog log)
        {
            EnsureDirectories();
            var path = Path.Combine(LogsPath, log.logId + ".json");
            WriteJson(path, log);
            return path;
        }

        public string SaveReview(ReviewData review)
        {
            EnsureDirectories();
            var path = Path.Combine(ReviewsPath, review.reviewId + ".json");
            WriteJson(path, review);
            return path;
        }

        public ReviewData LoadReview(string reviewId)
        {
            EnsureDirectories();
            if (string.IsNullOrWhiteSpace(reviewId))
            {
                return null;
            }

            var path = Path.Combine(ReviewsPath, reviewId + ".json");
            return File.Exists(path) ? JsonUtility.FromJson<ReviewData>(File.ReadAllText(path)) : null;
        }

        public string SaveDish(DishData dish)
        {
            EnsureDirectories();
            var path = Path.Combine(DishesPath, dish.dishId + ".json");
            WriteJson(path, dish);
            return path;
        }

        public List<DishData> LoadDishes(string userId)
        {
            EnsureDirectories();
            var dishes = new List<DishData>();
            foreach (var path in Directory.GetFiles(DishesPath, "*.json"))
            {
                var dish = JsonUtility.FromJson<DishData>(File.ReadAllText(path));
                if (dish.userId == userId)
                {
                    dishes.Add(dish);
                }
            }

            dishes.Sort((left, right) => string.CompareOrdinal(right.createdAt, left.createdAt));
            return dishes;
        }

        private void EnsureDirectories()
        {
            Directory.CreateDirectory(UsersPath);
            Directory.CreateDirectory(LogsPath);
            Directory.CreateDirectory(DishesPath);
            Directory.CreateDirectory(ReviewsPath);
        }

        private static void WriteJson<T>(string path, T value)
        {
            File.WriteAllText(path, JsonUtility.ToJson(value, true));
        }

        private static string SanitizeId(string value)
        {
            var cleaned = value.Trim().ToLowerInvariant();
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                cleaned = cleaned.Replace(invalid, '_');
            }

            return string.IsNullOrWhiteSpace(cleaned) ? "player" : cleaned;
        }
    }
}
