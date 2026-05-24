using System.Linq;
using TMPro;
using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using TwelveMoons.UI.City;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools
{
    public static class CityOverlayUiOnlyBuilder
    {
        [MenuItem("Twelve Moons/Setup/Create City Overlay UI Only")]
        public static void CreateCityOverlayUiOnly()
        {
            var cityRoot = FindCityRoot();
            if (cityRoot == null)
            {
                Fail("找不到 CityRoot。本工具只做城区覆盖层局部更新，不会重建桌面或基础场景。");
                return;
            }

            var runtimeDataService = FindRequired<RuntimeDataService>("RuntimeDataService");
            var factionService = FindRequired<FactionService>("FactionService");
            if (runtimeDataService == null || factionService == null)
            {
                Fail("缺少运行时或阵营服务。请先保留已有基础服务后再运行本局部工具。");
                return;
            }

            var sharedHudRoot = FindOrCreateSharedHudRoot(cityRoot.transform);
            var taskPanel = FindSharedPanel<TaskPanelView>("TaskPanel", "任务栏");
            var roundPanel = FindSharedPanel<RoundPanelView>("RoundPanel", "回合面板");
            if (taskPanel == null || roundPanel == null)
            {
                Fail("找不到现有 TaskPanel 或 RoundPanel。本工具只复用已有桌面面板，不会新建城区副本。");
                return;
            }

            MoveSharedPanelToHudRoot(taskPanel.transform, sharedHudRoot);
            MoveSharedPanelToHudRoot(roundPanel.transform, sharedHudRoot);

            var overlay = FindOrCreateUiChild(cityRoot.transform, "CityOverlayPanel");
            SetStretchRect(overlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            RemoveObsoleteOverlayPanel(overlay.transform, "CityTaskPanel");
            RemoveObsoleteOverlayPanel(overlay.transform, "CityRoundPanel");

            var suspicionPanel = BuildCitySuspicionPanel(overlay.transform, factionService, runtimeDataService);

            var overlayView = EnsureComponent<CityOverlayPanelView>(overlay);
            var serializedObject = new SerializedObject(overlayView);
            serializedObject.FindProperty("taskPanel").objectReferenceValue = taskPanel;
            serializedObject.FindProperty("citySuspicionPanel").objectReferenceValue = suspicionPanel.GetComponent<SuspicionPanelView>();
            serializedObject.FindProperty("roundPanel").objectReferenceValue = roundPanel;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = overlay;
            Debug.Log("城区覆盖层已局部更新：复用现有 TaskPanel 和 RoundPanel，仅保留城区专用质疑栏。");
        }

        private static GameObject FindCityRoot()
        {
            var gameEntry = Object.FindFirstObjectByType<TwelveMoons.Core.GameEntry>(FindObjectsInactive.Include);
            if (gameEntry != null && gameEntry.CityRoot != null)
            {
                return gameEntry.CityRoot;
            }

            return GameObject.Find("CityRoot");
        }

        private static Transform FindOrCreateSharedHudRoot(Transform cityRoot)
        {
            var parent = cityRoot != null && cityRoot.parent != null ? cityRoot.parent : cityRoot;
            var existing = parent != null ? parent.Find("SharedHudRoot") : null;
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return existing;
            }

            var sharedRoot = new GameObject("SharedHudRoot", typeof(RectTransform));
            sharedRoot.transform.SetParent(parent, false);
            SetStretchRect(sharedRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            return sharedRoot.transform;
        }

        private static void MoveSharedPanelToHudRoot(Transform panel, Transform sharedHudRoot)
        {
            if (panel == null || sharedHudRoot == null || panel.parent == sharedHudRoot)
            {
                return;
            }

            panel.SetParent(sharedHudRoot, false);
            panel.gameObject.SetActive(true);
            Debug.Log($"已将共享面板移动到 SharedHudRoot：{panel.name}");
        }

        private static GameObject BuildCitySuspicionPanel(
            Transform parent,
            FactionService factionService,
            RuntimeDataService runtimeDataService)
        {
            var panel = FindOrCreateUiChild(parent, "CitySuspicionPanel");
            SetFixedRect(panel.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-28f, -90f), new Vector2(420f, 300f), new Vector2(1f, 0.5f));
            ConfigurePanelImage(panel, new Color(0.07f, 0.09f, 0.1f, 0.86f));

            var title = FindOrCreateText(panel.transform, "TitleText", "城区质疑度", 20, FontStyles.Bold, TextAlignmentOptions.Left);
            SetStretchTopRect(title.rectTransform, new Vector2(18f, -44f), new Vector2(-18f, -12f));

            var content = FindOrCreateUiChild(panel.transform, "CitySuspicionContent");
            SetStretchRect(content.GetComponent<RectTransform>(), new Vector2(16f, 18f), new Vector2(-16f, -58f));
            var layout = EnsureComponent<VerticalLayoutGroup>(content);
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 10f;

            var factionIds = factionService.Definitions.Count > 0
                ? factionService.Definitions.Select(definition => definition.FactionId).ToArray()
                : new[] { "noble", "academy", "church", "civilian" };
            var rows = factionIds.Select(id => CreateCityFactionRow(content.transform, id)).ToArray();

            var panelView = EnsureComponent<SuspicionPanelView>(panel);
            var serializedObject = new SerializedObject(panelView);
            serializedObject.FindProperty("factionService").objectReferenceValue = factionService;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("contentRoot").objectReferenceValue = content.GetComponent<RectTransform>();
            var rowsProperty = serializedObject.FindProperty("factionRows");
            rowsProperty.arraySize = rows.Length;
            for (var index = 0; index < rows.Length; index++)
            {
                rowsProperty.GetArrayElementAtIndex(index).objectReferenceValue = rows[index];
            }

            serializedObject.FindProperty("feedbackText").objectReferenceValue = null;
            serializedObject.FindProperty("pointerIcon").objectReferenceValue = null;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        private static FactionSuspicionRow CreateCityFactionRow(Transform parent, string factionId)
        {
            var rowObject = FindOrCreateUiChild(parent, $"{factionId}CitySuspicionRow");
            SetFixedRect(rowObject.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(380f, 48f), new Vector2(0.5f, 1f));
            ConfigurePanelImage(rowObject, new Color(1f, 1f, 1f, 0.04f));

            var icon = FindOrCreateUiChild(rowObject.transform, "FactionIcon");
            SetFixedRect(icon.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(36f, 36f), new Vector2(0.5f, 0.5f));
            var iconImage = EnsureComponent<Image>(icon);
            iconImage.color = new Color(0.82f, 0.78f, 0.58f, 1f);

            var sliderObject = FindOrCreateUiChild(rowObject.transform, "SuspicionSlider");
            SetFixedRect(sliderObject.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(190f, 0f), new Vector2(250f, 18f), new Vector2(0.5f, 0.5f));
            var slider = EnsureComponent<Slider>(sliderObject);
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;

            var background = FindOrCreateUiChild(sliderObject.transform, "Background");
            SetStretchRect(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var backgroundImage = EnsureComponent<Image>(background);
            backgroundImage.color = new Color(0.22f, 0.24f, 0.25f, 1f);

            var fillArea = FindOrCreateUiChild(sliderObject.transform, "Fill Area");
            SetStretchRect(fillArea.GetComponent<RectTransform>(), new Vector2(2f, 2f), new Vector2(-2f, -2f));
            var fill = FindOrCreateUiChild(fillArea.transform, "Fill");
            SetStretchRect(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var fillImage = EnsureComponent<Image>(fill);
            fillImage.color = new Color(0.86f, 0.32f, 0.28f, 1f);
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fillImage;

            var valueText = FindOrCreateText(rowObject.transform, "ValueText", "0/100", 15, FontStyles.Bold, TextAlignmentOptions.Right);
            SetFixedRect(valueText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(76f, 26f), new Vector2(1f, 0.5f));

            var row = EnsureComponent<FactionSuspicionRow>(rowObject);
            row.SetFactionId(factionId);
            row.Configure(null, valueText, slider, iconImage, rowObject.GetComponent<Image>(), backgroundImage, fillImage);
            return row;
        }

        private static T FindRequired<T>(string label) where T : Object
        {
            var component = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (component == null)
            {
                Debug.LogWarning($"{label} not found. City overlay setup will not create unrelated systems.");
            }

            return component;
        }

        private static T FindSharedPanel<T>(string objectName, string displayName) where T : Component
        {
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in transforms)
            {
                if (candidate == null || candidate.name != objectName)
                {
                    continue;
                }

                var panel = candidate.GetComponent<T>();
                if (panel != null)
                {
                    return panel;
                }
            }

            Debug.LogWarning($"找不到现有 {objectName}，无法绑定城区共享{displayName}。");
            return null;
        }

        private static void RemoveObsoleteOverlayPanel(Transform overlay, string childName)
        {
            var child = overlay != null ? overlay.Find(childName) : null;
            if (child == null)
            {
                return;
            }

            Object.DestroyImmediate(child.gameObject);
            Debug.Log($"已清理旧城区重复面板：{childName}");
        }

        private static GameObject FindOrCreateUiChild(Transform parent, string childName)
        {
            var existing = parent.Find(childName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var childObject = new GameObject(childName, typeof(RectTransform));
            childObject.transform.SetParent(parent, false);
            return childObject;
        }

        private static TextMeshProUGUI FindOrCreateText(
            Transform parent,
            string name,
            string text,
            int fontSize,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            var textObject = FindOrCreateUiChild(parent, name);
            RemoveLegacyText(textObject);
            var tmp = EnsureComponent<TextMeshProUGUI>(textObject);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void ConfigurePanelImage(GameObject target, Color color)
        {
            var image = EnsureComponent<Image>(target);
            image.color = color;
        }

        private static void SetFixedRect(RectTransform rectTransform, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
        }

        private static void SetStretchRect(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static void SetStretchTopRect(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static void RemoveLegacyText(GameObject gameObject)
        {
            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (component != null && component.GetType().FullName == "UnityEngine.UI.Text")
                {
                    Object.DestroyImmediate(component);
                }
            }
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void Fail(string message)
        {
            Debug.LogError(message);
            EditorUtility.DisplayDialog("Create City Overlay UI Only", message, "OK");
        }
    }
}
