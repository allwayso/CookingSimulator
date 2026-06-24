using System;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class CookingUI : MonoBehaviour
    {
        [SerializeField] private Text recipeText;
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
        [SerializeField] private Sprite[] tomatoSprites; // [0]=全生 [1]=半生 [2]=全熟 [3]=过头
        [SerializeField] private Sprite[] eggSprites;
        [SerializeField] private Image tomatoPlateImage;  // 番茄在盘上的显示
        [SerializeField] private Image eggPlateImage;     // 鸡蛋在盘上的显示

        [Header("Container Interaction")]
        [SerializeField] private Button panButton;
        [SerializeField] private Button plateButton;

        [Header("Ingredient Selector")]
        [SerializeField] private Button tomatoSelectBtn;
        [SerializeField] private Button eggSelectBtn;
        [SerializeField] private Text selectedIngredientText;

        private Action<string, string> onAction;
        private Action onFinish;
        private Action onTransferToPlate;
        private Action onTransferToPan;
        private Action<int> onFireLevelChanged;

        private string selectedIngredient = "鸡蛋";

        private static readonly string[] FireLevelNames = { "关火", "小火", "中火", "大火", "猛火" };

        public void Show(RecipeData recipe, DishState state,
            Action<string, string> actionHandler, Action finishHandler,
            Action<int> fireLevelHandler,
            Action transferToPlateHandler, Action transferToPanHandler)
        {
            onAction = actionHandler;
            onFinish = finishHandler;
            onFireLevelChanged = fireLevelHandler;
            onTransferToPlate = transferToPlateHandler;
            onTransferToPan = transferToPanHandler;
            gameObject.SetActive(true);
            recipeText.text = recipe.name;
            messageText.text = string.Empty;

            // 默认选中鸡蛋
            selectedIngredient = "鸡蛋";
            UpdateIngredientSelection();

            // 初始化火力滑杆（默认小火）
            if (fireSlider != null)
            {
                fireSlider.value = 1;
                fireSlider.interactable = false;
                UpdateFireLevelText(1);
            }

            // 初始化熟度显示
            ResetDonenessDisplay();

            // 初始化容器交互
            SetContainerButtonsInteractable(false);

            UpdateState(state);
        }

        public void UpdateState(DishState state)
        {
            hintText.text = GetHint(state);

            if (dishStateImage != null)
            {
                dishStateImage.color = GetStateColor(state);
            }

            bool isCooking = state == DishState.Cooking;
            SetFireSliderInteractable(isCooking);
            SetContainerButtonsInteractable(isCooking);
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
            var sprites = GetIngredientSprites(ingredientName);

            if (targetText != null)
                targetText.text = $"{ingredientName}: {GetDonenessName(level)}";

            if (targetImage != null)
            {
                // 有精灵图就用精灵图，否则 fallback 到颜色
                if (sprites != null && sprites.Length > (int)level && sprites[(int)level] != null)
                    targetImage.sprite = sprites[(int)level];
                else
                    targetImage.color = GetDonenessColor(level);
            }

            // 盘图同步
            var plateImage = GetIngredientPlateImage(ingredientName);
            if (plateImage != null)
            {
                if (sprites != null && sprites.Length > (int)level && sprites[(int)level] != null)
                    plateImage.sprite = sprites[(int)level];
                else
                    plateImage.color = GetDonenessColor(level);
            }
        }

        public void SetFireSliderInteractable(bool interactable)
        {
            if (fireSlider != null)
            {
                fireSlider.interactable = interactable;
            }
        }

        public void SetContainerButtonsInteractable(bool interactable)
        {
            if (panButton != null)
                panButton.interactable = interactable;
            if (plateButton != null)
                plateButton.interactable = interactable;
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

        /// <summary>更新容器内容物显示</summary>
        public void UpdateContainerDisplay(bool hasPanContent, bool hasPlateContent)
        {
            if (panButton != null)
            {
                var img = panButton.GetComponent<Image>();
                if (img != null)
                    img.color = hasPanContent ? new Color(0.6f, 0.35f, 0.18f) : new Color(0.25f, 0.2f, 0.15f);
            }

            if (plateButton != null)
            {
                var img = plateButton.GetComponent<Image>();
                if (img != null)
                    img.color = hasPlateContent ? new Color(0.95f, 0.92f, 0.85f) : new Color(0.5f, 0.48f, 0.45f);
            }
        }

        /// <summary>按食材更新图片可见性：未激活→隐藏，锅中→锅图，盘上→盘图</summary>
        public void UpdateIngredientVisibility(string ingredientName, bool hasActivated, bool isInPan)
        {
            var panImg = GetIngredientImage(ingredientName);
            var plateImg = GetIngredientPlateImage(ingredientName);

            if (panImg != null) panImg.gameObject.SetActive(hasActivated && isInPan);
            if (plateImg != null) plateImg.gameObject.SetActive(hasActivated && !isInPan);
        }

        // ── 食材选择 ──

        public void SelectIngredient(string ingredientName)
        {
            selectedIngredient = ingredientName;
            UpdateIngredientSelection();
        }

        public void SelectTomato()
        {
            SelectIngredient("番茄");
        }

        public void SelectEgg()
        {
            SelectIngredient("鸡蛋");
        }

        private void UpdateIngredientSelection()
        {
            if (selectedIngredientText != null)
                selectedIngredientText.text = $"下锅: {selectedIngredient}";

            // 高亮选中按钮
            if (tomatoSelectBtn != null)
            {
                var img = tomatoSelectBtn.GetComponent<Image>();
                if (img != null)
                    img.color = selectedIngredient.Contains("番茄")
                        ? new Color(0.95f, 0.65f, 0.2f)
                        : new Color(0.35f, 0.25f, 0.15f);
            }

            if (eggSelectBtn != null)
            {
                var img = eggSelectBtn.GetComponent<Image>();
                if (img != null)
                    img.color = selectedIngredient.Contains("鸡蛋")
                        ? new Color(0.95f, 0.65f, 0.2f)
                        : new Color(0.35f, 0.25f, 0.15f);
            }
        }

        // ── 容器交互 ──

        public void OnPanClicked()
        {
            onTransferToPlate?.Invoke();
        }

        public void OnPlateClicked()
        {
            onTransferToPan?.Invoke();
        }

        // ── 动作按钮 ──

        public void Cut()
        {
            onAction?.Invoke("cut", "番茄");
        }

        public void PutInPan()
        {
            onAction?.Invoke("put_in_pan", selectedIngredient);
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

        // ── 辅助方法 ──

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

            // 设置初始精灵
            if (tomatoDonenessImage != null)
            {
                if (tomatoSprites != null && tomatoSprites.Length > 0 && tomatoSprites[0] != null)
                    tomatoDonenessImage.sprite = tomatoSprites[0];
                else
                    tomatoDonenessImage.color = GetDonenessColor(DonenessLevel.Raw);
            }
            if (eggDonenessImage != null)
            {
                if (eggSprites != null && eggSprites.Length > 0 && eggSprites[0] != null)
                    eggDonenessImage.sprite = eggSprites[0];
                else
                    eggDonenessImage.color = GetDonenessColor(DonenessLevel.Raw);
            }

            // 全部隐藏，等待下锅后才显示
            HideAllIngredientImages();
        }

        private void HideAllIngredientImages()
        {
            if (tomatoDonenessImage != null) tomatoDonenessImage.gameObject.SetActive(false);
            if (eggDonenessImage != null) eggDonenessImage.gameObject.SetActive(false);
            if (tomatoPlateImage != null)
            {
                if (tomatoSprites != null && tomatoSprites.Length > 0 && tomatoSprites[0] != null)
                    tomatoPlateImage.sprite = tomatoSprites[0];
                tomatoPlateImage.gameObject.SetActive(false);
            }
            if (eggPlateImage != null)
            {
                if (eggSprites != null && eggSprites.Length > 0 && eggSprites[0] != null)
                    eggPlateImage.sprite = eggSprites[0];
                eggPlateImage.gameObject.SetActive(false);
            }
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

        private Sprite[] GetIngredientSprites(string ingredientName)
        {
            if (ingredientName.Contains("番茄"))
                return tomatoSprites;
            if (ingredientName.Contains("鸡蛋"))
                return eggSprites;
            return null;
        }

        private Image GetIngredientPlateImage(string ingredientName)
        {
            if (ingredientName.Contains("番茄"))
                return tomatoPlateImage;
            if (ingredientName.Contains("鸡蛋"))
                return eggPlateImage;
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
                    return new Color(0.82f, 0.25f, 0.2f);
                case DonenessLevel.HalfCooked:
                    return new Color(0.95f, 0.65f, 0.2f);
                case DonenessLevel.FullyCooked:
                    return new Color(0.28f, 0.68f, 0.34f);
                case DonenessLevel.Overcooked:
                    return new Color(0.3f, 0.15f, 0.1f);
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
                    return "当前步骤：选择食材后点击下锅。";
                case DishState.Cooking:
                    return "调节火力控制熟度。点击锅盛出到盘子，点击盘子倒回锅中。";
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
