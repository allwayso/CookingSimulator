using CookingSimulator.Core;
using CookingSimulator.Services;
using CookingSimulator.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookingSimulator.Editor
{
    public static class MvpSceneBuilder
    {
        [MenuItem("Cooking Simulator/Build MVP Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MVP";

            CreateCamera();
            var canvas = CreateCanvas();
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            var services = new GameObject("Services");
            var saveManager = services.AddComponent<SaveManager>();
            var recipeManager = services.AddComponent<RecipeManager>();
            var logManager = services.AddComponent<LogManager>();
            var reviewManager = services.AddComponent<ReviewManager>();

            var login = CreateLoginPanel(canvas.transform);
            var mode = CreateModePanel(canvas.transform);
            var recipe = CreateRecipePanel(canvas.transform);
            var cooking = CreateCookingPanel(canvas.transform);
            var review = CreateReviewPanel(canvas.transform);
            var saveDish = CreateSaveDishPanel(canvas.transform);
            var menu = CreateMenuPanel(canvas.transform);

            var managerObject = new GameObject("GameManager");
            var gameManager = managerObject.AddComponent<GameManager>();
            Assign(gameManager, "saveManager", saveManager);
            Assign(gameManager, "recipeManager", recipeManager);
            Assign(gameManager, "logManager", logManager);
            Assign(gameManager, "reviewManager", reviewManager);
            Assign(gameManager, "loginUI", login);
            Assign(gameManager, "modeSelectUI", mode);
            Assign(gameManager, "recipeSelectUI", recipe);
            Assign(gameManager, "cookingUI", cooking);
            Assign(gameManager, "reviewUI", review);
            Assign(gameManager, "saveDishUI", saveDish);
            Assign(gameManager, "menuUI", menu);

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MVP.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/MVP.unity", true) };
        }

        private static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.09f);
            camera.orthographic = true;
            camera.orthographicSize = 5;
        }

        private static LoginUI CreateLoginPanel(Transform parent)
        {
            var panel = CreatePanel<LoginUI>(parent, "LoginPanel");
            CreateTitle(panel.transform, "胡闹厨房 MVP");
            var input = CreateInput(panel.transform, "用户名");
            var message = CreateText(panel.transform, string.Empty);
            var button = CreateButton(panel.transform, "进入游戏");
            Assign(panel, "usernameInput", input);
            Assign(panel, "messageText", message);
            UnityEventTools.AddPersistentListener(button.onClick, panel.Submit);
            return panel;
        }

        private static ModeSelectUI CreateModePanel(Transform parent)
        {
            var panel = CreatePanel<ModeSelectUI>(parent, "ModePanel");
            var userInfo = CreateTitle(panel.transform, "模式选择");
            var chefButton = CreateButton(panel.transform, "厨神模式");
            var locked = CreateText(panel.transform, "老八模式：MVP 暂未开放");
            Assign(panel, "userInfoText", userInfo);
            Assign(panel, "lockedText", locked);
            UnityEventTools.AddPersistentListener(chefButton.onClick, panel.EnterChefMode);
            return panel;
        }

        private static RecipeSelectUI CreateRecipePanel(Transform parent)
        {
            var panel = CreatePanel<RecipeSelectUI>(parent, "RecipePanel");
            var text = CreateTitle(panel.transform, "菜谱");
            var button = CreateButton(panel.transform, "开始做菜");
            Assign(panel, "recipeText", text);
            UnityEventTools.AddPersistentListener(button.onClick, panel.SelectFirstRecipe);
            return panel;
        }

        private static CookingUI CreateCookingPanel(Transform parent)
        {
            var panel = CreatePanel<CookingUI>(parent, "CookingPanel");
            var recipe = CreateTitle(panel.transform, "做菜");
            CreateKitchenVisual(panel.transform, out var dishStateImage);
            var state = CreateText(panel.transform, "当前状态");
            var hint = CreateText(panel.transform, "提示");
            var message = CreateText(panel.transform, string.Empty);
            Assign(panel, "recipeText", recipe);
            Assign(panel, "stateText", state);
            Assign(panel, "hintText", hint);
            Assign(panel, "messageText", message);
            Assign(panel, "dishStateImage", dishStateImage);
            UnityEventTools.AddPersistentListener(CreateButton(panel.transform, "切菜").onClick, panel.Cut);
            UnityEventTools.AddPersistentListener(CreateButton(panel.transform, "下锅").onClick, panel.PutInPan);
            UnityEventTools.AddPersistentListener(CreateButton(panel.transform, "加热").onClick, panel.Heat);
            UnityEventTools.AddPersistentListener(CreateButton(panel.transform, "加调料").onClick, panel.Season);
            UnityEventTools.AddPersistentListener(CreateButton(panel.transform, "翻炒").onClick, panel.Stir);
            UnityEventTools.AddPersistentListener(CreateButton(panel.transform, "出锅").onClick, panel.Finish);
            return panel;
        }

        private static void CreateKitchenVisual(Transform parent, out Image dishStateImage)
        {
            var stage = new GameObject("KitchenVisual", typeof(RectTransform));
            stage.transform.SetParent(parent, false);
            SetPreferredSize(stage, 760, 220);
            var background = stage.AddComponent<Image>();
            background.color = new Color(0.22f, 0.2f, 0.17f);

            CreateVisualBlock(stage.transform, "BackWall", new Vector2(0, 58), new Vector2(720, 90), new Color(0.55f, 0.5f, 0.42f));
            CreateVisualBlock(stage.transform, "Counter", new Vector2(0, -52), new Vector2(720, 70), new Color(0.35f, 0.27f, 0.2f));
            CreateVisualBlock(stage.transform, "Pan", new Vector2(0, -20), new Vector2(230, 86), new Color(0.08f, 0.09f, 0.09f));
            CreateVisualBlock(stage.transform, "PanInner", new Vector2(0, -20), new Vector2(176, 48), new Color(0.16f, 0.17f, 0.16f));
            CreateVisualBlock(stage.transform, "Tomato", new Vector2(-250, -30), new Vector2(78, 58), new Color(0.82f, 0.25f, 0.2f));
            CreateVisualBlock(stage.transform, "Egg", new Vector2(-165, -30), new Vector2(74, 54), new Color(0.95f, 0.88f, 0.56f));
            CreateVisualBlock(stage.transform, "Seasoning", new Vector2(258, -22), new Vector2(58, 96), new Color(0.9f, 0.92f, 0.88f));
            CreateVisualBlock(stage.transform, "Flame", new Vector2(0, -84), new Vector2(120, 24), new Color(0.95f, 0.42f, 0.18f));

            dishStateImage = CreateVisualBlock(stage.transform, "DishStateColor", new Vector2(0, -20), new Vector2(118, 38), new Color(0.82f, 0.25f, 0.2f));
        }

        private static Image CreateVisualBlock(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = obj.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static ReviewUI CreateReviewPanel(Transform parent)
        {
            var panel = CreatePanel<ReviewUI>(parent, "ReviewPanel");
            var text = CreateTitle(panel.transform, "评价");
            var button = CreateButton(panel.transform, "保存菜品");
            Assign(panel, "reviewText", text);
            UnityEventTools.AddPersistentListener(button.onClick, panel.Continue);
            return panel;
        }

        private static SaveDishUI CreateSaveDishPanel(Transform parent)
        {
            var panel = CreatePanel<SaveDishUI>(parent, "SaveDishPanel");
            CreateTitle(panel.transform, "保存菜品");
            var nameInput = CreateInput(panel.transform, "菜名");
            var priceInput = CreateInput(panel.transform, "价格");
            var message = CreateText(panel.transform, string.Empty);
            var button = CreateButton(panel.transform, "保存到食单");
            Assign(panel, "dishNameInput", nameInput);
            Assign(panel, "priceInput", priceInput);
            Assign(panel, "messageText", message);
            UnityEventTools.AddPersistentListener(button.onClick, panel.Submit);
            return panel;
        }

        private static MenuUI CreateMenuPanel(Transform parent)
        {
            var panel = CreatePanel<MenuUI>(parent, "MenuPanel");
            var text = CreateTitle(panel.transform, "我的食单");
            var button = CreateButton(panel.transform, "再做一道");
            Assign(panel, "dishesText", text);
            UnityEventTools.AddPersistentListener(button.onClick, panel.CookAgain);
            return panel;
        }

        private static T CreatePanel<T>(Transform parent, string name) where T : MonoBehaviour
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(80, 60);
            rect.offsetMax = new Vector2(-80, -60);
            var image = panel.AddComponent<Image>();
            image.color = new Color(0.12f, 0.12f, 0.12f, 0.92f);
            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 32, 32);
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.UpperCenter;
            return panel.AddComponent<T>();
        }

        private static Text CreateTitle(Transform parent, string text)
        {
            var label = CreateText(parent, text);
            label.fontSize = 32;
            SetPreferredSize(label.gameObject, 760, 64);
            return label;
        }

        private static Text CreateText(Transform parent, string text)
        {
            var obj = new GameObject("Text", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var label = obj.AddComponent<Text>();
            label.text = text;
            label.fontSize = 24;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            SetPreferredSize(obj, 760, 44);
            return label;
        }

        private static InputField CreateInput(Transform parent, string placeholder)
        {
            var obj = new GameObject(placeholder + "Input", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            SetPreferredSize(obj, 420, 48);
            var image = obj.AddComponent<Image>();
            image.color = Color.white;
            var input = obj.AddComponent<InputField>();
            var text = CreateText(obj.transform, string.Empty);
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleLeft;
            StretchChild(text.gameObject, 14, 8, -14, -8);
            var hint = CreateText(obj.transform, placeholder);
            hint.color = Color.gray;
            hint.alignment = TextAnchor.MiddleLeft;
            StretchChild(hint.gameObject, 14, 8, -14, -8);
            input.textComponent = text;
            input.placeholder = hint;
            return input;
        }

        private static Button CreateButton(Transform parent, string text)
        {
            var obj = new GameObject(text + "Button", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            SetPreferredSize(obj, 260, 48);
            var image = obj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.45f, 0.8f);
            var button = obj.AddComponent<Button>();
            var label = CreateText(obj.transform, text);
            label.fontSize = 24;
            StretchChild(label.gameObject, 0, 0, 0, 0);
            return button;
        }

        private static void SetPreferredSize(GameObject obj, float width, float height)
        {
            var rect = obj.GetComponent<RectTransform>() ?? obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            var layout = obj.GetComponent<LayoutElement>() ?? obj.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            layout.minHeight = height;
        }

        private static void StretchChild(GameObject obj, float left, float top, float right, float bottom)
        {
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, -top);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private static void Assign(Object target, string fieldName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(fieldName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
