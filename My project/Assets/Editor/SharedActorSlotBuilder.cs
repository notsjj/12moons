using TMPro;
using TwelveMoons.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools
{
    public static class SharedActorSlotBuilder
    {
        [MenuItem("Twelve Moons/Setup/Rebuild Shared Actor Slot Only")]
        public static void RebuildSharedActorSlotOnly()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Fail("Canvas not found. SharedActorSlot builder only updates existing desk UI.");
                return;
            }

            var deskPanel = canvas.transform.Find("DeskPanel");
            if (deskPanel == null)
            {
                Fail("DeskPanel not found under Canvas. SharedActorSlot builder will not create the desk.");
                return;
            }

            var existing = deskPanel.Find("SharedActorSlot");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var slot = BuildSharedActorSlot(deskPanel);
            RebindSharedActorSlotReferences(deskPanel, slot.GetComponent<SharedActorSlotView>());
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = slot;
            Debug.Log("SharedActorSlot rebuilt only. Other desk UI objects were not changed.");
        }

        private static GameObject BuildSharedActorSlot(Transform parent)
        {
            var slot = CreateUiChild(parent, "SharedActorSlot");
            SetFixedRect(slot.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(-260f, 56f), new Vector2(240f, 280f), new Vector2(0f, 0.5f));
            EnsureComponent<RectMask2D>(slot);

            var canvasGroup = EnsureComponent<CanvasGroup>(slot);
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            var actorRoot = CreateUiChild(slot.transform, "ActorRoot");
            SetFixedRect(actorRoot.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(240f, 280f), new Vector2(0f, 0.5f));
            ConfigureImage(actorRoot, new Color(0.12f, 0.115f, 0.1f, 0.96f), false);

            var portrait = CreateImage(actorRoot.transform, "PortraitImage", new Color(0.18f, 0.17f, 0.15f, 1f));
            SetFixedRect(portrait.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(192f, 174f), new Vector2(0.5f, 1f));

            var nameText = CreateText(actorRoot.transform, "NameText", "", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            SetFixedRect(nameText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(208f, 30f), new Vector2(0.5f, 0f));

            var roleText = CreateText(actorRoot.transform, "RoleText", "", 13, FontStyles.Normal, TextAlignmentOptions.Center);
            SetFixedRect(roleText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(208f, 24f), new Vector2(0.5f, 0f));

            var feedbackBackground = CreateImage(actorRoot.transform, "ProposerFeedbackBackground", new Color(0.08f, 0.075f, 0.065f, 0.92f));
            SetFixedRect(feedbackBackground.rectTransform, new Vector2(1f, 0.5f), new Vector2(20f, 0f), new Vector2(244f, 144f), new Vector2(0f, 0.5f));

            var proposerFeedbackText = CreateText(feedbackBackground.transform, "ProposerFeedbackText", "", 13, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            SetStretchRect(proposerFeedbackText.rectTransform, new Vector2(14f, 12f), new Vector2(-14f, -12f));

            var slotView = EnsureComponent<SharedActorSlotView>(slot);
            var serializedObject = new SerializedObject(slotView);
            serializedObject.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            serializedObject.FindProperty("portraitImage").objectReferenceValue = portrait;
            serializedObject.FindProperty("nameText").objectReferenceValue = nameText;
            serializedObject.FindProperty("roleText").objectReferenceValue = roleText;
            serializedObject.FindProperty("visibleClipRoot").objectReferenceValue = slot.GetComponent<RectTransform>();
            serializedObject.FindProperty("visibleClipMask").objectReferenceValue = slot.GetComponent<RectMask2D>();
            serializedObject.FindProperty("proposerFeedbackBackground").objectReferenceValue = feedbackBackground.gameObject;
            serializedObject.FindProperty("proposerFeedbackText").objectReferenceValue = proposerFeedbackText;
            serializedObject.FindProperty("actorRoot").objectReferenceValue = actorRoot.GetComponent<RectTransform>();
            serializedObject.FindProperty("hiddenMoveLeftDistance").floatValue = 284f;
            serializedObject.FindProperty("slideDuration").floatValue = 0.8f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return slot;
        }

        private static void RebindSharedActorSlotReferences(Transform deskPanel, SharedActorSlotView slotView)
        {
            AssignSharedActorSlot(deskPanel.GetComponent<DeskPanelView>(), slotView);
            AssignSharedActorSlot(deskPanel.GetComponent<DeskLoopController>(), slotView);
            AssignSharedActorSlot(deskPanel.GetComponent<DeskDebugControls>(), slotView);

            var documentPopup = deskPanel.Find("DocumentPopupPanel");
            if (documentPopup != null)
            {
                AssignSharedActorSlot(documentPopup.GetComponent<DocumentPopupPanelView>(), slotView);
            }
        }

        private static void AssignSharedActorSlot(Object target, SharedActorSlotView slotView)
        {
            if (target == null || slotView == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty("sharedActorSlot");
            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = slotView;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateUiChild(Transform parent, string childName)
        {
            var childObject = new GameObject(childName, typeof(RectTransform));
            childObject.transform.SetParent(parent, false);
            return childObject;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var imageObject = CreateUiChild(parent, name);
            return ConfigureImage(imageObject, color, false);
        }

        private static Image ConfigureImage(GameObject target, Color color, bool raycastTarget)
        {
            var image = EnsureComponent<Image>(target);
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            var textObject = CreateUiChild(parent, name);
            RemoveLegacyText(textObject);
            var text = EnsureComponent<TextMeshProUGUI>(textObject);
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void SetFixedRect(
            RectTransform rectTransform,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 pivot)
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
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
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
            EditorUtility.DisplayDialog("Rebuild Shared Actor Slot Only", message, "OK");
        }
    }
}
