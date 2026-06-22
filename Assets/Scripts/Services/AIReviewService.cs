using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using CookingSimulator.Models;
using UnityEngine;

namespace CookingSimulator.Services
{
    public class AIReviewService : MonoBehaviour
    {
        private const string ConfigFileName = "ai_review.local.json";
        private const string NpcProfilesFolder = "NPCs";

        private static readonly Dictionary<string, string> NpcDisplayNames = new Dictionary<string, string>
        {
            { "laoba", "AI 老八" },
            { "dagongren", "打工人老八" },
            { "zibenjia", "资本家老八" },
            { "baozupo", "包租婆老八" },
            { "meishijia", "美食家老八" },
            { "jianshen", "健身达人老八" },
            { "xiaoxuesheng", "小学生老八" },
            { "yangsheng", "养生专家老八" },
        };

        public static string GetNpcDisplayName(string profileKey)
        {
            if (NpcDisplayNames.TryGetValue(profileKey, out var name))
                return name;
            return string.IsNullOrEmpty(profileKey) ? "Unknown" : char.ToUpper(profileKey[0]) + profileKey.Substring(1);
        }

        public static List<(string profileKey, string displayName)> DiscoverNpcProfiles()
        {
            var result = new List<(string, string)>();
            var npcDir = Path.Combine(Application.streamingAssetsPath, NpcProfilesFolder);
            if (!Directory.Exists(npcDir))
                return result;

            foreach (var path in Directory.GetFiles(npcDir, "*.md"))
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                var profileKey = fileName.StartsWith("ai_") ? fileName.Substring(3) : fileName;
                result.Add((profileKey, GetNpcDisplayName(profileKey)));
            }

            return result;
        }

        public IEnumerator CreateNPCReview(
            DishData dish,
            RecipeData recipe,
            CookingLog log,
            ReviewData baseReview,
            string profileKey,
            Action<ReviewData, bool, string> onComplete)
        {
            if (!TryLoadProviders(out var providers, out var configError))
            {
                onComplete(CreateFallbackReview(dish, baseReview, profileKey, configError), false, configError);
                yield break;
            }

            var prompt = BuildPrompt(dish, recipe, log, baseReview, LoadNpcProfile(profileKey));
            var responseText = string.Empty;
            var requestError = string.Empty;
            yield return RunProviderRequests(providers, prompt, (response, error) =>
            {
                responseText = response;
                requestError = error;
            });

            if (!string.IsNullOrWhiteSpace(requestError))
            {
                onComplete(CreateFallbackReview(dish, baseReview, profileKey, requestError), false, requestError);
                yield break;
            }

            if (!TryParseReview(dish, responseText, profileKey, out var review, out var parseError))
            {
                onComplete(CreateFallbackReview(dish, baseReview, profileKey, parseError), false, parseError);
                yield break;
            }

            onComplete(review, true, string.Empty);
        }

        public IEnumerator CreateLaobaReview(
            DishData dish,
            RecipeData recipe,
            CookingLog log,
            ReviewData baseReview,
            Action<ReviewData, bool, string> onComplete)
        {
            yield return CreateNPCReview(dish, recipe, log, baseReview, "laoba", onComplete);
        }

        public IEnumerator TestConnection(Action<bool, string> onComplete)
        {
            if (!TryLoadProviders(out var providers, out var configError))
            {
                onComplete(false, configError);
                yield break;
            }

            var responseText = string.Empty;
            var requestError = string.Empty;
            yield return RunProviderRequests(providers, "只回复 OK。", (response, error) =>
            {
                responseText = response;
                requestError = error;
            });

            if (!string.IsNullOrWhiteSpace(requestError))
            {
                onComplete(false, requestError);
                yield break;
            }

            onComplete(!string.IsNullOrWhiteSpace(responseText), "AI test response received.");
        }

        public static bool TestConnectionNow(out string message)
        {
            if (!TryLoadProviders(out var providers, out var configError))
            {
                message = configError;
                return false;
            }

            var responseText = string.Empty;
            if (!TrySendWithProviders(providers, "只回复 OK。", out responseText, out var requestError))
            {
                message = requestError;
                return false;
            }

            message = "AI test response received.";
            return !string.IsNullOrWhiteSpace(responseText);
        }

        private static IEnumerator RunProviderRequests(AIReviewProvider[] providers, string prompt, Action<string, string> onComplete)
        {
            var completed = false;
            string responseText = null;
            string error = null;

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    TrySendWithProviders(providers, prompt, out responseText, out error);
                }
                catch (Exception exception)
                {
                    error = "AI request failed: " + exception.Message;
                }
                finally
                {
                    completed = true;
                }
            });

            while (!completed)
            {
                yield return null;
            }

            onComplete(responseText, error);
        }

        private static bool TrySendWithProviders(AIReviewProvider[] providers, string prompt, out string responseText, out string error)
        {
            responseText = null;
            var errors = new StringBuilder();
            foreach (var provider in providers)
            {
                var requestJson = JsonUtility.ToJson(ChatCompletionRequest.Create(provider.model, prompt));
                var result = SendPostRequest(provider, requestJson, out var providerError);
                if (string.IsNullOrWhiteSpace(providerError))
                {
                    responseText = result;
                    error = null;
                    return true;
                }

                if (errors.Length > 0)
                {
                    errors.Append(" | ");
                }

                var name = string.IsNullOrWhiteSpace(provider.name) ? "unnamed" : provider.name;
                errors.Append(name).Append(": ").Append(providerError);
            }

            error = errors.Length == 0 ? "No AI providers configured." : errors.ToString();
            return false;
        }

        private static string SendPostRequest(AIReviewProvider provider, string requestJson, out string error)
        {
            error = null;
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(BuildChatCompletionsUrl(provider.baseUrl));
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers["Authorization"] = "Bearer " + provider.apiKey;
                request.Timeout = 30000;

                var body = Encoding.UTF8.GetBytes(requestJson);
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(body, 0, body.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (WebException exception)
            {
                var status = exception.Response is HttpWebResponse response ? ((int)response.StatusCode).ToString() : "network";
                error = "AI request failed: " + status + " " + exception.Message;
                return null;
            }
            catch (Exception exception)
            {
                error = "AI request failed: " + exception.Message;
                return null;
            }
        }

        private static bool TryLoadProviders(out AIReviewProvider[] providers, out string error)
        {
            providers = null;
            error = string.Empty;

            foreach (var path in GetConfigPaths())
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                var json = File.ReadAllText(path);
                providers = ParseProviders(json);
                break;
            }

            if (providers == null || providers.Length == 0)
            {
                error = "Missing ai_review.local.json.";
                return false;
            }

            for (var index = 0; index < providers.Length; index++)
            {
                var provider = providers[index];
                if (string.IsNullOrWhiteSpace(provider.baseUrl) || !Uri.IsWellFormedUriString(provider.baseUrl, UriKind.Absolute))
                {
                    provider.baseUrl = "https://api.openai.com/v1";
                }

                if (string.IsNullOrWhiteSpace(provider.apiKey) || string.IsNullOrWhiteSpace(provider.model))
                {
                    error = "Every AI provider must include apiKey and model.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(provider.name))
                {
                    provider.name = "provider_" + (index + 1);
                }
            }

            return true;
        }

        private static AIReviewProvider[] ParseProviders(string json)
        {
            var config = JsonUtility.FromJson<AIReviewConfig>(json);
            if (config != null && config.providers != null && config.providers.Length > 0)
            {
                return config.providers;
            }

            var legacy = JsonUtility.FromJson<AIReviewProvider>(json);
            if (legacy != null && !string.IsNullOrWhiteSpace(legacy.apiKey))
            {
                return new[] { legacy };
            }

            return Array.Empty<AIReviewProvider>();
        }

        private static IEnumerable<string> GetConfigPaths()
        {
            yield return Path.Combine(Application.dataPath, "..", ConfigFileName);
            yield return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
        }

        private static string LoadNpcProfile(string profileKey)
        {
            var folder = Path.Combine(Application.streamingAssetsPath, NpcProfilesFolder);
            var path = Path.Combine(folder, profileKey + ".md");
            if (File.Exists(path))
                return File.ReadAllText(path);

            var legacyPath = Path.Combine(folder, "ai_" + profileKey + ".md");
            if (File.Exists(legacyPath))
                return File.ReadAllText(legacyPath);

            var displayName = GetNpcDisplayName(profileKey);
            return $"你是 {displayName}，负责用挑剔但有用的方式评价菜品。";
        }

        private static string BuildChatCompletionsUrl(string baseUrl)
        {
            var trimmed = baseUrl.TrimEnd('/');
            return trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
                ? trimmed
                : trimmed + "/chat/completions";
        }

        private static string BuildPrompt(DishData dish, RecipeData recipe, CookingLog log, ReviewData baseReview, string profile)
        {
            var builder = new StringBuilder();
            builder.AppendLine(profile);
            builder.AppendLine();
            builder.AppendLine("请评价这道菜，只返回 JSON，不要 Markdown。JSON 字段必须是 score, summary, suggestion, reputationDelta。");
            builder.AppendLine($"菜品：{dish.name}，价格：{dish.price}，最终状态：{dish.finalState}");
            builder.AppendLine($"菜谱：{recipe.name} - {recipe.description}");
            builder.AppendLine("标准步骤：");
            foreach (var step in recipe.steps)
            {
                builder.AppendLine($"{step.order}. {step.action} -> {step.target}：{step.hint}");
            }

            builder.AppendLine("玩家操作日志：");
            foreach (var record in log.records)
            {
                builder.AppendLine($"{record.elapsedSeconds:F1}s {record.action} {record.target} {record.stateBefore}->{record.stateAfter}");
            }

            builder.AppendLine($"本地初评：分数 {baseReview.score}，{baseReview.summary}，建议：{baseReview.suggestion}");
            builder.AppendLine("评分范围 0-100，reputationDelta 范围 -5 到 5。");
            return builder.ToString();
        }

        private static bool TryParseReview(DishData dish, string responseText, string profileKey, out ReviewData review, out string error)
        {
            review = null;
            error = string.Empty;

            try
            {
                var response = JsonUtility.FromJson<ChatCompletionResponse>(responseText);
                var content = response?.choices != null && response.choices.Length > 0 ? response.choices[0].message.content : string.Empty;
                var json = ExtractJsonObject(content);
                var payload = JsonUtility.FromJson<AIReviewPayload>(json);
                if (payload == null || string.IsNullOrWhiteSpace(payload.summary))
                {
                    error = "AI response did not include a valid review.";
                    return false;
                }

                var displayName = GetNpcDisplayName(profileKey);
                review = new ReviewData
                {
                    reviewId = Guid.NewGuid().ToString("N"),
                    dishId = dish.dishId,
                    reviewerId = profileKey,
                    score = Mathf.Clamp(payload.score, 0, 100),
                    summary = displayName + "评价：" + payload.summary,
                    suggestion = payload.suggestion,
                    reputationDelta = Mathf.Clamp(payload.reputationDelta, -5, 5),
                    createdAt = DateTime.UtcNow.ToString("O")
                };
                return true;
            }
            catch (Exception exception)
            {
                error = "AI response parse failed: " + exception.Message;
                return false;
            }
        }

        private static string ExtractJsonObject(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "{}";
            }

            var start = value.IndexOf('{');
            var end = value.LastIndexOf('}');
            return start >= 0 && end > start ? value.Substring(start, end - start + 1) : value;
        }

        private static ReviewData CreateFallbackReview(DishData dish, ReviewData baseReview, string profileKey, string reason)
        {
            var displayName = GetNpcDisplayName(profileKey);
            return new ReviewData
            {
                reviewId = Guid.NewGuid().ToString("N"),
                dishId = dish.dishId,
                reviewerId = profileKey,
                score = baseReview.score,
                summary = $"{displayName}暂时没连上，先按本地规则评价：{baseReview.summary}",
                suggestion = baseReview.suggestion + "（AI 降级原因：" + reason + "）",
                reputationDelta = baseReview.reputationDelta,
                createdAt = DateTime.UtcNow.ToString("O")
            };
        }

        [Serializable]
        private class ChatCompletionRequest
        {
            public string model;
            public ChatMessage[] messages;
            public float temperature = 0.7f;

            public static ChatCompletionRequest Create(string model, string prompt)
            {
                return new ChatCompletionRequest
                {
                    model = model,
                    messages = new[]
                    {
                        new ChatMessage { role = "user", content = prompt }
                    }
                };
            }
        }

        [Serializable]
        private class ChatMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        private class ChatCompletionResponse
        {
            public ChatChoice[] choices;
        }

        [Serializable]
        private class ChatChoice
        {
            public ChatMessage message;
        }

        [Serializable]
        private class AIReviewPayload
        {
            public int score;
            public string summary;
            public string suggestion;
            public int reputationDelta;
        }
    }
}
