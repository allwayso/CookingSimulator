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

            EnsureFridgePrefabExists();
            EnsureCharacterPrefabExists();
            EnsureStovePrefabExists();

            var loginBackground = CreateLoginBackground();
            var character = CreateCharacter();
            var fridge = CreateFridge(character != null ? character.transform : null);
            var stove = CreateStove(character != null ? character.transform : null);
            CreateBoundaryWalls();
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
            var reviewWrite = CreateReviewWritePanel(canvas.transform);
            reviewWrite.gameObject.SetActive(false);
            var leaderboard = CreateLeaderboardPanel(canvas.transform);
            leaderboard.gameObject.SetActive(false);

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
            Assign(gameManager, "fridgeObject", fridge);
            Assign(gameManager, "reviewWriteUI", reviewWrite);
            Assign(gameManager, "leaderboardUI", leaderboard);

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

            // 复制 AI 配置文件到构建输出目录
            CopyAIResources(Path.GetDirectoryName(outputPath));
        }

        private static void CopyAIResources(string buildDir)
        {
            // 复制 ai_review.local.json
            var configSrc = Path.Combine(Application.dataPath, "..", "ai_review.local.json");
            var configDst = Path.Combine(buildDir, "ai_review.local.json");
            if (File.Exists(configSrc))
            {
                File.Copy(configSrc, configDst, true);
                Debug.Log($"[Build] Copied AI config to: {configDst}");
            }

            // 复制 NPC 文件
            var npcsSrc = Path.Combine(Application.streamingAssetsPath, "NPCs");
            var npcsDst = Path.Combine(buildDir, "CookingSimulator_Data", "StreamingAssets", "NPCs");
            if (Directory.Exists(npcsSrc))
            {
                if (!Directory.Exists(npcsDst))
                    Directory.CreateDirectory(npcsDst);
                foreach (var file in Directory.GetFiles(npcsSrc))
                {
                    var fileName = Path.GetFileName(file);
                    File.Copy(file, Path.Combine(npcsDst, fileName), true);
                }
                Debug.Log($"[Build] Copied NPC files to: {npcsDst}");
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

        private static void CreateBoundaryWalls()
        {
            // 房间边界不可见墙（防止角色走出场景）
            // 摄像机正交尺寸 7.5，可视范围约 ±9.4x ±6.5（1280×720 参考分辨率）
            CreateWall("Wall_Top",    new Vector2(0f,   4.5f), new Vector2(20f, 0.3f));
            CreateWall("Wall_Bottom", new Vector2(0f,  -3.2f), new Vector2(20f, 0.3f));
            CreateWall("Wall_Left",   new Vector2(-10f, 0.6f), new Vector2(0.3f, 8f));
            CreateWall("Wall_Right",  new Vector2(10f,  0.6f), new Vector2(0.3f, 8f));
        }

        private static void CreateWall(string name, Vector2 position, Vector2 size)
        {
            var wall = new GameObject(name);
            wall.transform.position = position;
            var block = wall.AddComponent<BlockObject>();
            var blockSO = new SerializedObject(block);
            blockSO.FindProperty("size").vector2Value = size;
            blockSO.FindProperty("offset").vector2Value = Vector2.zero;
            blockSO.ApplyModifiedPropertiesWithoutUndo();
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

        private static void EnsureCharacterPrefabExists()
        {
            const string path = "Assets/prefab/kitchen/Character.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return;

            var character = BuildCharacterAsset();
            PrefabUtility.SaveAsPrefabAsset(character, path);
            UnityEngine.Object.DestroyImmediate(character);
            AssetDatabase.SaveAssets();
        }

        private static GameObject CreateCharacter()
        {
            const string path = "Assets/prefab/kitchen/Character.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var character = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            character.name = "小人"; // 保持原名，冰箱动画运行时查找依赖此名称
            return character;
        }

        private static GameObject BuildCharacterAsset()
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
            sr.sortingOrder = 5; // 确保角色渲染在 tilemap 层之上

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

        private static void EnsureFridgePrefabExists()
        {
            const string path = "Assets/prefab/kitchen/Fridge.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return;

            var fridge = BuildFridgeAsset();
            Directory.CreateDirectory("Assets/prefab/kitchen");
            PrefabUtility.SaveAsPrefabAsset(fridge, path);
            UnityEngine.Object.DestroyImmediate(fridge);
            AssetDatabase.SaveAssets();
        }

        private static GameObject CreateFridge(Transform playerTransform)
        {
            const string path = "Assets/prefab/kitchen/Fridge.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var fridge = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            fridge.name = "Fridge 1 _0"; // 保持原名，人物移动碰撞检测依赖此名称
            fridge.transform.position = new Vector3(-4f, 4f, 0f);

            // 注入运行时引用（prefab 无法预置 player 引用）
            var anim = fridge.GetComponent<冰箱动画>();
            if (anim != null && playerTransform != null)
            {
                var animSO = new SerializedObject(anim);
                animSO.FindProperty("player").objectReferenceValue = playerTransform;
                animSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // 确保旧 prefab 也有 BlockObject（兼容旧版本 prefab）
            if (fridge.GetComponent<BlockObject>() == null)
            {
                var block = fridge.AddComponent<BlockObject>();
                var blockSO = new SerializedObject(block);
                blockSO.FindProperty("size").vector2Value = new Vector2(0.39f, 0.76f);
                blockSO.FindProperty("offset").vector2Value = new Vector2(-0.175f, 0f);
                blockSO.ApplyModifiedPropertiesWithoutUndo();
            }

            return fridge;
        }

        private static GameObject BuildFridgeAsset()
        {
            var fridge = new GameObject("Fridge 1 _0");
            fridge.transform.position = new Vector3(-4f, 4f, 0f);
            fridge.transform.localScale = new Vector3(6.25f, 6.25f, 1f);

            var frame0 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/kitchen/Fridge 1 _sprites/Fridge 1 _000.png");
            var frame1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/kitchen/Fridge 1 _sprites/Fridge 1 _001.png");
            var frame2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/kitchen/Fridge 1 _sprites/Fridge 1 _002.png");
            var frame3 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/kitchen/Fridge 1 _sprites/Fridge 1 _003.png");
            var frame4 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/kitchen/Fridge 1 _sprites/Fridge 1 _004.png");

            var sr = fridge.AddComponent<SpriteRenderer>();
            sr.sprite = frame0;
            sr.sortingOrder = 5; // 确保渲染在 tilemap 层之上（floor=0, border=1, otherStuff=2）

            var anim = fridge.AddComponent<冰箱动画>();
            var animSO = new SerializedObject(anim);
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

            // 碰撞体
            var block = fridge.AddComponent<BlockObject>();
            var blockSO = new SerializedObject(block);
            blockSO.FindProperty("size").vector2Value = new Vector2(0.39f, 0.76f);
            blockSO.FindProperty("offset").vector2Value = new Vector2(-0.175f, 0f);
            blockSO.ApplyModifiedPropertiesWithoutUndo();

            return fridge;
        }

        private static void EnsureStovePrefabExists()
        {
            const string path = "Assets/prefab/kitchen/Stove.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return;

            var stove = BuildStoveAsset();
            PrefabUtility.SaveAsPrefabAsset(stove, path);
            UnityEngine.Object.DestroyImmediate(stove);
            AssetDatabase.SaveAssets();
        }

        private static GameObject CreateStove(Transform playerTransform)
        {
            const string path = "Assets/prefab/kitchen/Stove.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var stove = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            // 确保旧 prefab 也有 BlockObject + 更新触发距离（兼容旧版本 prefab）
            if (stove.GetComponent<BlockObject>() == null)
            {
                var block = stove.AddComponent<BlockObject>();
                var blockSO = new SerializedObject(block);
                blockSO.FindProperty("size").vector2Value = new Vector2(0.4f, 0.3f);
                blockSO.FindProperty("offset").vector2Value = Vector2.zero;
                blockSO.ApplyModifiedPropertiesWithoutUndo();
            }

            var interactable = stove.GetComponent<交互物>();
            if (interactable != null)
            {
                var intSO = new SerializedObject(interactable);
                intSO.FindProperty("triggerDistance").floatValue = 2f;
                intSO.ApplyModifiedPropertiesWithoutUndo();
            }

            return stove;
        }

        private static GameObject BuildStoveAsset()
        {
            var stove = new GameObject("灶台");
            stove.transform.position = new Vector3(7f, 2.76f, 0f);
            stove.transform.localScale = new Vector3(5.0f, 5.0f, 1f);

            var stoveSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/kitchen/StoveCutouts/灶台2.png");

            var sr = stove.AddComponent<SpriteRenderer>();
            sr.sprite = stoveSprite;
            sr.sortingOrder = 5; // 确保渲染在 tilemap 层之上

            // 灶台动画（静态精灵）
            var anim = stove.AddComponent<灶台动画>();
            var animSO = new SerializedObject(anim);
            animSO.FindProperty("stoveSprite").objectReferenceValue = stoveSprite;
            animSO.ApplyModifiedPropertiesWithoutUndo();

            // 交互物组件
            var interactable = stove.AddComponent<交互物>();
            var intSO = new SerializedObject(interactable);
            intSO.FindProperty("triggerDistance").floatValue = 2f;
            intSO.FindProperty("bodyOffset").vector3Value = Vector3.zero;
            intSO.FindProperty("promptMessage").stringValue = "按F做菜";
            intSO.FindProperty("interactionType").enumValueIndex = (int)InteractionType.Stove;
            intSO.ApplyModifiedPropertiesWithoutUndo();

            // 碰撞体（只挡灶台核心区域）
            var block = stove.AddComponent<BlockObject>();
            var blockSO = new SerializedObject(block);
            blockSO.FindProperty("size").vector2Value = new Vector2(0.4f, 0.3f);
            blockSO.FindProperty("offset").vector2Value = Vector2.zero;
            blockSO.ApplyModifiedPropertiesWithoutUndo();

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

            // Wire Foodie Mode button
            var laobaButton = instance.transform.Find("LaobaButton");
            if (laobaButton != null)
            {
                var btn = laobaButton.GetComponent<Button>();
                if (btn != null)
                {
                    UnityEventTools.AddPersistentListener(btn.onClick, modeUI.EnterFoodieMode);
                }
            }

            // 追加排行榜按钮（独立定位，屏幕中上区域）
            var lbBtn = CreateButton(instance.transform, "排行榜", 260);
            lbBtn.name = "排行榜Button";
            var lbRect = lbBtn.GetComponent<RectTransform>();
            lbRect.anchorMin = new Vector2(0.5f, 0.5f);
            lbRect.anchorMax = new Vector2(0.5f, 0.5f);
            lbRect.anchoredPosition = new Vector2(0, 296);
            UnityEventTools.AddPersistentListener(lbBtn.onClick, modeUI.EnterLeaderboard);

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
            const string prefabPath = "Assets/prefab/UI/CookingPanel.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Missing prefab: {prefabPath}");

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Failed to instantiate prefab: {prefabPath}");

            instance.transform.SetParent(parent, false);
            instance.name = "CookingPanel";
            Debug.Log("Build MVP Scene: CookingPanel instantiated directly from prefab without auto-upgrade.");
            return instance.GetComponent<CookingUI>();
        }

        /// <summary>
        /// 一次性升级 CookingPanel.prefab
        /// </summary>
        [MenuItem("Cooking Simulator/Upgrade CookingPanel Prefab")]
        public static void UpgradeCookingPanelPrefab()
        {
            const string prefabPath = "Assets/prefab/UI/CookingPanel.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing == null)
            {
                Debug.LogError($"Prefab not found: {prefabPath}");
                return;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var root = scope.prefabContentsRoot;
                var cookingUI = root.GetComponent<CookingUI>();
                if (cookingUI == null)
                {
                    Debug.LogError("CookingPanel prefab must have CookingUI component.");
                    return;
                }

                Debug.Log("Upgrading CookingPanel prefab (delta)...");

                // 每个元素独立检测，只补缺失的

                // 1. 移除旧的"加热"按钮
                RemoveHeatButton(root.transform);

                // 2. 计时器
                if (root.transform.Find("TimerText") == null)
                    Assign(cookingUI, "timerText", CreateTimerLabel(root.transform));

                // 3. 锅按钮化 + 盘子
                var potChild = root.transform.Find("pot");
                if (potChild != null && potChild.GetComponent<Button>() == null)
                {
                    ButtonizePan(root.transform, out var panBtn);
                    Assign(cookingUI, "panButton", panBtn);
                    UnityEventTools.AddPersistentListener(panBtn.onClick, cookingUI.OnPanClicked);
                }
                if (root.transform.Find("Plate") == null)
                {
                    var plateBtn = CreatePlate(root.transform);
                    Assign(cookingUI, "plateButton", plateBtn);
                    UnityEventTools.AddPersistentListener(plateBtn.onClick, cookingUI.OnPlateClicked);
                }

                // 4. 食材选择行
                if (root.transform.Find("IngredientSelector") == null)
                {
                    CreateIngredientSelector(root.transform, out var tBtn, out var eBtn, out var sTxt);
                    Assign(cookingUI, "tomatoSelectBtn", tBtn);
                    Assign(cookingUI, "eggSelectBtn", eBtn);
                    Assign(cookingUI, "selectedIngredientText", sTxt);
                    UnityEventTools.AddPersistentListener(tBtn.onClick, cookingUI.SelectTomato);
                    UnityEventTools.AddPersistentListener(eBtn.onClick, cookingUI.SelectEgg);
                }

                // 5. 食材熟度行
                if (root.transform.Find("IngredientDoneness") == null)
                {
                    CreateDonenessRow(root.transform, out var tImg, out var eImg, out var tTxt, out var eTxt);
                    Assign(cookingUI, "tomatoDonenessImage", tImg);
                    Assign(cookingUI, "eggDonenessImage", eImg);
                    Assign(cookingUI, "tomatoDonenessText", tTxt);
                    Assign(cookingUI, "eggDonenessText", eTxt);
                }

                // 6. 盘子上的食材图片
                if (root.transform.Find("TomatoPlateImage") == null)
                    Assign(cookingUI, "tomatoPlateImage", CreatePlateIngredientImage(root.transform, "TomatoPlateImage"));
                if (root.transform.Find("EggPlateImage") == null)
                    Assign(cookingUI, "eggPlateImage", CreatePlateIngredientImage(root.transform, "EggPlateImage"));

                // 7. 火力滑杆行
                if (root.transform.Find("FireControl") == null)
                {
                    CreateFireControlRow(root.transform, out var fs, out var flt);
                    Assign(cookingUI, "fireSlider", fs);
                    Assign(cookingUI, "fireLevelText", flt);
                    UnityEventTools.AddPersistentListener(fs.onValueChanged, cookingUI.OnFireSliderChanged);
                }

                // 8. 定时提示弹窗
                if (root.transform.Find("PopupPanel") == null)
                {
                    CreatePopupPanel(root.transform, out var popupRoot, out var titleTxt, out var bodyTxt, out var closeBtn, out var cg);
                    Assign(cookingUI, "popupRoot", popupRoot);
                    Assign(cookingUI, "popupTitleText", titleTxt);
                    Assign(cookingUI, "popupBodyText", bodyTxt);
                    Assign(cookingUI, "popupCloseButton", closeBtn);
                    Assign(cookingUI, "popupCanvasGroup", cg);
                    UnityEventTools.AddPersistentListener(closeBtn.onClick, cookingUI.OnPopupCloseClicked);
                }

                Debug.Log("CookingPanel prefab upgrade complete.");
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 给 prefab 中的 "pot" Image 添加 Button 组件使其可点击
        /// </summary>
        private static void ButtonizePan(Transform root, out Button panButton)
        {
            panButton = null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "pot")
                {
                    panButton = child.gameObject.AddComponent<Button>();
                    return;
                }
            }

            // fallback: 如果找不到 pot，创建一个
            var fallback = new GameObject("pot", typeof(RectTransform), typeof(Image), typeof(Button));
            fallback.transform.SetParent(root, false);
            var rect = fallback.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(120, 120);
            fallback.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.09f);
            panButton = fallback.GetComponent<Button>();
        }

        /// <summary>
        /// 创建盘子：一个可点击的 Image+Button
        /// </summary>
        private static Button CreatePlate(Transform parent)
        {
            var plate = new GameObject("Plate", typeof(RectTransform), typeof(Image), typeof(Button));
            plate.transform.SetParent(parent, false);
            var rect = plate.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(260, -30);
            rect.sizeDelta = new Vector2(100, 24);
            var img = plate.GetComponent<Image>();
            img.color = new Color(0.5f, 0.48f, 0.45f);
            var btn = plate.GetComponent<Button>();

            // 盘子标签
            var label = CreateText(plate.transform, "盘子");
            label.fontSize = 16;
            label.color = new Color(0.9f, 0.88f, 0.8f);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            btn.interactable = false;
            return btn;
        }

        /// <summary>
        /// 创建盘子上的食材图片（初始隐藏）
        /// </summary>
        private static Image CreatePlateIngredientImage(Transform parent, string name)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(260, -10);
            rect.sizeDelta = new Vector2(64, 64);
            var img = obj.AddComponent<Image>();
            img.color = Color.white;
            obj.SetActive(false);
            return img;
        }

        /// <summary>
        /// 创建食材选择行：番茄/鸡蛋按钮 + 选中文本
        /// </summary>
        private static void CreateIngredientSelector(Transform parent,
            out Button tomatoBtn, out Button eggBtn, out Text selText)
        {
            var row = new GameObject("IngredientSelector", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            SetPreferredSize(row, 760, 44);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var label = CreateText(row.transform, "选择食材:");
            label.fontSize = 18;
            label.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 32);

            // 番茄按钮
            var tomatoObj = new GameObject("TomatoSelectBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            tomatoObj.transform.SetParent(row.transform, false);
            SetPreferredSize(tomatoObj, 96, 36);
            tomatoObj.GetComponent<Image>().color = new Color(0.35f, 0.25f, 0.15f);
            tomatoBtn = tomatoObj.GetComponent<Button>();
            var tomatoLabel = CreateText(tomatoObj.transform, "番茄");
            tomatoLabel.fontSize = 20;
            StretchChild(tomatoLabel.gameObject, 0, 0, 0, 0);

            // 鸡蛋按钮
            var eggObj = new GameObject("EggSelectBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            eggObj.transform.SetParent(row.transform, false);
            SetPreferredSize(eggObj, 96, 36);
            eggObj.GetComponent<Image>().color = new Color(0.35f, 0.25f, 0.15f);
            eggBtn = eggObj.GetComponent<Button>();
            var eggLabel = CreateText(eggObj.transform, "鸡蛋");
            eggLabel.fontSize = 20;
            StretchChild(eggLabel.gameObject, 0, 0, 0, 0);

            // 选中文本
            selText = CreateText(row.transform, "下锅: 鸡蛋");
            selText.fontSize = 20;
            selText.color = new Color(0.95f, 0.88f, 0.56f);
            selText.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 32);
        }

        /// <summary>
        /// 在 prefab 实例中查找并移除"加热"按钮（CookingUI.Heat() 已删除，由火力滑杆替代）
        /// </summary>
        private static void RemoveHeatButton(Transform root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Contains("加热"))
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                    return;
                }
            }
        }

        /// <summary>
        /// 创建左上角烹饪计时器
        /// </summary>
        private static Text CreateTimerLabel(Transform parent)
        {
            var timerObj = new GameObject("TimerText", typeof(RectTransform));
            timerObj.transform.SetParent(parent, false);
            var rect = timerObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(16, -16);
            rect.sizeDelta = new Vector2(160, 40);
            var text = timerObj.AddComponent<Text>();
            text.text = "⏱ 00:00";
            text.fontSize = 28;
            text.color = new Color(0.95f, 0.88f, 0.56f);
            text.alignment = TextAnchor.MiddleLeft;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private static void CreateDonenessRow(Transform parent,
            out Image tomatoImage, out Image eggImage,
            out Text tomatoText, out Text eggText)
        {
            var row = new GameObject("IngredientDoneness", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            SetPreferredSize(row, 760, 64);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 40;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // 番茄熟度组
            var tomatoGroup = new GameObject("TomatoGroup", typeof(RectTransform));
            tomatoGroup.transform.SetParent(row.transform, false);
            SetPreferredSize(tomatoGroup, 240, 56);
            var tomatoGroupLayout = tomatoGroup.AddComponent<HorizontalLayoutGroup>();
            tomatoGroupLayout.spacing = 8;
            tomatoGroupLayout.childAlignment = TextAnchor.MiddleCenter;
            tomatoGroupLayout.childControlWidth = false;
            tomatoGroupLayout.childControlHeight = false;
            tomatoGroupLayout.childForceExpandWidth = false;
            tomatoGroupLayout.childForceExpandHeight = false;

            var tomatoImgObj = new GameObject("TomatoImage", typeof(RectTransform));
            tomatoImgObj.transform.SetParent(tomatoGroup.transform, false);
            SetPreferredSize(tomatoImgObj, 48, 48);
            tomatoImage = tomatoImgObj.AddComponent<Image>();
            tomatoImage.color = new Color(0.82f, 0.25f, 0.2f);

            tomatoText = CreateText(tomatoGroup.transform, "番茄: 全生");
            tomatoText.fontSize = 20;
            var tomatoTextRect = tomatoText.GetComponent<RectTransform>();
            tomatoTextRect.sizeDelta = new Vector2(172, 32);

            // 鸡蛋熟度组
            var eggGroup = new GameObject("EggGroup", typeof(RectTransform));
            eggGroup.transform.SetParent(row.transform, false);
            SetPreferredSize(eggGroup, 240, 56);
            var eggGroupLayout = eggGroup.AddComponent<HorizontalLayoutGroup>();
            eggGroupLayout.spacing = 8;
            eggGroupLayout.childAlignment = TextAnchor.MiddleCenter;
            eggGroupLayout.childControlWidth = false;
            eggGroupLayout.childControlHeight = false;
            eggGroupLayout.childForceExpandWidth = false;
            eggGroupLayout.childForceExpandHeight = false;

            var eggImgObj = new GameObject("EggImage", typeof(RectTransform));
            eggImgObj.transform.SetParent(eggGroup.transform, false);
            SetPreferredSize(eggImgObj, 48, 48);
            eggImage = eggImgObj.AddComponent<Image>();
            eggImage.color = new Color(0.95f, 0.88f, 0.56f);

            eggText = CreateText(eggGroup.transform, "鸡蛋: 全生");
            eggText.fontSize = 20;
            var eggTextRect = eggText.GetComponent<RectTransform>();
            eggTextRect.sizeDelta = new Vector2(172, 32);
        }

        private static void CreateFireControlRow(Transform parent,
            out Slider fireSlider, out Text fireLevelText)
        {
            var row = new GameObject("FireControl", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            SetPreferredSize(row, 760, 56);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // 标签
            var label = CreateText(row.transform, "火力");
            label.fontSize = 22;
            label.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 32);

            // 滑杆
            var sliderObj = new GameObject("FireSlider", typeof(RectTransform));
            sliderObj.transform.SetParent(row.transform, false);
            SetPreferredSize(sliderObj, 300, 32);

            // 滑杆背景
            var sliderBg = sliderObj.AddComponent<Image>();
            sliderBg.color = new Color(0.25f, 0.18f, 0.12f);

            fireSlider = sliderObj.AddComponent<Slider>();
            fireSlider.minValue = 0;
            fireSlider.maxValue = 4;
            fireSlider.wholeNumbers = true;
            fireSlider.value = 0;
            fireSlider.interactable = false;
            fireSlider.direction = Slider.Direction.LeftToRight;

            // 滑杆填充区域
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.95f, 0.42f, 0.18f);
            fireSlider.fillRect = fillRect;
            fireSlider.targetGraphic = fillImage;

            // 滑杆手柄
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObj.transform, false);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;
            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(24, 40);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.95f, 0.6f, 0.2f);
            fireSlider.handleRect = handleRect;

            // 火力档位文字
            fireLevelText = CreateText(row.transform, "关火");
            fireLevelText.fontSize = 22;
            fireLevelText.color = new Color(0.95f, 0.42f, 0.18f);
            fireLevelText.GetComponent<RectTransform>().sizeDelta = new Vector2(72, 32);
        }

        private static ReviewUI CreateReviewPanel(Transform parent)
        {
            const string prefabPath = "Assets/prefab/UI/ReviewPanel.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Missing prefab: {prefabPath}");

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Failed to instantiate prefab: {prefabPath}");

            instance.transform.SetParent(parent, false);
            instance.name = "ReviewPanel";
            Debug.Log("Build MVP Scene: ReviewPanel instantiated directly from prefab.");
            return instance.GetComponent<ReviewUI>();
        }

        private static SaveDishUI CreateSaveDishPanel(Transform parent)
        {
            const string prefabPath = "Assets/prefab/UI/SaveDishPanel.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Missing prefab: {prefabPath}");

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Failed to instantiate prefab: {prefabPath}");

            instance.transform.SetParent(parent, false);
            instance.name = "SaveDishPanel";
            Debug.Log("Build MVP Scene: SaveDishPanel instantiated directly from prefab.");
            return instance.GetComponent<SaveDishUI>();
        }

        private static MenuUI CreateMenuPanel(Transform parent)
        {
            const string prefabPath = "Assets/prefab/UI/MenuPanel.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Missing prefab: {prefabPath}");

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Failed to instantiate prefab: {prefabPath}");

            instance.transform.SetParent(parent, false);
            instance.name = "MenuPanel";

            var menuUI = instance.GetComponent<MenuUI>();

            // 追加翻页控件（锚定到底部偏上）
            var pageRow = new GameObject("PageRow", typeof(RectTransform));
            pageRow.transform.SetParent(instance.transform, false);
            var pageRowRect = pageRow.GetComponent<RectTransform>();
            pageRowRect.anchorMin = new Vector2(0.5f, 0f);
            pageRowRect.anchorMax = new Vector2(0.5f, 0f);
            pageRowRect.anchoredPosition = new Vector2(0, 176);
            SetPreferredSize(pageRow, 400, 36);
            var pageLayout = pageRow.AddComponent<HorizontalLayoutGroup>();
            pageLayout.spacing = 16;
            pageLayout.childAlignment = TextAnchor.MiddleCenter;
            pageLayout.childControlWidth = false;
            pageLayout.childControlHeight = true;

            var prevBtn = CreateButton(pageRow.transform, "上一页", 120);
            var pageText = CreateText(pageRow.transform, "1 / 1");
            pageText.fontSize = 20;
            pageText.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 32);
            var nextBtn = CreateButton(pageRow.transform, "下一页", 120);

            Assign(menuUI, "prevButton", prevBtn);
            Assign(menuUI, "nextButton", nextBtn);
            Assign(menuUI, "pageText", pageText);

            UnityEventTools.AddPersistentListener(prevBtn.onClick, menuUI.PrevPage);
            UnityEventTools.AddPersistentListener(nextBtn.onClick, menuUI.NextPage);

            Debug.Log("Build MVP Scene: MenuPanel instantiated directly from prefab.");
            return menuUI;
        }

        private static ReviewWriteUI CreateReviewWritePanel(Transform parent)
        {
            var panel = CreatePanel<ReviewWriteUI>(parent, "ReviewWritePanel");

            var title = CreateTitle(panel.transform, "写评价");

            // 菜名
            var dishNameText = CreateText(panel.transform, "");
            dishNameText.fontSize = 26;

            // 评分输入行
            var scoreRow = new GameObject("ScoreRow", typeof(RectTransform));
            scoreRow.transform.SetParent(panel.transform, false);
            SetPreferredSize(scoreRow, 500, 44);
            var scoreLayout = scoreRow.AddComponent<HorizontalLayoutGroup>();
            scoreLayout.spacing = 12;
            scoreLayout.childAlignment = TextAnchor.MiddleCenter;
            scoreLayout.childControlWidth = false;
            scoreLayout.childControlHeight = true;

            var scoreLabel = CreateText(scoreRow.transform, "评分 (0-100):");
            scoreLabel.fontSize = 22;
            scoreLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 32);

            var scoreInputObj = new GameObject("ScoreInput", typeof(RectTransform), typeof(Image));
            scoreInputObj.transform.SetParent(scoreRow.transform, false);
            SetPreferredSize(scoreInputObj, 120, 40);
            scoreInputObj.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);
            var scoreInputField = scoreInputObj.AddComponent<InputField>();
            var scoreTextArea = new GameObject("Text", typeof(RectTransform));
            scoreTextArea.transform.SetParent(scoreInputObj.transform, false);
            var scorePlaceholder = scoreTextArea.AddComponent<Text>();
            scorePlaceholder.fontSize = 20;
            scorePlaceholder.color = new Color(0.5f, 0.5f, 0.5f);
            scorePlaceholder.alignment = TextAnchor.MiddleCenter;
            scorePlaceholder.font = scoreLabel.font;
            scorePlaceholder.text = "0-100";
            StretchChild(scoreTextArea, 4, 0, 4, 0);
            scoreInputField.textComponent = scorePlaceholder;

            // 评语输入行
            var commentRow = new GameObject("CommentRow", typeof(RectTransform));
            commentRow.transform.SetParent(panel.transform, false);
            SetPreferredSize(commentRow, 500, 44);
            var commentLayout = commentRow.AddComponent<HorizontalLayoutGroup>();
            commentLayout.spacing = 12;
            commentLayout.childAlignment = TextAnchor.MiddleCenter;
            commentLayout.childControlWidth = false;
            commentLayout.childControlHeight = true;

            var commentLabel = CreateText(commentRow.transform, "评语:");
            commentLabel.fontSize = 22;
            commentLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(64, 32);

            var commentInputObj = new GameObject("CommentInput", typeof(RectTransform), typeof(Image));
            commentInputObj.transform.SetParent(commentRow.transform, false);
            SetPreferredSize(commentInputObj, 340, 40);
            commentInputObj.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);
            var commentInputField = commentInputObj.AddComponent<InputField>();
            var commentTextArea = new GameObject("Text", typeof(RectTransform));
            commentTextArea.transform.SetParent(commentInputObj.transform, false);
            var commentPlaceholder = commentTextArea.AddComponent<Text>();
            commentPlaceholder.fontSize = 20;
            commentPlaceholder.color = new Color(0.5f, 0.5f, 0.5f);
            commentPlaceholder.alignment = TextAnchor.MiddleLeft;
            commentPlaceholder.font = commentLabel.font;
            commentPlaceholder.text = "写下你的评价...";
            StretchChild(commentTextArea, 8, 2, 8, 2);
            commentInputField.textComponent = commentPlaceholder;

            // 提示文字
            var messageText = CreateText(panel.transform, "");
            messageText.fontSize = 18;
            messageText.color = new Color(0.95f, 0.5f, 0.3f);

            // 按钮行
            var buttonsRow = new GameObject("ButtonsRow", typeof(RectTransform));
            buttonsRow.transform.SetParent(panel.transform, false);
            SetPreferredSize(buttonsRow, 400, 48);
            var buttonsLayout = buttonsRow.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 20;
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;

            var submitButton = CreateButton(buttonsRow.transform, "提交评价");
            var cancelButton = CreateButton(buttonsRow.transform, "返回");

            Assign(panel, "dishNameText", dishNameText);
            Assign(panel, "scoreInput", scoreInputField);
            Assign(panel, "commentInput", commentInputField);
            Assign(panel, "messageText", messageText);

            UnityEventTools.AddPersistentListener(submitButton.onClick, panel.Submit);
            UnityEventTools.AddPersistentListener(cancelButton.onClick, panel.Cancel);

            return panel;
        }

        private static LeaderboardUI CreateLeaderboardPanel(Transform parent)
        {
            var panel = CreatePanel<LeaderboardUI>(parent, "LeaderboardPanel");

            // 调整面板位置到屏幕中上
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.35f);
            panelRect.anchorMax = new Vector2(0.5f, 0.95f);
            panelRect.offsetMin = new Vector2(-350, 0);
            panelRect.offsetMax = new Vector2(350, 0);

            var title = CreateTitle(panel.transform, "排行榜");
            var titleText = title;

            // Tab 切换行
            var tabRow = new GameObject("TabRow", typeof(RectTransform));
            tabRow.transform.SetParent(panel.transform, false);
            SetPreferredSize(tabRow, 500, 40);
            var tabLayout = tabRow.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 12;
            tabLayout.childAlignment = TextAnchor.MiddleCenter;
            tabLayout.childControlWidth = false;
            tabLayout.childControlHeight = true;

            var chefTab = CreateButton(tabRow.transform, "厨神声望榜", 180);
            var foodieTab = CreateButton(tabRow.transform, "美食家血量榜", 180);

            // 条目列表区（5 条，每条高 32）
            var entryRoot = new GameObject("EntryRoot", typeof(RectTransform));
            entryRoot.transform.SetParent(panel.transform, false);
            SetPreferredSize(entryRoot, 600, 180);
            var entryLayout = entryRoot.AddComponent<VerticalLayoutGroup>();
            entryLayout.spacing = 0;
            entryLayout.childAlignment = TextAnchor.UpperCenter;
            entryLayout.childControlWidth = true;
            entryLayout.childControlHeight = false;

            // 条目模板（纯文本）
            var entryTemplateObj = new GameObject("EntryTemplate", typeof(RectTransform));
            entryTemplateObj.transform.SetParent(entryRoot.transform, false);
            SetPreferredSize(entryTemplateObj, 560, 32);
            var entryTplText = entryTemplateObj.AddComponent<Text>();
            entryTplText.fontSize = 22;
            entryTplText.color = new Color(1f, 0.94f, 0.72f);
            entryTplText.alignment = TextAnchor.MiddleCenter;
            entryTplText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            entryTplText.text = "#N  用户名  —  分数";
            entryTemplateObj.SetActive(false);
            var entryTemplate = entryTemplateObj.AddComponent<Button>();

            // 间距（下移 2cm ≈ 76px）
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(panel.transform, false);
            SetPreferredSize(spacer, 600, 76);

            // 翻页行
            var pageRow = new GameObject("PageRow", typeof(RectTransform));
            pageRow.transform.SetParent(panel.transform, false);
            SetPreferredSize(pageRow, 400, 36);
            var pageLayout = pageRow.AddComponent<HorizontalLayoutGroup>();
            pageLayout.spacing = 16;
            pageLayout.childAlignment = TextAnchor.MiddleCenter;
            pageLayout.childControlWidth = false;
            pageLayout.childControlHeight = true;

            var prevBtn = CreateButton(pageRow.transform, "上一页", 120);
            var pageText = CreateText(pageRow.transform, "1 / 1");
            pageText.fontSize = 20;
            pageText.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 32);
            var nextBtn = CreateButton(pageRow.transform, "下一页", 120);

            // 返回按钮
            var backBtn = CreateButton(panel.transform, "返回");

            Assign(panel, "titleText", titleText);
            Assign(panel, "entryRoot", entryRoot.transform);
            Assign(panel, "entryTemplate", entryTemplate);
            Assign(panel, "chefTabButton", chefTab);
            Assign(panel, "foodieTabButton", foodieTab);
            Assign(panel, "prevButton", prevBtn);
            Assign(panel, "nextButton", nextBtn);
            Assign(panel, "pageText", pageText);

            UnityEventTools.AddPersistentListener(chefTab.onClick, panel.SwitchToChef);
            UnityEventTools.AddPersistentListener(foodieTab.onClick, panel.SwitchToFoodie);
            UnityEventTools.AddPersistentListener(prevBtn.onClick, panel.PrevPage);
            UnityEventTools.AddPersistentListener(nextBtn.onClick, panel.NextPage);
            UnityEventTools.AddPersistentListener(backBtn.onClick, panel.Back);

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

        /// <summary>
        /// 创建做菜定时弹窗面板：深色半透明背景+金色描边+标题+正文+关闭按钮+CanvasGroup
        /// </summary>
        private static void CreatePopupPanel(Transform parent,
            out GameObject popupRoot, out Text titleText, out Text bodyText,
            out Button closeButton, out CanvasGroup canvasGroup)
        {
            // 根节点
            popupRoot = new GameObject("PopupPanel", typeof(RectTransform));
            popupRoot.transform.SetParent(parent, false);
            popupRoot.transform.SetAsLastSibling();

            var rootRect = popupRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0, -56);
            rootRect.sizeDelta = new Vector2(620, 180);

            var bgImage = popupRoot.AddComponent<Image>();
            bgImage.color = new Color(0.10f, 0.09f, 0.08f, 0.95f);
            bgImage.raycastTarget = false;

            var outline = popupRoot.AddComponent<Outline>();
            outline.effectColor = new Color(0.94f, 0.76f, 0.38f, 1f);
            outline.effectDistance = new Vector2(2, -2);

            canvasGroup = popupRoot.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // 标题
            var titleObj = new GameObject("TitleText", typeof(RectTransform));
            titleObj.transform.SetParent(popupRoot.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -12);
            titleRect.sizeDelta = new Vector2(560, 32);
            titleText = titleObj.AddComponent<Text>();
            titleText.fontSize = 24;
            titleText.color = new Color(0.95f, 0.88f, 0.56f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontStyle = FontStyle.Bold;

            // 正文
            var bodyObj = new GameObject("BodyText", typeof(RectTransform));
            bodyObj.transform.SetParent(popupRoot.transform, false);
            var bodyRect = bodyObj.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.offsetMin = new Vector2(30, 16);
            bodyRect.offsetMax = new Vector2(-30, -48);
            bodyText = bodyObj.AddComponent<Text>();
            bodyText.fontSize = 20;
            bodyText.color = Color.white;
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;

            // 关闭按钮（右上角）
            var closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObj.transform.SetParent(popupRoot.transform, false);
            var closeRect = closeObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-4, -4);
            closeRect.sizeDelta = new Vector2(28, 28);
            var closeImg = closeObj.GetComponent<Image>();
            closeImg.color = new Color(0.25f, 0.18f, 0.12f);
            closeImg.raycastTarget = true;
            closeButton = closeObj.GetComponent<Button>();

            var closeLabel = CreateText(closeObj.transform, "X");
            closeLabel.fontSize = 18;
            closeLabel.color = new Color(0.94f, 0.76f, 0.38f);
            var closeLblRect = closeLabel.GetComponent<RectTransform>();
            closeLblRect.anchorMin = Vector2.zero;
            closeLblRect.anchorMax = Vector2.one;
            closeLblRect.offsetMin = Vector2.zero;
            closeLblRect.offsetMax = Vector2.zero;

            popupRoot.SetActive(false);
        }
    }
}
