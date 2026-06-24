using System;
using System.Collections.Generic;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    /// <summary>
    /// 备菜选菜面板：食材多选 → 集合匹配菜谱 → 确认回调。
    /// MVP 阶段目前仅有番茄和鸡蛋两种食材，匹配 tomato_egg 菜谱。
    /// </summary>
    public class 备菜选菜UI : MonoBehaviour
    {
        [Header("UI 引用")]
        [SerializeField] private Text titleText;
        [SerializeField] private Transform ingredientButtonRoot;
        [SerializeField] private Button ingredientButtonTemplate;
        [SerializeField] private Text selectedDisplayText;
        [SerializeField] private Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private HashSet<string> selectedIngredients = new HashSet<string>();
        private Dictionary<string, Button> ingredientButtons = new Dictionary<string, Button>();
        private Dictionary<string, Color> originalColors = new Dictionary<string, Color>();

        private string[] currentIngredients;
        private List<RecipeData> currentRecipes;

        private Action<RecipeData> onConfirm;
        private Action onCancel;

        private static readonly Color HighlightColor = new Color(0.94f, 0.76f, 0.38f, 1f); // 金色高亮

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(Confirm);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(Cancel);
        }

        /// <summary>
        /// 显示备菜选菜面板
        /// </summary>
        /// <param name="ingredients">所有可用食材（去重后的列表）</param>
        /// <param name="allRecipes">所有菜谱，用于匹配</param>
        /// <param name="confirmCallback">匹配成功后的回调</param>
        /// <param name="cancelCallback">取消时的回调</param>
        public void Show(string[] ingredients, List<RecipeData> allRecipes,
            Action<RecipeData> confirmCallback, Action cancelCallback)
        {
            currentIngredients = ingredients;
            currentRecipes = allRecipes;
            onConfirm = confirmCallback;
            onCancel = cancelCallback;

            selectedIngredients.Clear();
            gameObject.SetActive(true);

            BuildIngredientButtons();
            UpdateSelectedDisplay();

            if (messageText != null)
                messageText.text = string.Empty;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 确认备菜 —— 按已选食材集合匹配菜谱
        /// </summary>
        public void Confirm()
        {
            if (selectedIngredients.Count == 0)
            {
                ShowMessage("请至少选择一种食材");
                return;
            }

            var matched = MatchRecipe();
            if (matched != null)
            {
                ShowMessage("备菜完成");
                onConfirm?.Invoke(matched);
            }
            else
            {
                ShowMessage("无法匹配菜谱，请重新选择");
            }
        }

        /// <summary>
        /// 取消选菜，返回自由走动
        /// </summary>
        public void Cancel()
        {
            onCancel?.Invoke();
        }

        /// <summary>
        /// 清空旧按钮，重新按食材列表生成 Toggle 风格按钮
        /// </summary>
        private void BuildIngredientButtons()
        {
            // 销毁旧按钮
            foreach (var kvp in ingredientButtons)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            ingredientButtons.Clear();
            originalColors.Clear();

            if (ingredientButtonTemplate != null)
                ingredientButtonTemplate.gameObject.SetActive(false);

            if (ingredientButtonRoot == null || ingredientButtonTemplate == null)
                return;

            foreach (var ingredient in currentIngredients)
            {
                var button = Instantiate(ingredientButtonTemplate, ingredientButtonRoot);
                button.gameObject.SetActive(true);

                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = ingredient;

                // 保存原始颜色
                var image = button.GetComponent<Image>();
                if (image != null)
                    originalColors[ingredient] = image.color;

                // 点击切换选中状态
                string captured = ingredient; // 闭包捕获
                button.onClick.AddListener(() => OnIngredientClicked(captured, button));

                ingredientButtons[ingredient] = button;
            }
        }

        private void OnIngredientClicked(string ingredient, Button button)
        {
            if (selectedIngredients.Contains(ingredient))
            {
                selectedIngredients.Remove(ingredient);
                var image = button.GetComponent<Image>();
                if (image != null && originalColors.TryGetValue(ingredient, out var origColor))
                    image.color = origColor;
            }
            else
            {
                selectedIngredients.Add(ingredient);
                var image = button.GetComponent<Image>();
                if (image != null)
                    image.color = HighlightColor;
            }

            UpdateSelectedDisplay();
            if (messageText != null)
                messageText.text = string.Empty;
        }

        private void UpdateSelectedDisplay()
        {
            if (selectedDisplayText == null) return;

            if (selectedIngredients.Count == 0)
                selectedDisplayText.text = "已选：无";
            else
                selectedDisplayText.text = "已选：" + string.Join("、", selectedIngredients);
        }

        private void ShowMessage(string msg)
        {
            if (messageText != null)
                messageText.text = msg;
        }

        /// <summary>
        /// 集合匹配：已选食材集合 完全等于 菜谱的 ingredients 集合
        /// </summary>
        private RecipeData MatchRecipe()
        {
            if (currentRecipes == null) return null;

            var selectedSet = new HashSet<string>(selectedIngredients);
            foreach (var recipe in currentRecipes)
            {
                if (recipe.ingredients == null) continue;
                var recipeSet = new HashSet<string>(recipe.ingredients);
                if (selectedSet.SetEquals(recipeSet))
                    return recipe;
            }
            return null;
        }
    }
}
