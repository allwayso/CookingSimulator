using System;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class CookingUI : MonoBehaviour
    {
        [SerializeField] private Text recipeText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text hintText;
        [SerializeField] private Text messageText;
        [SerializeField] private Image dishStateImage;

        [Header("Fire Control")]
        [SerializeField] private Slider fireSlider;
        [SerializeField] private Text fireLevelText;

        [Header("Timer")]
        [SerializeField] private Text timerText;

        [Header("Ingredient Doneness")]
        [SerializeField] private Image tomatoDonenessImage;
        [SerializeField] private Image eggDonenessImage;
        [SerializeField] private Text tomatoDonenessText;
        [SerializeField] private Text eggDonenessText;

        private Action<string, string> onAction;
        private Action onFinish;
        private Action<int> onFireLevelChanged;

        private static readonly string[] FireLevelNames = { "关火", "小火", "中火", "大火", "猛火" };

        public void Show(RecipeData recipe, DishState state,
            Action<string, string> actionHandler, Action finishHandler,
            Action<int> fireLevelHandler)
        {
            onAction = actionHandler;
            onFinish = finishHandler;
            onFireLevelChanged = fireLevelHandler;
            gameObject.SetActive(true);
            recipeText.text = recipe.name;
            messageText.text = string.Empty;

            // 初始化火力滑杆
            if (fireSlider != null)
            {
                fireSlider.value = 0;
                fireSlider.interactable = false;
                UpdateFireLevelText(0);
            }

            // 初始化熟度显示
            ResetDonenessDisplay();

            UpdateState(state);
        }

        public void UpdateState(DishState state)
        {
            stateText.text = $"当前状态：{state}";
            hintText.text = GetHint(state);

            if (dishStateImage != null)
            {
                dishStateImage.color = GetStateColor(state);
            }

            // 仅在 Cooking 状态下可调节火力
            SetFireSliderInteractable(state == DishState.Cooking);
        }

        public void ShowMessage(string message)
        {
            messageText.text = message;
        }

        public void OnFireSliderChanged(float value)
        {
            int level = Mathf.RoundToInt(value);
            UpdateFireLevelText(level);
            onFireLevelChanged?.Invoke(level);
        }

        public void UpdateIngredientDoneness(string ingredientName, DonenessLevel level)
        {
            var targetImage = GetIngredientImage(ingredientName);
            var targetText = GetIngredientText(ingredientName);

            // 更新熟度文字
            if (targetText != null)
            {
                targetText.text = $"{ingredientName}: {GetDonenessName(level)}";
            }

            // 更新熟度贴图颜色（临时方案，后续替换为贴图切换）
            if (targetImage != null)
            {
                targetImage.color = GetDonenessColor(level);
            }
        }

        public void SetFireSliderInteractable(bool interactable)
        {
            if (fireSlider != null)
            {
                fireSlider.interactable = interactable;
            }
        }

        public void UpdateTimer(float elapsedSeconds)
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(elapsedSeconds / 60f);
                int seconds = Mathf.FloorToInt(elapsedSeconds % 60f);
                timerText.text = $"⏱ {minutes:00}:{seconds:00}";
            }
        }

        public void Cut()
        {
            onAction?.Invoke("cut", "番茄");
        }

        public void PutInPan()
        {
            onAction?.Invoke("put_in_pan", "食材");
        }

        public void Season()
        {
            onAction?.Invoke("season", "盐");
        }

        public void Stir()
        {
            onAction?.Invoke("stir", "锅");
        }

        public void Finish()
        {
            onFinish?.Invoke();
        }

        private void UpdateFireLevelText(int level)
        {
            if (fireLevelText != null)
            {
                fireLevelText.text = FireLevelNames[Mathf.Clamp(level, 0, FireLevelNames.Length - 1)];
            }
        }

        private void ResetDonenessDisplay()
        {
            if (tomatoDonenessText != null)
                tomatoDonenessText.text = "番茄: 全生";
            if (eggDonenessText != null)
                eggDonenessText.text = "鸡蛋: 全生";
            if (tomatoDonenessImage != null)
                tomatoDonenessImage.color = GetDonenessColor(DonenessLevel.Raw);
            if (eggDonenessImage != null)
                eggDonenessImage.color = GetDonenessColor(DonenessLevel.Raw);
        }

        private Image GetIngredientImage(string ingredientName)
        {
            if (ingredientName.Contains("番茄"))
                return tomatoDonenessImage;
            if (ingredientName.Contains("鸡蛋"))
                return eggDonenessImage;
            return null;
        }

        private Text GetIngredientText(string ingredientName)
        {
            if (ingredientName.Contains("番茄"))
                return tomatoDonenessText;
            if (ingredientName.Contains("鸡蛋"))
                return eggDonenessText;
            return null;
        }

        private static string GetDonenessName(DonenessLevel level)
        {
            switch (level)
            {
                case DonenessLevel.Raw:
                    return "全生";
                case DonenessLevel.HalfCooked:
                    return "半生";
                case DonenessLevel.FullyCooked:
                    return "全熟";
                case DonenessLevel.Overcooked:
                    return "过头";
                default:
                    return "未知";
            }
        }

        private static Color GetDonenessColor(DonenessLevel level)
        {
            switch (level)
            {
                case DonenessLevel.Raw:
                    return new Color(0.82f, 0.25f, 0.2f);       // 红色 — 生
                case DonenessLevel.HalfCooked:
                    return new Color(0.95f, 0.65f, 0.2f);      // 黄色 — 半生
                case DonenessLevel.FullyCooked:
                    return new Color(0.28f, 0.68f, 0.34f);     // 绿色 — 全熟
                case DonenessLevel.Overcooked:
                    return new Color(0.3f, 0.15f, 0.1f);       // 深褐 — 过头
                default:
                    return Color.white;
            }
        }

        private static string GetHint(DishState state)
        {
            switch (state)
            {
                case DishState.Raw:
                    return "当前步骤：先切菜。";
                case DishState.Cut:
                    return "当前步骤：把食材下锅。";
                case DishState.Cooking:
                    return "当前步骤：调节火力控制熟度，番茄和鸡蛋熟得不一样快哦。";
                case DishState.Seasoned:
                    return "当前步骤：翻炒均匀后出锅。";
                case DishState.Done:
                    return "当前步骤：菜品完成，进入评价。";
                default:
                    return string.Empty;
            }
        }

        private static Color GetStateColor(DishState state)
        {
            switch (state)
            {
                case DishState.Raw:
                    return new Color(0.82f, 0.25f, 0.2f);
                case DishState.Cut:
                    return new Color(0.95f, 0.46f, 0.25f);
                case DishState.Cooking:
                    return new Color(0.95f, 0.65f, 0.2f);
                case DishState.Seasoned:
                    return new Color(0.28f, 0.68f, 0.34f);
                case DishState.Done:
                    return new Color(0.95f, 0.88f, 0.56f);
                default:
                    return Color.white;
            }
        }
    }
}
