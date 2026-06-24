using CookingSimulator.Core;
using CookingSimulator.Services;
using CookingSimulator.UI;
using System;
using System.IO;
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

            var loginBackground = CreateLoginBackground();
            var character = CreateCharacter();
            var fridge = CreateFridge(character != null ? character.transform : null);
            var stove = CreateStove(character != null ? character.transform : null);
            CreateCamera();
            var canvas = CreateCanvas();
            var interactionPrompt = CreateInteractionPrompt(canvas);
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            var services = new GameObject("Services");
            var saveManager = services.AddComponent<SaveManager>();
            var recipeManager = services.AddComponent<RecipeManager>();
            var logManager = services.AddComponent<LogManager>();
            var reviewManager = services.AddComponent<ReviewManager>();
            var aiReviewService = services.AddComponent<AIReviewService>();

            var login = CreateLoginPanel(canvas.transform);
            var mode = CreateModePanel(canvas.transform);
            var recipe = CreateRecipePanel(canvas.transform);
            var cooking = CreateCookingPanel(canvas.transform);
            var review = CreateReviewPanel(canvas.transform);
            var saveDish = CreateSaveDishPanel(canvas.transform);
            var menu = CreateMenuPanel(canvas.transform);
            var statusBar = CreateStatusBar(canvas.transform);
            var ingredientSelect = CreateIngredientSelectPanel(canvas.transform);
            ingredientSelect.gameObject.SetActive(false);

            // 交互管理器（默认关闭，FreeRoam 时激活）
            var interactionManagerObj = new GameObject("InteractionManager");
            interactionManagerObj.SetActive(false);
            var interactionManager = interactionManagerObj.AddComponent<交互管理>();
            Assign(interactionManager, "player", character != null ? character.transform : null);
            Assign(interactionManager, "interactionPrompt", interactionPrompt);

            // 设置 promptText（从 InteractionPrompt 子对象中获取）
            var promptTextTransform = interactionPrompt.transform.Find("PromptText");
            if (promptTextTransform != null)
            {
                var promptText = promptTextTransform.GetComponent<Text>();
                Assign(interactionManager, "promptText", promptText);
            }

            var managerObject = new GameObject("GameManager");
            var gameManager = managerObject.AddComponent<GameManager>();
            Assign(gameManager, "saveManager", saveManager);
            Assign(gameManager, "recipeManager", recipeManager);
            Assign(gameManager, "logManager", logManager);
            Assign(gameManager, "reviewManager", reviewManager);
            Assign(gameManager, "aiReviewService", aiReviewService);
            Assign(gameManager, "loginUI", login);
            Assign(gameManager, "modeSelectUI", mode);
            Assign(gameManager, "recipeSelectUI", recipe);
            Assign(gameManager, "cookingUI", cooking);
            Assign(gameManager, "reviewUI", review);
            Assign(gameManager, "saveDishUI", saveDish);
            Assign(gameManager, "menuUI", menu);
            Assign(gameManager, "statusBarUI", statusBar);
            Assign(gameManager, "loginBackgroundRoot", loginBackground);
            Assign(gameManager, "interactionManager", interactionManager);
            Assign(gameManager, "ingredientSelectUI", ingredientSelect);
            Assign(gameManager, "playerObject", character);

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MVP.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/MVP.unity", true) };
        }

        [MenuItem("Cooking Simulator/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            const string scenePath = "Assets/Scenes/MVP.unity";
            const string outputPath = "Builds/Windows/CookingSimulator.exe";

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

            var report = BuildPipeline.BuildPlayer(
                new[] { scenePath },
                outputPath,
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows build failed: {report.summary.result}");
            }
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
            camera.orthographicSize = 7.5f;
        }

        private static GameObject CreateLoginBackground()
        {
            const string prefabPath = "Assets/kitchen/LoginBackgroundGrid.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return null;
            }

            var background = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            background.name = "LoginBackgroundRoot";
            return background;
        }

        private static GameObject CreateCharacter()
        {
            const string spriteSheetPath = "Assets/character/sprite sheet init.png";
            var subSprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath);

            Sprite FindSprite(string name)
            {
                foreach (var s in subSprites)
                {
                    if (s != null && s.name == name) return s as Sprite;
                }
                return null;
            }

            var idleSprite   = FindSprite("sprite sheet init_15");
            var walkFrame12  = FindSprite("sprite sheet init_12");
            var walkFrame13  = FindSprite("sprite sheet init_13");
            var walkFrame14  = FindSprite("sprite sheet init_14");

            var character = new GameObject("小人");
            character.transform.position = new Vector3(-4.5f, -1.06f, 0f);
            character.transform.localScale = new Vector3(8f, 8f, 1f);

            var sr = character.AddComponent<SpriteRenderer>();
            sr.sprite = idleSprite;

            var move = character.AddComponent<人物移动>();
            var moveSO = new SerializedObject(move);
            moveSO.FindProperty("moveSpeed").floatValue = 5f;
            moveSO.FindProperty("characterSize").vector2Value = new Vector2(0.16f, 0.23f);

            var walkFramesProp = moveSO.FindProperty("walkFrames");
            walkFramesProp.arraySize = 4;
            walkFramesProp.GetArrayElementAtIndex(0).objectReferenceValue = walkFrame12;
            walkFramesProp.GetArrayElementAtIndex(1).objectReferenceValue = walkFrame13;
            walkFramesProp.GetArrayElementAtIndex(2).objectReferenceValue = walkFrame14;
            walkFramesProp.GetArrayElementAtIndex(3).objectReferenceValue = idleSprite;
            moveSO.FindProperty("idleSprite").objectReferenceValue = idleSprite;
            moveSO.FindProperty("walkFrameDuration").floatValue = 0.15f;
            moveSO.ApplyModifiedPropertiesWithoutUndo();

            return character;
        }

        private static GameObject CreateFridge(Transform playerTransform)
        {
            var fridge = new GameObject("Fridge 1 _0");
            fridge.transform.position = new Vector3(-0.08f, -0.36f, 0f);
            fridge.transform.localScale = new Vector3(6.25f, 6.25f, 1f);

            var frame0 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/kitchen/Fridge 1 _sprites/Fridge 1 _000.png");
            var frame1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/kitchen/Fridge 1 _sprites/Fridge 1 _001.png");
            var frame2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/kitchen/Fridge 1 _sprites/Fridge 1 _002.png");
            var frame3 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/kitchen/Fridge 1 _sprites/Fridge 1 _003.png");
            var frame4 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/kitchen/Fridge 1 _sprites/Fridge 1 _004.png");

            var sr = fridge.AddComponent<SpriteRenderer>();
            sr.sprite = frame0;

            var anim = fridge.AddComponent<冰箱动画>();
            var animSO = new SerializedObject(anim);
            animSO.FindProperty("player").objectReferenceValue = playerTransform;
            animSO.FindProperty("triggerDistance").floatValue = 4f;
            animSO.FindProperty("frameDuration").floatValue = 0.12f;

            var framesProp = animSO.FindProperty("frames");
            framesProp.arraySize = 5;
            framesProp.GetArrayElementAtIndex(0).objectReferenceValue = frame0;
            framesProp.GetArrayElementAtIndex(1).objectReferenceValue = frame1;
            framesProp.GetArrayElementAtIndex(2).objectReferenceValue = frame2;
            framesProp.GetArrayElementAtIndex(3).objectReferenceValue = frame3;
            framesProp.GetArrayElementAtIndex(4).objectReferenceValue = frame4;
            animSO.ApplyModifiedPropertiesWithoutUndo();

            // 添加交互物组件（与冰箱动画共用相同的 triggerDistance 和 bodyOffset）
            var interactable = fridge.AddComponent<交互物>();
            var intSO = new SerializedObject(interactable);
            intSO.FindProperty("triggerDistance").floatValue = 4f;
            intSO.FindProperty("bodyOffset").vector3Value = new Vector3(-0.175f, 0f, 0f);
            intSO.FindProperty("promptMessage").stringValue = "按F选菜";
            intSO.FindProperty("interactionType").enumValueIndex = (int)InteractionType.Fridge;
            intSO.ApplyModifiedPropertiesWithoutUndo();

            return fridge;
        }

        private static GameObject CreateStove(Transform playerTransform)
        {
            var stove = new GameObject("灶台");
            stove.transform.position = new Vector3(7f, 2.76f, 0f);
            stove.transform.localScale = new Vector3(5.0f, 5.0f, 1f);

            var stoveSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/kitchen/StoveCutouts/灶台2.png");

            var sr = stove.AddComponent<SpriteRenderer>();
            sr.sprite = stoveSprite;

            // 灶台动画（静态精灵）
            var anim = stove.AddComponent<灶台动画>();
            var animSO = new SerializedObject(anim);
            animSO.FindProperty("stoveSprite").objectReferenceValue = stoveSprite;
            animSO.ApplyModifiedPropertiesWithoutUndo();

            // 交互物组件
            var interactable = stove.AddComponent<交互物>();
            var intSO = new SerializedObject(interactable);
            intSO.FindProperty("triggerDistance").floatValue = 0.2f;
            intSO.FindProperty("bodyOffset").vector3Value = Vector3.zero;
            intSO.FindProperty("promptMessage").stringValue = "按F做菜";
            intSO.FindProperty("interactionType").enumValueIndex = (int)InteractionType.Stove;
            intSO.ApplyModifiedPropertiesWithoutUndo();

            return stove;
        }

        private static GameObject CreateInteractionPrompt(Canvas canvas)
        {
            var prompt = new GameObject("InteractionPrompt", typeof(RectTransform));
            prompt.transform.SetParent(canvas.transform, false);

            var rect = prompt.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);

            var image = prompt.AddComponent<Image>();
            image.color = new Color(0.08f, 0.07f, 0.06f, 0.85f);

            var outline = prompt.AddComponent<Outline>();
            outline.effectColor = new Color(0.94f, 0.76f, 0.38f, 1f);
            outline.effectDistance = new Vector2(2, -2);

            var ignoreLayout = prompt.AddComponent<LayoutElement>();
            ignoreLayout.ignoreLayout = true;

            var textObj = new GameObject("PromptText", typeof(RectTransform));
            textObj.transform.SetParent(prompt.transform, false);
            var label = textObj.AddComponent<Text>();
            label.text = string.Empty;
            label.fontSize = 20;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            StretchChild(textObj, 4, 4, -4, -4);

            prompt.SetActive(false);
            return prompt;
        }

        private static 备菜选菜UI CreateIngredientSelectPanel(Transform parent)
        {
            var panel = CreatePanel<备菜选菜UI>(parent, "IngredientSelectPanel");

            var title = CreateTitle(panel.transform, "备菜选菜");

            var ingredientRoot = new GameObject("IngredientButtonColumn", typeof(RectTransform));
            ingredientRoot.transform.SetParent(panel.transform, false);
            SetPreferredSize(ingredientRoot, 760, 200);
            var layout = ingredientRoot.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;

            var template = CreateButton(ingredientRoot.transform, "食材");
            template.gameObject.SetActive(false);

            var selectedText = CreateText(panel.transform, "已选：无");
            selectedText.fontSize = 22;

            var messageText = CreateText(panel.transform, string.Empty);
            messageText.fontSize = 18;

            var confirmButton = CreateButton(panel.transform, "确认备菜", 200);
            var cancelButton = CreateButton(panel.transform, "返回", 200);

            Assign(panel, "titleText", title);
            Assign(panel, "ingredientButtonRoot", ingredientRoot.transform);
            Assign(panel, "ingredientButtonTemplate", template);
            Assign(panel, "selectedDisplayText", selectedText);
            Assign(panel, "messageText", messageText);
            Assign(panel, "confirmButton", confirmButton);
            Assign(panel, "cancelButton", cancelButton);

            UnityEventTools.AddPersistentListener(confirmButton.onClick, panel.Confirm);
            UnityEventTools.AddPersistentListener(cancelButton.onClick, panel.Cancel);

            return panel;
        }

        private static LoginUI CreateLoginPanel(Transform parent)
        {
            const string prefabPath = "Assets/prefab/UI/LoginPanel.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Missing prefab: {prefabPath}");
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException($"Failed to instantiate prefab: {prefabPath}");
            }

            instance.name = "LoginPanel";

            var loginUI = instance.GetComponent<LoginUI>();
            if (loginUI == null)
            {
                throw new InvalidOperationException("LoginPanel prefab must have LoginUI component.");
            }

            return loginUI;
        }

        private static ModeSelectUI CreateModePanel(Transform parent)
        {
            const string prefabPath = "Assets/prefab/UI/ModePanel.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Missing prefab: {prefabPath}");
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException($"Failed to instantiate prefab: {prefabPath}");
            }

            instance.name = "ModePanel";

            var modeUI = instance.GetComponent<ModeSelectUI>();
            if (modeUI == null)
            {
                throw new InvalidOperationException("ModePanel prefab must have ModeSelectUI component.");
            }

            return modeUI;
        }

        private static RecipeSelectUI CreateRecipePanel(Transform parent)
        {
            var panel = CreatePanel<RecipeSelectUI>(parent, "RecipePanel");
            var title = CreateTitle(panel.transform, "菜谱");

            // 菜谱按钮容器
            var recipeButtonRoot = new GameObject("RecipeButtonColumn", typeof(RectTransform));
            recipeButtonRoot.transform.SetParent(panel.transform, false);
            SetPreferredSize(recipeButtonRoot, 760, 260);
            var layout = recipeButtonRoot.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;

            // 菜谱按钮模板（默认隐藏）
            var recipeButtonTemplate = CreateButton(recipeButtonRoot.transform, "菜品模板");
            recipeButtonTemplate.gameObject.SetActive(false);

            var menuButton = CreateButton(panel.transform, "厨神菜单");
            Assign(panel, "recipeButtonRoot", recipeButtonRoot.transform);
            Assign(panel, "recipeButtonTemplate", recipeButtonTemplate);
            UnityEventTools.AddPersistentListener(menuButton.onClick, panel.OpenMenu);
            return panel;
        }

        private static StatusBarUI CreateStatusBar(Transform parent)
        {
            var bar = new GameObject("StatusBar", typeof(RectTransform));
            bar.transform.SetParent(parent, false);
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.offsetMin = new Vector2(0, -48);
            rect.offsetMax = Vector2.zero;

            var background = bar.AddComponent<Image>();
            background.color = new Color(0.07f, 0.07f, 0.08f, 0.96f);

            var statusBar = bar.AddComponent<StatusBarUI>();
            var text = CreateText(bar.transform, string.Empty);
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = 22;
            StretchChild(text.gameObject, 24, 0, -24, 0);
            Assign(statusBar, "statusText", text);
            bar.SetActive(false);
            return statusBar;
        }

        private static CookingUI CreateCookingPanel(Transform parent)
        {
            var panel = CreatePanel<CookingUI>(parent, "CookingPanel");
            var recipe = CreateTitle(panel.transform, "做菜");

            // Timer text (hidden by default)
            var timerText = CreateText(panel.transform, "");
            timerText.fontSize = 28;
            timerText.gameObject.SetActive(false);

            CreateKitchenVisual(panel.transform, out var dishStateImage);
            var state = CreateText(panel.transform, "当前状态");
            var hint = CreateText(panel.transform, "提示");
            var message = CreateText(panel.transform, string.Empty);
            Assign(panel, "recipeText", recipe);
            Assign(panel, "stateText", state);
            Assign(panel, "hintText", hint);
            Assign(panel, "messageText", message);
            Assign(panel, "dishStateImage", dishStateImage);
            Assign(panel, "timerText", timerText);

            var actions = CreateButtonRow(panel.transform);
            Assign(panel, "actionButtonRow", actions.transform);
            UnityEventTools.AddPersistentListener(CreateButton(actions.transform, "切菜", 120).onClick, panel.Cut);
            UnityEventTools.AddPersistentListener(CreateButton(actions.transform, "下锅", 120).onClick, panel.PutInPan);
            UnityEventTools.AddPersistentListener(CreateButton(actions.transform, "加热", 120).onClick, panel.Heat);
            UnityEventTools.AddPersistentListener(CreateButton(actions.transform, "加调料", 120).onClick, panel.Season);
            UnityEventTools.AddPersistentListener(CreateButton(actions.transform, "翻炒", 120).onClick, panel.Stir);
            UnityEventTools.AddPersistentListener(CreateButton(actions.transform, "出锅", 120).onClick, panel.Finish);

            // Timed popup overlay (hidden by default)
            CreateTimedPopupOverlay(panel.transform, panel);

            return panel;
        }

        private static void CreateTimedPopupOverlay(Transform parent, CookingUI cookingUI)
        {
            var overlayObj = new GameObject("TimedPopupOverlay", typeof(RectTransform));
            overlayObj.transform.SetParent(parent, false);
            var rect = overlayObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(420, 200);

            var image = overlayObj.AddComponent<Image>();
            image.color = new Color(0.08f, 0.07f, 0.06f, 0.92f);

            var outline = overlayObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.94f, 0.76f, 0.38f, 1f);
            outline.effectDistance = new Vector2(4, -4);

            // Ignore parent VerticalLayoutGroup so this sits centered on top
            var ignoreLayout = overlayObj.AddComponent<LayoutElement>();
            ignoreLayout.ignoreLayout = true;

            var layout = overlayObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 28, 28);
            layout.spacing = 18;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            overlayObj.SetActive(false);

            var questionText = CreateText(overlayObj.transform, "是否加热？");
            questionText.fontSize = 28;
            questionText.alignment = TextAnchor.MiddleCenter;

            var yesButton = CreateButton(overlayObj.transform, "是", 160);

            Assign(cookingUI, "popupOverlay", overlayObj);
            Assign(cookingUI, "popupQuestionText", questionText);
            Assign(cookingUI, "popupYesButton", yesButton);
        }

        private static GameObject CreateButtonRow(Transform parent)
        {
            var row = new GameObject("ActionButtonRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            SetPreferredSize(row, 840, 56);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
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
            CreateTitle(panel.transform, "评价");
            var text = CreateScrollableTextArea(panel.transform);
            var button = CreateButton(panel.transform, "保存菜品");
            var buttonText = button.GetComponentInChildren<Text>();
            Assign(panel, "reviewText", text);
            Assign(panel, "continueButtonText", buttonText);
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
            var dishRoot = CreateButtonColumn(panel.transform);
            var dishButtonTemplate = CreateButton(dishRoot.transform, "菜品");
            dishButtonTemplate.gameObject.SetActive(false);
            var backButton = CreateButton(panel.transform, "返回食单");
            backButton.gameObject.SetActive(false);
            var button = CreateButton(panel.transform, "再做一道");
            Assign(panel, "dishesText", text);
            Assign(panel, "dishesButtonRoot", dishRoot.transform);
            Assign(panel, "dishButtonTemplate", dishButtonTemplate);
            Assign(panel, "backButton", backButton);
            UnityEventTools.AddPersistentListener(backButton.onClick, panel.BackToDishes);
            UnityEventTools.AddPersistentListener(button.onClick, panel.CookAgain);
            return panel;
        }

        private static GameObject CreateButtonColumn(Transform parent)
        {
            var column = new GameObject("DishButtonColumn", typeof(RectTransform));
            column.transform.SetParent(parent, false);
            SetPreferredSize(column, 760, 260);
            var layout = column.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return column;
        }

        private static Text CreateScrollableTextArea(Transform parent)
        {
            var scrollObject = new GameObject("ReviewScrollView", typeof(RectTransform));
            scrollObject.transform.SetParent(parent, false);
            SetPreferredSize(scrollObject, 760, 320);
            var scrollImage = scrollObject.AddComponent<Image>();
            scrollImage.color = new Color(1f, 1f, 1f, 0.06f);
            var scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollObject.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(12, 12);
            viewportRect.offsetMax = new Vector2(-24, -12);
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 320);
            var contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var text = CreateText(content.transform, string.Empty);
            text.alignment = TextAnchor.UpperLeft;
            text.fontSize = 22;
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 1);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0.5f, 1);
            textRect.sizeDelta = new Vector2(0, 320);
            var textLayout = text.gameObject.GetComponent<LayoutElement>();
            textLayout.preferredHeight = 320;
            textLayout.minHeight = 320;

            var scrollbar = CreateVerticalScrollbar(scrollObject.transform);
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            return text;
        }

        private static Scrollbar CreateVerticalScrollbar(Transform parent)
        {
            var scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform));
            scrollbarObject.transform.SetParent(parent, false);
            var rect = scrollbarObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.sizeDelta = new Vector2(16, 0);
            rect.offsetMin = new Vector2(-16, 0);
            rect.offsetMax = Vector2.zero;

            var background = scrollbarObject.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.08f);
            var scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbarObject.transform, false);
            var slidingRect = slidingArea.GetComponent<RectTransform>();
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.offsetMin = new Vector2(2, 2);
            slidingRect.offsetMax = new Vector2(-2, -2);

            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(slidingArea.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.35f, 0.66f, 0.9f, 0.95f);
            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handleRect;
            return scrollbar;
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

        private static T CreatePixelPanel<T>(Transform parent, string name, float width, float height) where T : MonoBehaviour
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);

            var image = panel.AddComponent<Image>();
            image.color = new Color(0.08f, 0.07f, 0.06f, 0.86f);
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.94f, 0.76f, 0.38f, 1f);
            outline.effectDistance = new Vector2(4, -4);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 28, 28);
            layout.spacing = 14;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
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
            image.color = new Color(0.96f, 0.9f, 0.72f);
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.12f, 0.08f);
            outline.effectDistance = new Vector2(3, -3);
            var input = obj.AddComponent<InputField>();
            var text = CreateText(obj.transform, string.Empty);
            text.color = new Color(0.12f, 0.08f, 0.05f);
            text.alignment = TextAnchor.MiddleLeft;
            StretchChild(text.gameObject, 14, 8, -14, -8);
            var hint = CreateText(obj.transform, placeholder);
            hint.color = new Color(0.45f, 0.34f, 0.24f);
            hint.alignment = TextAnchor.MiddleLeft;
            StretchChild(hint.gameObject, 14, 8, -14, -8);
            input.textComponent = text;
            input.placeholder = hint;
            return input;
        }

        private static Button CreateButton(Transform parent, string text)
        {
            return CreateButton(parent, text, 260);
        }

        private static Button CreateButton(Transform parent, string text, float width)
        {
            var obj = new GameObject(text + "Button", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            SetPreferredSize(obj, width, 48);
            var image = obj.AddComponent<Image>();
            image.color = new Color(0.56f, 0.28f, 0.13f);
            var button = obj.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.56f, 0.28f, 0.13f);
            colors.highlightedColor = new Color(0.72f, 0.39f, 0.18f);
            colors.pressedColor = new Color(0.32f, 0.16f, 0.08f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(0.15f, 0.08f, 0.04f);
            outline.effectDistance = new Vector2(3, -3);
            var label = CreateText(obj.transform, text);
            label.fontSize = 24;
            label.color = new Color(1f, 0.94f, 0.72f);
            StretchChild(label.gameObject, 0, 0, 0, 0);
            return button;
        }

        private static Button CreatePrefabButton(Transform parent, string prefabPath, string text)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return CreateButton(parent, text);
            }

            var obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (obj == null)
            {
                return CreateButton(parent, text);
            }

            obj.transform.SetParent(parent, false);
            obj.name = text + "Button";

            var button = obj.GetComponent<Button>() ?? obj.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            for (var index = button.onClick.GetPersistentEventCount() - 1; index >= 0; index--)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, index);
            }

            var label = obj.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = text;
            }

            var layout = obj.GetComponent<LayoutElement>() ?? obj.AddComponent<LayoutElement>();
            layout.preferredWidth = 260;
            layout.preferredHeight = 48;
            layout.minHeight = 48;
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

        private static void Assign(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(fieldName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
