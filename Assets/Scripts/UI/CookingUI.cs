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

        private Action<string, string> onAction;
        private Action onFinish;

        public void Show(RecipeData recipe, DishState state, Action<string, string> actionHandler, Action finishHandler)
        {
            onAction = actionHandler;
            onFinish = finishHandler;
            gameObject.SetActive(true);
            recipeText.text = recipe.name;
            messageText.text = string.Empty;
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
        }

        public void ShowMessage(string message)
        {
            messageText.text = message;
        }

        public void Cut()
        {
            onAction?.Invoke("cut", "番茄");
        }

        public void PutInPan()
        {
            onAction?.Invoke("put_in_pan", "食材");
        }

        public void Heat()
        {
            onAction?.Invoke("heat", "锅");
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

        private static string GetHint(DishState state)
        {
            switch (state)
            {
                case DishState.Raw:
                    return "当前步骤：先切菜。";
                case DishState.Cut:
                    return "当前步骤：把食材下锅。";
                case DishState.Cooking:
                    return "当前步骤：加热后加入调料。";
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
