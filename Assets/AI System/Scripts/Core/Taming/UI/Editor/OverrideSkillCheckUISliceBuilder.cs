using Artifika.UI.Override;
using Michsky.UI.Reach;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Artifika.UI.Override.Editor
{
    public static class OverrideSkillCheckUISliceBuilder
    {
        private const string RootFolder = "Assets/Project/Systems/AI System/UI/Override Prototype";
        private const string DataFolder = RootFolder + "/Data";
        private const string ScenesFolder = RootFolder + "/Scenes";
        private const string ConfigPath = DataFolder + "/SO_OverrideSkillCheckUIConfig.asset";
        private const string PrefabPath = RootFolder + "/P_UI_OverrideSkillCheckHUD.prefab";
        private const string ScenePath = ScenesFolder + "/Scene_OverrideSkillCheckUISlice.unity";
        private const string PreviewSceneName = "Scene_OverrideSkillCheckUISlice";
        private const string CircularSpritePath =
            "Assets/ThirdParty/UI/Progress Bar Kit/Resources/circular_bar.png";

        [MenuItem("Tools/Elysium/Override UI/Rebuild Isolated Slice")]
        public static void RebuildSlice()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(DataFolder);
            EnsureFolder(ScenesFolder);

            UIManager reachTheme = Resources.Load<UIManager>("Reach UI Manager");
            OverrideSkillCheckUIConfigSO config = GetOrCreateConfig(reachTheme);

            Scene previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            previewScene.name = PreviewSceneName;
            CloseOtherLoadedSliceScenes(previewScene);
            SceneManager.SetActiveScene(previewScene);

            GameObject prefabRoot = BuildHUDRoot(config, reachTheme);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            Object.DestroyImmediate(prefabRoot);

            BuildPreviewScene(prefab, config, reachTheme);
            if (!EditorSceneManager.SaveScene(previewScene, ScenePath))
                throw new System.InvalidOperationException($"Could not save override UI preview scene at '{ScenePath}'.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log($"[Override UI] Rebuilt HUD prefab and isolated scene.\nPrefab: {PrefabPath}\nScene: {ScenePath}");
        }

        private static void CloseOtherLoadedSliceScenes(Scene keepScene)
        {
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.handle == keepScene.handle || scene.name != PreviewSceneName)
                    continue;

                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static OverrideSkillCheckUIConfigSO GetOrCreateConfig(UIManager theme)
        {
            OverrideSkillCheckUIConfigSO config =
                AssetDatabase.LoadAssetAtPath<OverrideSkillCheckUIConfigSO>(ConfigPath);

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<OverrideSkillCheckUIConfigSO>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.SetReachTheme(theme);
            EditorUtility.SetDirty(config);
            return config;
        }

        private static GameObject BuildHUDRoot(
            OverrideSkillCheckUIConfigSO config,
            UIManager theme)
        {
            Sprite ringSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CircularSpritePath);
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            GameObject root = CreateRect("P_UI_OverrideSkillCheckHUD", null, Vector2.one * config.RootSize);
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            OverrideSkillCheckHUDView view = root.AddComponent<OverrideSkillCheckHUDView>();
            OverrideSkillCheckHUDPresenter presenter = root.AddComponent<OverrideSkillCheckHUDPresenter>();
            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;
            OverrideSkillCheckAudioFeedback audioFeedback =
                root.AddComponent<OverrideSkillCheckAudioFeedback>();
            audioFeedback.SetAudioSource(audioSource);

            RectTransform content = CreateRect("Content", root.transform, Vector2.one * config.RootSize)
                .GetComponent<RectTransform>();

            Image outerDiamond = AddImage(
                CreateRect("Outer Diamond", content, new Vector2(430f, 430f)),
                uiSprite,
                WithAlpha(config.AccentColor, 0.08f),
                Image.Type.Sliced);
            outerDiamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            Image innerDiamond = AddImage(
                CreateRect("Inner Diamond", content, new Vector2(414f, 414f)),
                uiSprite,
                WithAlpha(config.BackgroundColor, 0.62f),
                Image.Type.Sliced);
            innerDiamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            OverrideTimingRingMinigameView timingMinigame = CreateTimingRingMinigame(
                content,
                config,
                theme,
                ringSprite,
                uiSprite);
            OverridePulseStabilizationMinigameView pulseMinigame =
                CreatePulseStabilizationMinigame(
                    content,
                    config,
                    theme,
                    ringSprite,
                    uiSprite);

            TextMeshProUGUI title = CreateText(
                "Title",
                content,
                "ROBOT OVERRIDE",
                17f,
                new Vector2(340f, 32f),
                new Vector2(0f, 316f),
                theme,
                UIManagerText.FontType.Semibold,
                UIManagerText.ColorType.Primary);
            title.characterSpacing = 7f;

            TextMeshProUGUI hint = CreateText(
                "Input Hint",
                content,
                "HOLD ATTACK  /  PRESS SPACE",
                12f,
                new Vector2(390f, 30f),
                new Vector2(0f, -232f),
                theme,
                UIManagerText.FontType.Medium,
                UIManagerText.ColorType.Primary,
                0.62f);
            hint.characterSpacing = 2.5f;

            TextMeshProUGUI progress = CreateText(
                "Override Progress",
                content,
                "40%",
                12f,
                new Vector2(120f, 24f),
                new Vector2(150f, 273f),
                theme,
                UIManagerText.FontType.Bold,
                UIManagerText.ColorType.Accent,
                alignment: TextAlignmentOptions.Right);
            progress.characterSpacing = 2.5f;

            RectTransform terminalFeedbackRoot = CreateRect(
                    "Session Result Feedback",
                    content,
                    new Vector2(500f, 104f))
                .GetComponent<RectTransform>();
            CanvasGroup terminalFeedbackCanvasGroup =
                terminalFeedbackRoot.gameObject.AddComponent<CanvasGroup>();
            terminalFeedbackCanvasGroup.alpha = 0f;
            terminalFeedbackCanvasGroup.interactable = false;
            terminalFeedbackCanvasGroup.blocksRaycasts = false;

            Image terminalBackdrop = AddImage(
                CreateStretchedRect("Terminal Backdrop", terminalFeedbackRoot, Vector2.zero),
                uiSprite,
                WithAlpha(config.NegativeColor, 0.26f),
                Image.Type.Sliced);

            Image terminalTopLine = AddImage(
                CreateRect("Terminal Top Line", terminalFeedbackRoot, new Vector2(420f, 2f)),
                uiSprite,
                config.NegativeColor,
                Image.Type.Sliced);
            terminalTopLine.rectTransform.anchoredPosition = new Vector2(0f, 50f);

            Image terminalBottomLine = AddImage(
                CreateRect("Terminal Bottom Line", terminalFeedbackRoot, new Vector2(420f, 2f)),
                uiSprite,
                config.NegativeColor,
                Image.Type.Sliced);
            terminalBottomLine.rectTransform.anchoredPosition = new Vector2(0f, -50f);

            TextMeshProUGUI terminalText = CreateText(
                "Terminal Result Text",
                terminalFeedbackRoot,
                "LINK LOST",
                24f,
                new Vector2(460f, 48f),
                Vector2.zero,
                theme,
                UIManagerText.FontType.Bold,
                UIManagerText.ColorType.Negative);
            terminalText.characterSpacing = 5f;

            SetObjectReference(view, "config", config);
            SetObjectReference(view, "rootCanvasGroup", canvasGroup);
            SetObjectReference(view, "contentRoot", content);
            SetObjectReference(view, "titleText", title);
            SetObjectReference(view, "hintText", hint);
            SetObjectReference(view, "progressText", progress);
            SetObjectReference(view, "terminalFeedbackCanvasGroup", terminalFeedbackCanvasGroup);
            SetObjectReference(view, "terminalFeedbackRoot", terminalFeedbackRoot);
            SetObjectReference(view, "terminalFeedbackBackdrop", terminalBackdrop);
            SetObjectReferences(
                view,
                "terminalFeedbackAccents",
                terminalTopLine,
                terminalBottomLine);
            SetObjectReference(view, "terminalFeedbackText", terminalText);
            SetObjectReference(view, "audioFeedback", audioFeedback);
            SetObjectReference(presenter, "view", view);

            view.SetMinigameViews(timingMinigame, pulseMinigame);
            view.SetConfig(config);
            view.SetEditorPreview(5, 2, OverrideMinigameType.TimingRing);
            return root;
        }

        private static OverrideTimingRingMinigameView CreateTimingRingMinigame(
            Transform parent,
            OverrideSkillCheckUIConfigSO config,
            UIManager theme,
            Sprite ringSprite,
            Sprite uiSprite)
        {
            GameObject moduleRoot = CreateRect(
                "Minigame - Timing Ring",
                parent,
                Vector2.one * config.RootSize);
            CanvasGroup moduleCanvasGroup = moduleRoot.AddComponent<CanvasGroup>();
            OverrideTimingRingMinigameView minigame =
                moduleRoot.AddComponent<OverrideTimingRingMinigameView>();

            RectTransform timingRing = CreateRect(
                    "Timing Ring",
                    moduleRoot.transform,
                    Vector2.one * config.TimingRingDiameter)
                .GetComponent<RectTransform>();

            RectTransform ambientHalo = CreateRect(
                    "Ambient Halo",
                    timingRing,
                    Vector2.one * (config.TimingRingDiameter + 28f))
                .GetComponent<RectTransform>();
            Image ambientHaloImage = CreateRingImage(
                "Ambient Halo Graphic",
                ambientHalo,
                ringSprite,
                WithAlpha(config.AccentColor, 0.08f),
                false);

            Image track = CreateRingImage(
                "Ring Track",
                timingRing,
                ringSprite,
                WithAlpha(config.PrimaryColor, 0.14f),
                false);

            RectTransform successZoneRoot = CreateRect(
                    "Success Zone Root",
                    timingRing,
                    Vector2.one * config.TimingRingDiameter)
                .GetComponent<RectTransform>();
            successZoneRoot.localRotation = Quaternion.Euler(0f, 0f, -0.58f * 360f);

            Image successZoneGlow = CreateRingImage(
                "Success Zone Glow",
                successZoneRoot,
                ringSprite,
                WithAlpha(config.SecondaryColor, 0.22f),
                true);
            successZoneGlow.fillAmount = 0.22f;
            successZoneGlow.rectTransform.sizeDelta = new Vector2(18f, 18f);

            Image successZone = CreateRingImage(
                "Success Zone",
                successZoneRoot,
                ringSprite,
                config.SecondaryColor,
                true);
            successZone.fillAmount = 0.22f;

            Image resultFlash = CreateRingImage(
                "Result Flash",
                timingRing,
                ringSprite,
                WithAlpha(config.NegativeColor, 0f),
                false);

            RectTransform needlePivot = CreateRect(
                    "Needle Pivot",
                    timingRing,
                    Vector2.one * config.TimingRingDiameter)
                .GetComponent<RectTransform>();
            needlePivot.localRotation = Quaternion.Euler(0f, 0f, -0.31f * 360f);

            GameObject needleGlowObject = CreateRect(
                "Needle Glow",
                needlePivot,
                new Vector2(13f, 68f));
            RectTransform needleGlowRect = needleGlowObject.GetComponent<RectTransform>();
            needleGlowRect.anchorMin = new Vector2(0.5f, 1f);
            needleGlowRect.anchorMax = new Vector2(0.5f, 1f);
            needleGlowRect.pivot = new Vector2(0.5f, 1f);
            needleGlowRect.anchoredPosition = new Vector2(0f, -5f);
            Image needleGlow = AddImage(
                needleGlowObject,
                uiSprite,
                WithAlpha(config.AccentColor, 0.18f),
                Image.Type.Sliced);

            GameObject needleObject = CreateRect("Needle", needlePivot, new Vector2(5f, 58f));
            RectTransform needleRect = needleObject.GetComponent<RectTransform>();
            needleRect.anchorMin = new Vector2(0.5f, 1f);
            needleRect.anchorMax = new Vector2(0.5f, 1f);
            needleRect.pivot = new Vector2(0.5f, 1f);
            needleRect.anchoredPosition = new Vector2(0f, -8f);
            Image needle = AddImage(needleObject, uiSprite, config.PrimaryColor, Image.Type.Sliced);

            Image needleCap = AddImage(
                CreateRect("Needle Cap", needlePivot, new Vector2(22f, 5f)),
                uiSprite,
                config.SecondaryColor,
                Image.Type.Sliced);
            RectTransform capRect = needleCap.rectTransform;
            capRect.anchorMin = new Vector2(0.5f, 1f);
            capRect.anchorMax = new Vector2(0.5f, 1f);
            capRect.pivot = new Vector2(0.5f, 1f);
            capRect.anchoredPosition = new Vector2(0f, -4f);

            RectTransform centerPulse = CreateRect(
                    "Center Prompt",
                    moduleRoot.transform,
                    new Vector2(238f, 102f))
                .GetComponent<RectTransform>();

            AddImage(
                CreateRect("Center Accent Frame", centerPulse, new Vector2(238f, 102f)),
                uiSprite,
                WithAlpha(config.AccentColor, 0.72f),
                Image.Type.Sliced);

            Image centerFrame = AddImage(
                CreateRect("Center Frame", centerPulse, new Vector2(226f, 90f)),
                uiSprite,
                WithAlpha(config.BackgroundColor, 0.96f),
                Image.Type.Sliced);
            AddReachImageTheme(centerFrame, theme, UIManagerImage.ColorType.Background, true);

            TextMeshProUGUI hotkey = CreateText(
                "Skill Check Key",
                centerPulse,
                "SPACE",
                31f,
                new Vector2(210f, 42f),
                new Vector2(0f, 13f),
                theme,
                UIManagerText.FontType.Bold,
                UIManagerText.ColorType.Primary);
            hotkey.characterSpacing = 8f;

            TextMeshProUGUI result = CreateText(
                "Result Text",
                centerPulse,
                "SYNC WINDOW",
                12f,
                new Vector2(210f, 24f),
                new Vector2(0f, -26f),
                theme,
                UIManagerText.FontType.Semibold,
                UIManagerText.ColorType.Secondary);
            result.characterSpacing = 3f;

            RectTransform impactRipple = CreateRect(
                    "Impact Ripple",
                    moduleRoot.transform,
                    Vector2.one * (config.TimingRingDiameter + 22f))
                .GetComponent<RectTransform>();
            Image impactRippleImage = CreateRingImage(
                "Impact Ripple Graphic",
                impactRipple,
                ringSprite,
                WithAlpha(config.AccentColor, 0f),
                false);

            RectTransform directionBadge = CreateRect(
                    "Direction Badge",
                    moduleRoot.transform,
                    new Vector2(280f, 28f))
                .GetComponent<RectTransform>();
            directionBadge.anchoredPosition = new Vector2(0f, -194f);
            TextMeshProUGUI directionText = CreateText(
                "Direction Text",
                directionBadge,
                "CW  /  0.50 RPS",
                12f,
                new Vector2(280f, 28f),
                Vector2.zero,
                theme,
                UIManagerText.FontType.Semibold,
                UIManagerText.ColorType.Accent);
            directionText.characterSpacing = 3f;

            SetObjectReference(minigame, "moduleCanvasGroup", moduleCanvasGroup);
            SetObjectReference(minigame, "timingRingRoot", timingRing);
            SetObjectReference(minigame, "ambientHaloRoot", ambientHalo);
            SetObjectReference(minigame, "successZoneRoot", successZoneRoot);
            SetObjectReference(minigame, "needlePivot", needlePivot);
            SetObjectReference(minigame, "centerPulseRoot", centerPulse);
            SetObjectReference(minigame, "impactRippleRoot", impactRipple);
            SetObjectReference(minigame, "directionBadgeRoot", directionBadge);
            SetObjectReference(minigame, "ringTrackImage", track);
            SetObjectReference(minigame, "ambientHaloImage", ambientHaloImage);
            SetObjectReference(minigame, "successZoneImage", successZone);
            SetObjectReference(minigame, "successZoneGlowImage", successZoneGlow);
            SetObjectReference(minigame, "needleImage", needle);
            SetObjectReference(minigame, "needleGlowImage", needleGlow);
            SetObjectReference(minigame, "centerFrameImage", centerFrame);
            SetObjectReference(minigame, "resultFlashImage", resultFlash);
            SetObjectReference(minigame, "impactRippleImage", impactRippleImage);
            SetObjectReference(minigame, "resultText", result);
            SetObjectReference(minigame, "directionText", directionText);
            minigame.Configure(config);
            minigame.SetEditorPreview(0.31f, 0.58f, 0.22f);
            return minigame;
        }

        private static OverridePulseStabilizationMinigameView CreatePulseStabilizationMinigame(
            Transform parent,
            OverrideSkillCheckUIConfigSO config,
            UIManager theme,
            Sprite ringSprite,
            Sprite uiSprite)
        {
            Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            GameObject moduleRoot = CreateRect(
                "Minigame - Pulse Stabilization",
                parent,
                Vector2.one * config.RootSize);
            CanvasGroup moduleCanvasGroup = moduleRoot.AddComponent<CanvasGroup>();
            OverridePulseStabilizationMinigameView minigame =
                moduleRoot.AddComponent<OverridePulseStabilizationMinigameView>();

            float diameter = config.StabilizationArenaDiameter;
            float movementRadius = Mathf.Max(
                1f,
                (diameter - config.StabilizationCursorDiameter) * 0.5f);
            float safeDiameter = movementRadius * config.StabilizationSuccessRadius * 2f;

            RectTransform arena = CreateRect(
                    "Stabilization Arena",
                    moduleRoot.transform,
                    Vector2.one * diameter)
                .GetComponent<RectTransform>();

            AddImage(
                CreateRect("Arena Backdrop", arena, Vector2.one * (diameter - 30f)),
                circleSprite,
                WithAlpha(config.BackgroundColor, 0.72f),
                Image.Type.Simple);

            AddImage(
                CreateRect("Horizontal Axis", arena, new Vector2(diameter - 58f, 2f)),
                uiSprite,
                WithAlpha(config.PrimaryColor, 0.07f),
                Image.Type.Sliced);
            AddImage(
                CreateRect("Vertical Axis", arena, new Vector2(2f, diameter - 58f)),
                uiSprite,
                WithAlpha(config.PrimaryColor, 0.07f),
                Image.Type.Sliced);

            Image arenaTrack = CreateRingImage(
                "Arena Track",
                arena,
                ringSprite,
                WithAlpha(config.PrimaryColor, 0.16f),
                false);

            Image resultFlash = AddImage(
                CreateRect("Result Flash", arena, Vector2.one * (diameter - 24f)),
                circleSprite,
                WithAlpha(config.NegativeColor, 0f),
                Image.Type.Simple);

            RectTransform safeZone = CreateRect(
                    "Safe Center",
                    arena,
                    Vector2.one * safeDiameter)
                .GetComponent<RectTransform>();
            Image safeZoneImage = AddImage(
                CreateStretchedRect("Safe Center Fill", safeZone, Vector2.zero),
                circleSprite,
                WithAlpha(config.AccentColor, 0.2f),
                Image.Type.Simple);
            CreateRingImage(
                "Safe Center Ring",
                safeZone,
                ringSprite,
                WithAlpha(config.AccentColor, 0.72f),
                false);

            RectTransform pulseRing = CreateRect(
                    "Incoming Pulse",
                    arena,
                    Vector2.one * diameter)
                .GetComponent<RectTransform>();
            Image pulseRingImage = CreateRingImage(
                "Pulse Ring Graphic",
                pulseRing,
                ringSprite,
                WithAlpha(config.SecondaryColor, 0.7f),
                false);
            pulseRing.localScale = Vector3.one * 0.48f;

            RectTransform cursor = CreateRect(
                    "Signal Cursor",
                    arena,
                    Vector2.one * config.StabilizationCursorDiameter)
                .GetComponent<RectTransform>();
            Image cursorGlow = AddImage(
                CreateStretchedRect("Cursor Glow", cursor, new Vector2(14f, 14f)),
                circleSprite,
                WithAlpha(config.AccentColor, 0.34f),
                Image.Type.Simple);
            Image cursorImage = AddImage(
                CreateStretchedRect("Cursor Core", cursor, new Vector2(-6f, -6f)),
                circleSprite,
                config.PrimaryColor,
                Image.Type.Simple);
            AddImage(
                CreateRect("Cursor Center", cursor, new Vector2(5f, 5f)),
                circleSprite,
                config.SecondaryColor,
                Image.Type.Simple);

            TextMeshProUGUI modeLabel = CreateText(
                "Mode Label",
                moduleRoot.transform,
                "STABILITY FIELD",
                11f,
                new Vector2(250f, 24f),
                new Vector2(0f, 190f),
                theme,
                UIManagerText.FontType.Medium,
                UIManagerText.ColorType.Primary,
                0.5f);
            modeLabel.characterSpacing = 4f;

            TextMeshProUGUI stateText = CreateText(
                "Stability State",
                moduleRoot.transform,
                "INCOMING PULSE",
                12f,
                new Vector2(300f, 28f),
                new Vector2(0f, -194f),
                theme,
                UIManagerText.FontType.Semibold,
                UIManagerText.ColorType.Secondary);
            stateText.characterSpacing = 3f;

            SetObjectReference(minigame, "moduleCanvasGroup", moduleCanvasGroup);
            SetObjectReference(minigame, "arenaRoot", arena);
            SetObjectReference(minigame, "safeZoneRect", safeZone);
            SetObjectReference(minigame, "pulseRingRect", pulseRing);
            SetObjectReference(minigame, "cursorRect", cursor);
            SetObjectReference(minigame, "arenaTrackImage", arenaTrack);
            SetObjectReference(minigame, "safeZoneImage", safeZoneImage);
            SetObjectReference(minigame, "pulseRingImage", pulseRingImage);
            SetObjectReference(minigame, "cursorGlowImage", cursorGlow);
            SetObjectReference(minigame, "cursorImage", cursorImage);
            SetObjectReference(minigame, "resultFlashImage", resultFlash);
            SetObjectReference(minigame, "stateText", stateText);
            minigame.Configure(config);
            minigame.SetEditorPreview(new Vector2(0.18f, -0.12f), 0.48f);
            return minigame;
        }

        private static void BuildPreviewScene(
            GameObject prefab,
            OverrideSkillCheckUIConfigSO config,
            UIManager theme)
        {
            GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.04f, 0.06f, 1f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            GameObject lightObject = new("Directional Light", typeof(Light));
            lightObject.GetComponent<Light>().type = LightType.Directional;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject canvasObject = new(
                "Override UI Preview Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.layer = 5;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Image background = AddImage(
                CreateStretchedRect("Preview Background", canvasObject.transform, Vector2.zero),
                null,
                new Color(0.025f, 0.04f, 0.06f, 1f),
                Image.Type.Simple);
            background.transform.SetAsFirstSibling();

            CreatePreviewDecor(canvasObject.transform, config);

            GameObject hudInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasObject.transform);
            hudInstance.name = "Override Skill Check HUD (Copy This Prefab To HUD Canvas)";
            RectTransform hudRect = hudInstance.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0.5f, 0.5f);
            hudRect.anchorMax = new Vector2(0.5f, 0.5f);
            hudRect.anchoredPosition = Vector2.zero;
            hudRect.localScale = Vector3.one;

            OverrideSkillCheckHUDView hudView = hudInstance.GetComponent<OverrideSkillCheckHUDView>();

            GameObject previewController = new("Preview Controller", typeof(OverrideSkillCheckSlicePreview));
            SetObjectReference(previewController.GetComponent<OverrideSkillCheckSlicePreview>(), "view", hudView);

            CreateText(
                "Preview Title",
                canvasObject.transform,
                "OVERRIDE MINIGAME SLICE",
                20f,
                new Vector2(420f, 36f),
                new Vector2(48f, -44f),
                theme,
                UIManagerText.FontType.Bold,
                UIManagerText.ColorType.Primary,
                1f,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                TextAlignmentOptions.Left);

            CreateText(
                "Preview Instructions",
                canvasObject.transform,
                "1  Timing Ring    2  Pulse Stabilization\nSPACE  Timing input    - / =  Needle speed\nP  Toggle post-press pause    L  Simulate loss\nMOUSE / RIGHT STICK  Stabilize\nR  Restart    [ / ]  Check count",
                13f,
                new Vector2(520f, 120f),
                new Vector2(48f, -92f),
                theme,
                UIManagerText.FontType.Regular,
                UIManagerText.ColorType.Primary,
                0.62f,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                TextAlignmentOptions.TopLeft);

            CreateText(
                "Prefab Note",
                canvasObject.transform,
                "HUD-READY PREFAB\nP_UI_OverrideSkillCheckHUD",
                12f,
                new Vector2(420f, 54f),
                new Vector2(-48f, 42f),
                theme,
                UIManagerText.FontType.Medium,
                UIManagerText.ColorType.Accent,
                0.8f,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                TextAlignmentOptions.BottomRight);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void CreatePreviewDecor(Transform parent, OverrideSkillCheckUIConfigSO config)
        {
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            Image topLine = AddImage(
                CreateRect("Top Accent Line", parent, new Vector2(520f, 2f)),
                uiSprite,
                WithAlpha(config.AccentColor, 0.5f),
                Image.Type.Sliced);
            topLine.rectTransform.anchorMin = new Vector2(0f, 1f);
            topLine.rectTransform.anchorMax = new Vector2(0f, 1f);
            topLine.rectTransform.pivot = new Vector2(0f, 1f);
            topLine.rectTransform.anchoredPosition = new Vector2(48f, -34f);

            Image bottomLine = AddImage(
                CreateRect("Bottom Accent Line", parent, new Vector2(360f, 2f)),
                uiSprite,
                WithAlpha(config.SecondaryColor, 0.42f),
                Image.Type.Sliced);
            bottomLine.rectTransform.anchorMin = new Vector2(1f, 0f);
            bottomLine.rectTransform.anchorMax = new Vector2(1f, 0f);
            bottomLine.rectTransform.pivot = new Vector2(1f, 0f);
            bottomLine.rectTransform.anchoredPosition = new Vector2(-48f, 32f);
        }

        private static Image CreateRingImage(
            string name,
            Transform parent,
            Sprite sprite,
            Color color,
            bool filled)
        {
            Image image = AddImage(
                CreateStretchedRect(name, parent, Vector2.zero),
                sprite,
                color,
                filled ? Image.Type.Filled : Image.Type.Simple);
            image.preserveAspect = true;

            if (filled)
            {
                image.fillMethod = Image.FillMethod.Radial360;
                image.fillOrigin = 2;
                image.fillClockwise = true;
            }

            return image;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string value,
            float fontSize,
            Vector2 size,
            Vector2 position,
            UIManager theme,
            UIManagerText.FontType fontType,
            UIManagerText.ColorType colorType,
            float alpha = 1f,
            Vector2? anchorMin = null,
            Vector2? anchorMax = null,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            GameObject textObject = CreateRect(name, parent, size);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
            rect.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            text.color = new Color(1f, 1f, 1f, alpha);

            UIManagerText reachText = textObject.AddComponent<UIManagerText>();
            reachText.UIManagerAsset = theme;
            reachText.fontType = fontType;
            reachText.colorType = colorType;
            reachText.useCustomAlpha = true;
            reachText.UpdateText();
            return text;
        }

        private static void AddReachImageTheme(
            Image image,
            UIManager theme,
            UIManagerImage.ColorType colorType,
            bool keepAlpha)
        {
            UIManagerImage reachImage = image.gameObject.AddComponent<UIManagerImage>();
            reachImage.UIManagerAsset = theme;
            reachImage.colorType = colorType;
            reachImage.useCustomAlpha = keepAlpha;
            reachImage.UpdateImage();
        }

        private static GameObject CreateRect(
            string name,
            Transform parent,
            Vector2 size)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.layer = 5;
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            return gameObject;
        }

        private static GameObject CreateStretchedRect(
            string name,
            Transform parent,
            Vector2 sizeDelta)
        {
            GameObject gameObject = CreateRect(name, parent, Vector2.zero);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = Vector2.zero;
            return gameObject;
        }

        private static Image AddImage(
            GameObject gameObject,
            Sprite sprite,
            Color color,
            Image.Type type)
        {
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = type;
            image.raycastTarget = false;
            return image;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[Override UI] Missing serialized property '{propertyName}' on {target.name}.");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectReferences(
            Object target,
            string propertyName,
            params Object[] values)
        {
            SerializedObject serializedObject = new(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                Debug.LogError($"[Override UI] Missing serialized array '{propertyName}' on {target.name}.");
                return;
            }

            property.arraySize = values?.Length ?? 0;
            for (int i = 0; i < property.arraySize; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
