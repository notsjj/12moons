using System.IO;
using System.Reflection;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class DeskLoopButtonBindingSmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run Desk Loop Button Binding Smoke Test")]
        public static void Run()
        {
            var root = new GameObject("DeskLoopButtonBindingSmokeTest");
            try
            {
                var documentButton = CreateButton(root.transform, "\u516c\u6587\u6309\u94ae");
                var newspaperButton = CreateButton(root.transform, "\u62a5\u7eb8\u6309\u94ae");
                var cityButton = CreateButton(root.transform, "\u57ce\u533a\u6309\u94ae");
                CreateMask(cityButton.transform, "\u5de6\u906e\u7f69", -12f);
                CreateMask(cityButton.transform, "\u53f3\u906e\u7f69", 12f);
                cityButton.gameObject.SetActive(false);

                var controller = root.AddComponent<DeskLoopController>();

                InvokePrivate(controller, "ResolveDependencies");
                InvokePrivate(controller, "RefreshButtons");

                AssertButtonBound(controller, "documentButton", documentButton, "\u516c\u6587\u6309\u94ae");
                AssertButtonBound(controller, "newspaperButton", newspaperButton, "\u62a5\u7eb8\u6309\u94ae");
                AssertButtonBound(controller, "cityButton", cityButton, "\u57ce\u533a\u6309\u94ae");
                AssertCityButtonVisibleAndReady(cityButton);
                AssertCityButtonMasksResolved(controller);
                AssertCityButtonMaskRevealDistance(controller);
                AssertCityButtonMaskRevealDuration(controller);
                AssertDocumentButtonHiddenDuringDocumentFlow(controller, documentButton, root.transform);

                Debug.Log("\u684c\u9762\u6d41\u7a0b\u6309\u94ae\u5173\u8054\u5192\u70df\u6d4b\u8bd5\u901a\u8fc7\uff1a\u516c\u6587\u6309\u94ae\u3001\u62a5\u7eb8\u6309\u94ae\u3001\u57ce\u533a\u6309\u94ae\u53ef\u6309\u4e2d\u6587\u5bf9\u8c61\u540d\u81ea\u52a8\u5173\u8054\u5230 DeskLoopController\uff0c\u4e14\u57ce\u533a\u6309\u94ae\u5728\u5237\u65b0\u540e\u4f1a\u663e\u793a\u3002");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Button CreateButton(Transform parent, string objectName)
        {
            var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            return buttonObject.GetComponent<Button>();
        }

        private static RectTransform CreateMask(Transform parent, string objectName, float anchoredX)
        {
            var maskObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            maskObject.transform.SetParent(parent, false);
            var rectTransform = maskObject.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(anchoredX, 0f);
            return rectTransform;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidDataException($"\u627e\u4e0d\u5230 DeskLoopController.{methodName}\uff0c\u65e0\u6cd5\u9a8c\u8bc1\u6309\u94ae\u81ea\u52a8\u5173\u8054\u3002");
            }

            method.Invoke(target, null);
        }

        private static void AssertButtonBound(
            DeskLoopController controller,
            string fieldName,
            Button expectedButton,
            string displayName)
        {
            var field = typeof(DeskLoopController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidDataException($"\u627e\u4e0d\u5230 DeskLoopController.{fieldName} \u5b57\u6bb5\uff0c\u65e0\u6cd5\u9a8c\u8bc1 {displayName} \u5173\u8054\u3002");
            }

            var actualButton = field.GetValue(controller) as Button;
            if (actualButton != expectedButton)
            {
                throw new InvalidDataException($"{displayName} \u672a\u81ea\u52a8\u5173\u8054\u5230 DeskLoopController.{fieldName}\u3002");
            }
        }

        private static void AssertCityButtonVisibleAndReady(Button cityButton)
        {
            if (!cityButton.gameObject.activeSelf)
            {
                throw new InvalidDataException("\u57ce\u533a\u6309\u94ae\u5237\u65b0\u540e\u4ecd\u672a\u663e\u793a\uff0c\u516c\u6587\u7ed3\u675f\u56de\u5230\u684c\u9762\u540e\u65e0\u6cd5\u8fdb\u5165\u57ce\u533a\u3002");
            }

            if (!cityButton.interactable)
            {
                throw new InvalidDataException("\u57ce\u533a\u6309\u94ae\u5237\u65b0\u540e\u4e0d\u53ef\u70b9\u51fb\uff0c\u516c\u6587\u7ed3\u675f\u540e\u5e94\u5141\u8bb8\u6253\u5f00\u57ce\u533a\u3002");
            }
        }

        private static void AssertCityButtonMasksResolved(DeskLoopController controller)
        {
            var method = typeof(DeskLoopController).GetMethod("TryResolveCityButtonRevealMasks", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidDataException("\u627e\u4e0d\u5230\u57ce\u533a\u6309\u94ae\u906e\u7f69\u8bc6\u522b\u65b9\u6cd5\uff0c\u65e0\u6cd5\u9a8c\u8bc1\u57ce\u533a\u6309\u94ae\u5148\u62c9\u5f00\u906e\u7f69\u518d\u8fdb\u5165\u8fc7\u573a\u3002");
            }

            var parameters = new object[] { null, null };
            var resolved = (bool)method.Invoke(controller, parameters);
            if (!resolved || parameters[0] == null || parameters[1] == null)
            {
                throw new InvalidDataException("\u57ce\u533a\u6309\u94ae\u4e0b\u7684\u4e24\u4e2a\u906e\u7f69\u6ca1\u6709\u88ab\u8bc6\u522b\u51fa\u6765\u3002");
            }
        }

        private static void AssertCityButtonMaskRevealDistance(DeskLoopController controller)
        {
            var field = typeof(DeskLoopController).GetField("cityButtonMaskRevealDistance", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidDataException("\u627e\u4e0d\u5230\u57ce\u533a\u6309\u94ae\u906e\u7f69\u62c9\u5f00\u8ddd\u79bb\u5b57\u6bb5\u3002");
            }

            var distance = (float)field.GetValue(controller);
            if (distance < 360f)
            {
                throw new InvalidDataException($"\u57ce\u533a\u6309\u94ae\u906e\u7f69\u62c9\u5f00\u8ddd\u79bb\u4e0d\u8db3\uff0c\u5f53\u524d\u4e3a {distance}\uff0c\u5e94\u81f3\u5c11\u4e3a 360\u3002");
            }
        }

        private static void AssertCityButtonMaskRevealDuration(DeskLoopController controller)
        {
            var field = typeof(DeskLoopController).GetField("cityButtonMaskRevealDuration", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidDataException("\u627e\u4e0d\u5230\u57ce\u533a\u6309\u94ae\u906e\u7f69\u62c9\u5f00\u65f6\u957f\u5b57\u6bb5\u3002");
            }

            var duration = (float)field.GetValue(controller);
            if (duration < 0.5f)
            {
                throw new InvalidDataException($"\u57ce\u533a\u6309\u94ae\u906e\u7f69\u62c9\u5f00\u901f\u5ea6\u8fc7\u5feb\uff0c\u5f53\u524d\u65f6\u957f\u4e3a {duration}\uff0c\u5e94\u81f3\u5c11\u4e3a 0.5 \u79d2\u3002");
            }
        }

        private static void AssertDocumentButtonHiddenDuringDocumentFlow(
            DeskLoopController controller,
            Button documentButton,
            Transform parent)
        {
            var popupObject = new GameObject("DocumentPopupPanelView_Test", typeof(RectTransform));
            popupObject.transform.SetParent(parent, false);
            var popup = popupObject.AddComponent<DocumentPopupPanelView>();
            SetPrivateField(popup, "waitingForContinue", true);
            SetPrivateField(controller, "documentPopupPanel", popup);

            documentButton.gameObject.SetActive(true);
            InvokePrivate(controller, "RefreshButtons");
            if (documentButton.gameObject.activeSelf)
            {
                throw new InvalidDataException("\u516c\u6587\u6d41\u7a0b\u6253\u5f00\u65f6\u516c\u6587\u6309\u94ae\u6ca1\u6709\u9690\u85cf\u3002");
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidDataException($"\u627e\u4e0d\u5230 {target.GetType().Name}.{fieldName} \u5b57\u6bb5\uff0c\u65e0\u6cd5\u8bbe\u7f6e\u6d4b\u8bd5\u72b6\u6001\u3002");
            }

            field.SetValue(target, value);
        }
    }
}
