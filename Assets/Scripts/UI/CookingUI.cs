using System;
using System.Collections;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class CookingUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Text recipeText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text hintText;
        [SerializeField] private Text messageText;
        [SerializeField] private Image dishStateImage;

        [Header("Timer")]
        [SerializeField] private Text timerText;

        [Header("Popup")]
        [SerializeField] private GameObject popupOverlay;
        [SerializeField] private Text popupQuestionText;
        [SerializeField] private Button popupYesButton;

        [Header("Actions")]
        [SerializeField] private Transform actionButtonRow;

        private Action<string, string> onAction;
        private Action onFinish;

        // Timed mode state
        private Coroutine timedRoutine;
        private float timerStartTime;
        private float[] popupDelays;
        private int currentPopupIndex;
        private bool isTimedMode;
        private DishState currentDisplayedState;

        private Action<string, string> onTimedPopupAction;
        private Action<string, string> onTimedMissedAction;
        private Action onTimedComplete;

        private static readonly string[] TimedActions   = { "heat", "season", "stir", "finish" };
        private static readonly string[] TimedTargets   = { "锅",   "盐",     "锅",   "菜品" };
        private static readonly string[] TimedQuestions = { "是否加热？", "是否加调料？", "是否翻炒？", "是否出锅？" };

        // ── Public API ──

        public void Show(RecipeData recipe, DishState state,
            Action<string, string> actionHandler, Action finishHandler)
        {
            onAction = actionHandler;
            onFinish = finishHandler;
            gameObject.SetActive(true);
            recipeText.text = recipe.name;
            messageText.text = string.Empty;

            SetActionButtonsVisible(true);
            isTimedMode = false;

            // For timed recipes, hide buttons 2-5 (加热/加调料/翻炒/出锅)
            if (recipe.timedPopupDelays != null && recipe.timedPopupDelays.Length == 4)
            {
                for (int i = 2; i <= 5; i++)
                    SetActionButtonVisible(i, false);
            }

            if (timerText != null) timerText.gameObject.SetActive(false);
            if (popupOverlay != null) popupOverlay.SetActive(false);

            UpdateState(state);
        }

        public void UpdateState(DishState state)
        {
            currentDisplayedState = state;
            stateText.text = $"当前状态：{state}";
            hintText.text = GetHint(state, isTimedMode);
            if (dishStateImage != null)
                dishStateImage.color = GetStateColor(state);
        }

        public void ShowMessage(string msg)
        {
            if (messageText != null) messageText.text = msg;
        }

        // ── Timed mode entry ──

        public void StartTimedCooking(float[] delays,
            Action<string, string> popupActionHandler,
            Action<string, string> missedActionHandler,
            Action timedCompleteHandler)
        {
            popupDelays = delays;
            onTimedPopupAction = popupActionHandler;
            onTimedMissedAction = missedActionHandler;
            onTimedComplete = timedCompleteHandler;
            isTimedMode = true;

            SetActionButtonsVisible(false);

            if (timerText != null) timerText.gameObject.SetActive(true);
            timerText.text = "烹饪时间：0.00s";

            UpdateState(currentDisplayedState);
            timedRoutine = StartCoroutine(RunTimedCooking());
        }

        // ── Manual action buttons (guarded in timed mode) ──

        public void Cut()
        {
            if (!isTimedMode) onAction?.Invoke("cut", "番茄");
        }

        public void PutInPan()
        {
            if (!isTimedMode) onAction?.Invoke("put_in_pan", "食材");
        }

        public void Heat()
        {
            if (!isTimedMode) onAction?.Invoke("heat", "锅");
        }

        public void Season()
        {
            if (!isTimedMode) onAction?.Invoke("season", "盐");
        }

        public void Stir()
        {
            if (!isTimedMode) onAction?.Invoke("stir", "锅");
        }

        public void Finish()
        {
            if (!isTimedMode) onFinish?.Invoke();
        }

        // ── Timed cooking coroutines ──

        private IEnumerator RunTimedCooking()
        {
            timerStartTime = Time.time;
            currentPopupIndex = 0;

            while (currentPopupIndex < popupDelays.Length)
            {
                float elapsed = Time.time - timerStartTime;
                UpdateTimerDisplay(elapsed);

                if (elapsed >= popupDelays[currentPopupIndex])
                {
                    yield return StartCoroutine(ShowPopupRoutine(currentPopupIndex));
                    currentPopupIndex++;
                }

                yield return null;
            }

            HideTimerAndPopup();
            onTimedComplete?.Invoke();
        }

        private IEnumerator ShowPopupRoutine(int index)
        {
            if (index >= TimedActions.Length) yield break;

            string actionName = TimedActions[index];
            string targetName = TimedTargets[index];
            string question   = TimedQuestions[index];

            if (popupOverlay != null)
            {
                popupOverlay.SetActive(true);
                if (popupQuestionText != null) popupQuestionText.text = question;
            }

            bool clickedYes = false;

            UnityAction yesHandler = null;
            yesHandler = () =>
            {
                clickedYes = true;
                if (popupYesButton != null) popupYesButton.onClick.RemoveListener(yesHandler);
            };

            if (popupYesButton != null)
            {
                popupYesButton.onClick.RemoveAllListeners();
                popupYesButton.onClick.AddListener(yesHandler);
            }

            // 3-second window
            float popupStart = Time.time;
            while (Time.time - popupStart < 3f && !clickedYes)
            {
                UpdateTimerDisplay(Time.time - timerStartTime);
                yield return null;
            }

            if (popupOverlay != null) popupOverlay.SetActive(false);
            if (popupYesButton != null) popupYesButton.onClick.RemoveAllListeners();

            if (clickedYes)
                onTimedPopupAction?.Invoke(actionName, targetName);
            else
                onTimedMissedAction?.Invoke(actionName, targetName);
        }

        // ── Helpers ──

        private void UpdateTimerDisplay(float elapsed)
        {
            if (timerText == null) return;
            int sec = Mathf.FloorToInt(elapsed);
            int ms  = Mathf.FloorToInt((elapsed - sec) * 100);
            timerText.text = $"烹饪时间：{sec}.{ms:D2}s";
        }

        private void HideTimerAndPopup()
        {
            if (timerText != null) timerText.gameObject.SetActive(false);
            if (popupOverlay != null) popupOverlay.SetActive(false);
        }

        private void SetActionButtonsVisible(bool visible)
        {
            if (actionButtonRow == null) return;
            for (int i = 0; i < actionButtonRow.childCount; i++)
                actionButtonRow.GetChild(i).gameObject.SetActive(visible);
        }

        private void SetActionButtonVisible(int index, bool visible)
        {
            if (actionButtonRow == null || index < 0 || index >= actionButtonRow.childCount) return;
            actionButtonRow.GetChild(index).gameObject.SetActive(visible);
        }

        private static string GetHint(DishState state, bool timed)
        {
            if (timed)
            {
                switch (state)
                {
                    case DishState.Raw:      return "先切菜，再下锅。";
                    case DishState.Cut:      return "点击\"下锅\"开始计时烹饪！";
                    case DishState.Cooking:  return "注意弹窗提示，在3秒内点击\"是\"！";
                    case DishState.Seasoned: return "注意弹窗提示，在3秒内点击\"是\"！";
                    case DishState.Done:     return "菜品完成，进入评价。";
                    default: return string.Empty;
                }
            }

            switch (state)
            {
                case DishState.Raw:      return "当前步骤：先切菜。";
                case DishState.Cut:      return "当前步骤：把食材下锅。";
                case DishState.Cooking:  return "当前步骤：加热后加入调料。";
                case DishState.Seasoned: return "当前步骤：翻炒均匀后出锅。";
                case DishState.Done:     return "当前步骤：菜品完成，进入评价。";
                default: return string.Empty;
            }
        }

        private static Color GetStateColor(DishState state)
        {
            switch (state)
            {
                case DishState.Raw:      return new Color(0.82f, 0.25f, 0.2f);
                case DishState.Cut:      return new Color(0.95f, 0.46f, 0.25f);
                case DishState.Cooking:  return new Color(0.95f, 0.65f, 0.2f);
                case DishState.Seasoned: return new Color(0.28f, 0.68f, 0.34f);
                case DishState.Done:     return new Color(0.95f, 0.88f, 0.56f);
                default: return Color.white;
            }
        }
    }
}
