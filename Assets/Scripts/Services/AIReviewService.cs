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
        private const string LaobaProfilePath = "NPCs/ai_laoba.md";

        public IEnumerator CreateLaobaReview(
            DishData dish,
            RecipeData recipe,
            CookingLog log,
            ReviewData baseReview,
            Action<ReviewData, bool, string> onComplete)
        {
            if (!TryLoadConfig(out var config, out var configError))
            {
                onComplete(CreateFallbackReview(dish, baseReview, configError), false, configError);
                yield break;
            }

            var prompt = BuildPrompt(dish, recipe, log, baseReview, LoadLaobaProfile());
            var requestJson = JsonUtility.ToJson(ChatCompletionRequest.Create(config.model, prompt));
            string responseText = null;
            string requestError = null;
            yield return RunPostRequest(config, requestJson, (response, error) =>
            {
                responseText = response;
                requestError = error;
            });

            if (!string.IsNullOrWhiteSpace(requestError))
            {
                onComplete(CreateFallbackReview(dish, baseReview, requestError), false, requestError);
                yield break;
            }

            if (!TryParseReview(dish, responseText, out var review, out var parseError))
            {
                onComplete(CreateFallbackReview(dish, baseReview, parseError), false, parseError);
                yield break;
            }

            onComplete(review, true, string.Empty);
        }

        public IEnumerator TestConnection(Action<bool, string> onComplete)
        {
            if (!TryLoadConfig(out var config, out var configError))
            {
                onComplete(false, configError);
                yield break;
            }

            var requestJson = JsonUtility.ToJson(ChatCompletionRequest.Create(config.model, "只回复 OK。"));
            var responseText = SendPostRequest(config, requestJson, out var requestError);

            if (!string.IsNullOrWhiteSpace(requestError))
            {
                onComplete(false, requestError);
                yield break;
            }

            onComplete(!string.IsNullOrWhiteSpace(responseText), "AI test response received.");
        }

        public static bool TestConnectionNow(out string message)
        {
            if (!TryLoadConfig(out var config, out var configError))
            {
                message = configError;
                return false;
            }

            var requestJson = JsonUtility.ToJson(ChatCompletionRequest.Create(config.model, "只回复 OK。"));
            var responseText = SendPostRequest(config, requestJson, out var requestError);
            if (!string.IsNullOrWhiteSpace(requestError))
            {
                message = requestError;
                return false;
            }

            message = "AI test response received.";
            return !string.IsNullOrWhiteSpace(responseText);
        }

        private static IEnumerator RunPostRequest(AIReviewConfig config, string requestJson, Action<string, string> onComplete)
        {
            var completed = false;
            string responseText = null;
            string error = null;

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    responseText = SendPostRequest(config, requestJson, out error);
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

        private static string SendPostRequest(AIReviewConfig config, string requestJson, out string error)
        {
            error = null;
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(BuildChatCompletionsUrl(config.baseUrl));
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers["Authorization"] = "Bearer " + config.apiKey;
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

        private static bool TryLoadConfig(out AIReviewConfig config, out string error)
        {
            config = null;
            error = string.Empty;

            foreach (var path in GetConfigPaths())
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                config = JsonUtility.FromJson<AIReviewConfig>(File.ReadAllText(path));
                break;
            }

            if (config == null)
            {
                error = "Missing ai_review.local.json.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.baseUrl))
            {
                config.baseUrl = "https://api.openai.com/v1";
            }

            if (!Uri.IsWellFormedUriString(config.baseUrl, UriKind.Absolute))
            {
                config.baseUrl = "https://api.openai.com/v1";
            }

            if (string.IsNullOrWhiteSpace(config.apiKey) ||
                string.IsNullOrWhiteSpace(config.model))
            {
                error = "AI config must include apiKey and model.";
                return false;
            }

            return true;
        }

        private static IEnumerable<string> GetConfigPaths()
        {
            yield return Path.Combine(Application.dataPath, "..", ConfigFileName);
            yield return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
        }

        private static string LoadLaobaProfile()
        {
            var path = Path.Combine(Application.streamingAssetsPath, LaobaProfilePath);
            return File.Exists(path) ? File.ReadAllText(path) : "你是 AI 老八，负责用挑剔但有用的方式评价菜品。";
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

        private static bool TryParseReview(DishData dish, string responseText, out ReviewData review, out string error)
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

                review = new ReviewData
                {
                    reviewId = Guid.NewGuid().ToString("N"),
                    dishId = dish.dishId,
                    score = Mathf.Clamp(payload.score, 0, 100),
                    summary = "AI 老八评价：" + payload.summary,
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

        private static ReviewData CreateFallbackReview(DishData dish, ReviewData baseReview, string reason)
        {
            return new ReviewData
            {
                reviewId = Guid.NewGuid().ToString("N"),
                dishId = dish.dishId,
                score = baseReview.score,
                summary = $"AI 老八暂时没连上，先按本地规则评价：{baseReview.summary}",
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
