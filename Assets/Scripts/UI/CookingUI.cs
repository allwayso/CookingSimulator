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

        private Action<string, string> onAction;
        private Action onFinish;

        public void Show(RecipeData recipe, DishState state, Action<string, string> actionHandler, Action finishHandler)
        {
            onAction = actionHandler;
            onFinish = finishHandler;
            gameObject.SetActive(true);
            recipeText.text = recipe.name;
            hintText.text = recipe.steps.Length > 0 ? recipe.steps[0].hint : string.Empty;
            messageText.text = string.Empty;
            UpdateState(state);
        }

        public void UpdateState(DishState state)
        {
            stateText.text = $"当前状态：{state}";
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
    }
}
