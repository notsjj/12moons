using System.Collections.Generic;
using TMPro;
using TwelveMoons.City;
using TwelveMoons.Core;
using TwelveMoons.UI.City;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools
{
    public static class CityCameraMovementOnlyBuilder
    {
        [MenuItem("Twelve Moons/Setup/Create City Camera Movement Only")]
        public static void CreateCityCameraMovementOnly()
        {
            var cityRoot = FindCityRoot();
            if (cityRoot == null)
            {
                Fail("找不到 CityRoot。本工具只做阶段13城区摄像机局部更新，不会重建基础场景。");
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                Fail("找不到 MainCamera。请先在场景中保留一个带 MainCamera 标签的摄像机。");
                return;
            }

            var worldParent = FindCityWorldParent();
            var pointsRoot = FindOrCreateWorldPointsRoot(cityRoot.transform, worldParent);
            RemoveObsoleteChildren(pointsRoot.transform, "EastViewPoint", "WestViewPoint", "SouthViewPoint");

            var globalPoint = FindOrCreateViewPoint(pointsRoot.transform, "GlobalViewPoint", new Vector3(0f, 8f, -10f), new Vector3(45f, 0f, 0f));
            var royalPoint = FindOrCreateViewPoint(pointsRoot.transform, "RoyalViewPoint", new Vector3(0f, 5.5f, -6.5f), new Vector3(36f, 0f, 0f));
            var churchPoint = FindOrCreateViewPoint(pointsRoot.transform, "ChurchViewPoint", new Vector3(-5.5f, 5f, -6.5f), new Vector3(36f, 32f, 0f));
            var upperCityPoint = FindOrCreateViewPoint(pointsRoot.transform, "UpperCityViewPoint", new Vector3(5.5f, 5f, -6.5f), new Vector3(36f, -32f, 0f));
            var academyPoint = FindOrCreateViewPoint(pointsRoot.transform, "AcademyViewPoint", new Vector3(0f, 4.5f, -4.8f), new Vector3(30f, 0f, 0f));
            var lowerCityPoint = FindOrCreateViewPoint(pointsRoot.transform, "LowerCityViewPoint", new Vector3(0f, 4.2f, -8.5f), new Vector3(34f, 0f, 0f));

            var controller = camera.GetComponent<CityCameraController>() ?? camera.gameObject.AddComponent<CityCameraController>();
            ConfigureController(controller, camera, globalPoint, royalPoint, churchPoint, upperCityPoint, academyPoint, lowerCityPoint);

            var controls = BuildControls(cityRoot.transform, controller);
            controls.SetActive(true);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = controller.gameObject;
            Debug.Log("阶段13城区摄像机移动系统已局部创建：CityCameraViewPoints 位于世界空间，CityCameraControls 保留在 CityRoot。");
        }

        private static GameObject FindCityRoot()
        {
            var gameEntry = Object.FindFirstObjectByType<GameEntry>(FindObjectsInactive.Include);
            if (gameEntry != null && gameEntry.CityRoot != null)
            {
                return gameEntry.CityRoot;
            }

            return GameObject.Find("CityRoot");
        }

        private static Transform FindCityWorldParent()
        {
            var cityMap = FindSceneObjectByName("City_01");
            if (cityMap != null)
            {
                return cityMap.transform.parent;
            }

            var fallbackRoot = GameObject.Find("CityWorldRoot");
            if (fallbackRoot == null)
            {
                fallbackRoot = new GameObject("CityWorldRoot");
                Debug.LogWarning("找不到 3D 地图 City_01，已在场景根节点创建 CityWorldRoot 承载摄像机点位。");
            }
            else
            {
                Debug.LogWarning("找不到 3D 地图 City_01，复用场景中的 CityWorldRoot 承载摄像机点位。");
            }

            return fallbackRoot.transform;
        }

        private static GameObject FindSceneObjectByName(string objectName)
        {
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in transforms)
            {
                if (candidate != null && candidate.name == objectName)
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private static GameObject FindOrCreateWorldPointsRoot(Transform oldUiParent, Transform worldParent)
        {
            var existing = FindSceneObjectByName("CityCameraViewPoints");
            if (existing == null)
            {
                existing = new GameObject("CityCameraViewPoints");
            }

            if (existing.transform.parent != worldParent)
            {
                existing.transform.SetParent(worldParent, false);
                Debug.Log("已将 CityCameraViewPoints 移到世界空间父节点，避免放在 Canvas/CityRoot 下。");
            }

            var oldRoot = oldUiParent != null ? oldUiParent.Find("CityCameraViewPoints") : null;
            if (oldRoot != null && oldRoot.gameObject != existing)
            {
                Object.DestroyImmediate(oldRoot.gameObject);
            }

            return existing;
        }

        private static Transform FindOrCreateViewPoint(Transform parent, string name, Vector3 position, Vector3 eulerAngles)
        {
            var point = parent.Find(name);
            if (point == null)
            {
                point = new GameObject(name).transform;
                point.SetParent(parent, false);
                point.localPosition = position;
                point.localRotation = Quaternion.Euler(eulerAngles);
            }

            return point;
        }

        private static GameObject BuildControls(Transform parent, CityCameraController controller)
        {
            var panel = FindOrCreateUiChild(parent, "CityCameraControls");
            SetFixedRect(panel.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-88f, 0f), new Vector2(152f, 324f), new Vector2(1f, 0.5f));
            ConfigurePanelImage(panel, new Color(0.08f, 0.09f, 0.1f, 0.78f));
            RemoveObsoleteChildren(panel.transform, "EastButton", "WestButton", "SouthButton");

            CreateControlButton(panel.transform, "GlobalButton", "全局", string.Empty, new Vector2(0f, 120f), controller);
            CreateControlButton(panel.transform, "RoyalButton", "王室", "city_royal", new Vector2(0f, 72f), controller);
            CreateControlButton(panel.transform, "ChurchButton", "教会", "city_church", new Vector2(0f, 24f), controller);
            CreateControlButton(panel.transform, "UpperCityButton", "上城区", "city_upper", new Vector2(0f, -24f), controller);
            CreateControlButton(panel.transform, "AcademyButton", "学院", "city_academy", new Vector2(0f, -72f), controller);
            CreateControlButton(panel.transform, "LowerCityButton", "下城区", "city_lower", new Vector2(0f, -120f), controller);
            return panel;
        }

        private static void CreateControlButton(
            Transform parent,
            string objectName,
            string label,
            string viewId,
            Vector2 anchoredPosition,
            CityCameraController controller)
        {
            var buttonObject = FindOrCreateUiChild(parent, objectName);
            SetFixedRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), anchoredPosition, new Vector2(112f, 34f), new Vector2(0.5f, 0.5f));

            var image = EnsureComponent<Image>(buttonObject);
            image.color = new Color(0.22f, 0.24f, 0.24f, 0.96f);

            var button = EnsureComponent<Button>(buttonObject);
            button.targetGraphic = image;

            var labelText = FindOrCreateText(buttonObject.transform, "Label", label, 15, FontStyles.Bold, TextAlignmentOptions.Center);
            labelText.color = Color.white;
            SetFixedRect(labelText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 24f), new Vector2(0.5f, 0.5f));

            var bridge = EnsureComponent<CityCameraControlButton>(buttonObject);
            var serializedObject = new SerializedObject(bridge);
            serializedObject.FindProperty("cameraController").objectReferenceValue = controller;
            serializedObject.FindProperty("targetViewId").stringValue = viewId;
            serializedObject.FindProperty("button").objectReferenceValue = button;
            serializedObject.FindProperty("labelText").objectReferenceValue = labelText;
            serializedObject.FindProperty("displayName").stringValue = label;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AddPersistentListenerIfMissing(button, bridge, nameof(CityCameraControlButton.MoveCamera), bridge.MoveCamera);
        }

        private static void ConfigureController(
            CityCameraController controller,
            Camera camera,
            Transform globalPoint,
            Transform royalPoint,
            Transform churchPoint,
            Transform upperCityPoint,
            Transform academyPoint,
            Transform lowerCityPoint)
        {
            var viewPoints = new List<CityCameraViewPoint>
            {
                new CityCameraViewPoint("city_royal", "王室", royalPoint),
                new CityCameraViewPoint("city_church", "教会", churchPoint),
                new CityCameraViewPoint("city_upper", "上城区", upperCityPoint),
                new CityCameraViewPoint("city_academy", "学院", academyPoint),
                new CityCameraViewPoint("city_lower", "下城区", lowerCityPoint)
            };

            var serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("cityCamera").objectReferenceValue = camera;
            serializedObject.FindProperty("defaultViewPoint").objectReferenceValue = globalPoint;
            serializedObject.FindProperty("moveDuration").floatValue = 0.6f;
            serializedObject.FindProperty("copyTargetRotation").boolValue = true;
            serializedObject.FindProperty("enableKeyboardMove").boolValue = true;
            serializedObject.FindProperty("keyboardMoveSpeed").floatValue = 6f;
            serializedObject.FindProperty("enableRightMouseRotate").boolValue = true;
            serializedObject.FindProperty("rightMouseRotateSpeed").floatValue = 3f;
            serializedObject.FindProperty("minPitchAngle").floatValue = 18f;
            serializedObject.FindProperty("maxPitchAngle").floatValue = 70f;
            serializedObject.FindProperty("minPositionX").floatValue = -10f;
            serializedObject.FindProperty("maxPositionX").floatValue = 10f;
            serializedObject.FindProperty("minPositionZ").floatValue = -12f;
            serializedObject.FindProperty("maxPositionZ").floatValue = 4f;
            serializedObject.FindProperty("minPositionY").floatValue = 3f;
            serializedObject.FindProperty("maxPositionY").floatValue = 9f;
            serializedObject.FindProperty("entryCinematicEndViewId").stringValue = "city_upper";
            serializedObject.FindProperty("entryCinematicEndViewPoint").objectReferenceValue = upperCityPoint;

            var pointsProperty = serializedObject.FindProperty("viewPoints");
            pointsProperty.arraySize = viewPoints.Count;
            for (var index = 0; index < viewPoints.Count; index++)
            {
                var element = pointsProperty.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("viewId").stringValue = viewPoints[index].ViewId;
                element.FindPropertyRelative("displayName").stringValue = viewPoints[index].DisplayName;
                element.FindPropertyRelative("target").objectReferenceValue = viewPoints[index].Target;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RemoveObsoleteChildren(Transform parent, params string[] childNames)
        {
            foreach (var childName in childNames)
            {
                var child = parent.Find(childName);
                if (child != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
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
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetFixedRect(RectTransform rectTransform, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
        }

        private static void ConfigurePanelImage(GameObject target, Color color)
        {
            var image = EnsureComponent<Image>(target);
            image.color = color;
        }

        private static void RemoveLegacyText(GameObject gameObject)
        {
            foreach (var component in gameObject.GetComponents<Component>())
            {
                var legacyTextTypeName = "UnityEngine.UI." + "Text";
                if (component != null && component.GetType().FullName == legacyTextTypeName)
                {
                    Object.DestroyImmediate(component);
                }
            }
        }

        private static void AddPersistentListenerIfMissing(Button button, Object target, string methodName, UnityAction action)
        {
            for (var index = 0; index < button.onClick.GetPersistentEventCount(); index++)
            {
                if (button.onClick.GetPersistentTarget(index) == target &&
                    button.onClick.GetPersistentMethodName(index) == methodName)
                {
                    return;
                }
            }

            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void Fail(string message)
        {
            Debug.LogError(message);
            EditorUtility.DisplayDialog("Create City Camera Movement Only", message, "OK");
        }
    }
}
