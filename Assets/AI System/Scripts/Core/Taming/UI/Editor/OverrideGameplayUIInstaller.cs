using Artifika.States;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Artifika.UI.Override.Editor
{
    public static class OverrideGameplayUIInstaller
    {
        private const string GlobalCanvasesPrefabPath =
            "Assets/Project/Prefabs/UI/P_UI_Canvases.prefab";
        private const string OverrideHudPrefabPath =
            "Assets/Project/Systems/AI System/UI/Override Prototype/P_UI_OverrideSkillCheckHUD.prefab";
        private const string OverridingStateProfilePath =
            "Assets/Resources/Player States/SO_PlayerStateProfile_Overriding.asset";
        private const string OverrideCanvasName = "Canvas_Override";
        private const string OverrideHudName = "P_UI_OverrideSkillCheckHUD";

        [MenuItem("Tools/Elysium/Override UI/Install Gameplay Canvas")]
        public static void Install()
        {
            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OverrideHudPrefabPath);
            if (hudPrefab == null)
                throw new System.InvalidOperationException(
                    $"Override HUD prefab was not found at '{OverrideHudPrefabPath}'.");

            InstallCanvas(hudPrefab);
            ConfigureStateProfile();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Override UI] Installed the dedicated gameplay canvas and configured the Overriding state profile.");
        }

        private static void InstallCanvas(GameObject hudPrefab)
        {
            GameObject canvasesRoot = PrefabUtility.LoadPrefabContents(GlobalCanvasesPrefabPath);
            if (canvasesRoot == null)
                throw new System.InvalidOperationException(
                    $"Global canvases prefab was not found at '{GlobalCanvasesPrefabPath}'.");

            try
            {
                Transform canvasTransform = FindDirectChild(canvasesRoot.transform, OverrideCanvasName);
                GameObject canvasObject = canvasTransform != null
                    ? canvasTransform.gameObject
                    : CreateOverrideCanvas(canvasesRoot.transform);

                ConfigureOverrideCanvas(canvasObject);
                EnsureHudInstance(canvasObject.transform, hudPrefab);

                PrefabUtility.SaveAsPrefabAsset(canvasesRoot, GlobalCanvasesPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(canvasesRoot);
            }
        }

        private static GameObject CreateOverrideCanvas(Transform parent)
        {
            GameObject canvasObject = new(
                OverrideCanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.layer = 5;

            RectTransform rect = canvasObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
            return canvasObject;
        }

        private static void ConfigureOverrideCanvas(GameObject canvasObject)
        {
            canvasObject.layer = 5;

            Canvas canvas = GetOrAddComponent<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvas.pixelPerfect = false;
            canvas.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1 |
                AdditionalCanvasShaderChannels.TexCoord2 |
                AdditionalCanvasShaderChannels.TexCoord3;

            CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvasObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                Object.DestroyImmediate(raycaster);
        }

        private static void EnsureHudInstance(Transform canvasTransform, GameObject hudPrefab)
        {
            OverrideSkillCheckHUDView[] existingViews =
                canvasTransform.GetComponentsInChildren<OverrideSkillCheckHUDView>(true);

            if (existingViews.Length > 1)
                throw new System.InvalidOperationException(
                    $"'{OverrideCanvasName}' contains more than one override HUD view.");

            GameObject hudObject = existingViews.Length == 1
                ? existingViews[0].gameObject
                : (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab, canvasTransform);

            hudObject.name = OverrideHudName;
            hudObject.layer = 5;
            RectTransform rect = hudObject.GetComponent<RectTransform>();
            rect.SetParent(canvasTransform, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void ConfigureStateProfile()
        {
            StateProfileSO profile =
                AssetDatabase.LoadAssetAtPath<StateProfileSO>(OverridingStateProfilePath);
            if (profile == null)
                throw new System.InvalidOperationException(
                    $"Overriding state profile was not found at '{OverridingStateProfilePath}'.");

            profile.ShowPanels = new[] { UIPanelId.Override };
            EditorUtility.SetDirty(profile);
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                    return child;
            }

            return null;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
