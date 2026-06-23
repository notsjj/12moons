using System.Linq;
using TwelveMoons.City;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class CityPointSceneMarkerBuilder
    {
        private const string BaseScenePath = "Assets/Scenes/BaseScene.unity";
        private const string BuildingActorPointName = "\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d";
        private const string EventPromptName = "\u4e8b\u4ef6\u63d0\u793a";
        private const string PointPortraitName = "\u70b9\u4f4d\u4eba\u7269\u7acb\u7ed8";
        private const string LegacyManName = "Man";
        private const string MarkerPrefabPath = "Assets/Resources/Prefabs/UI/\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d.prefab";

        [MenuItem("Twelve Moons/Tools/Only/\u8865\u9f50\u57ce\u533a\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d")]
        public static void AddMissingMarkersToBaseScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != BaseScenePath)
            {
                Debug.LogWarning("\u8bf7\u5148\u6253\u5f00 Assets/Scenes/BaseScene.unity\uff0c\u518d\u6267\u884c\u8865\u9f50\u57ce\u533a\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d\u3002\u5de5\u5177\u4e0d\u4f1a\u81ea\u52a8\u5207\u6362\u573a\u666f\uff0c\u907f\u514d\u8bef\u6253\u5f00\u5176\u5b83\u635f\u574f\u573a\u666f\u3002");
                return;
            }

            var markerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MarkerPrefabPath);
            var promptSprite = LoadPromptSprite(markerPrefab);
            var changed = false;

            foreach (var pointView in Object.FindObjectsByType<CityPointView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (pointView == null || string.IsNullOrEmpty(pointView.PointId) || !IsPointId(pointView.PointId))
                {
                    continue;
                }

                var marker = pointView.transform.Find(BuildingActorPointName);
                if (marker == null)
                {
                    marker = CreateMarker(pointView.transform, markerPrefab);
                    changed = true;
                }

                if (EnsureMarkerModel(marker, markerPrefab))
                {
                    changed = true;
                }

                RemoveGeneratedPortraitChildren(marker);
                var prompt = marker.Find(EventPromptName);
                if (prompt == null)
                {
                    var promptObject = new GameObject(EventPromptName);
                    prompt = promptObject.transform;
                    Undo.RegisterCreatedObjectUndo(promptObject, "Add city point event prompt");
                    prompt.SetParent(marker, false);
                    prompt.localPosition = new Vector3(0f, 1.4f, 0f);
                    prompt.localRotation = Quaternion.identity;
                    prompt.localScale = new Vector3(0.1f, 0.1f, 1f);
                    changed = true;
                }

                var renderer = prompt.GetComponent<SpriteRenderer>();
                if (renderer == null)
                {
                    renderer = prompt.gameObject.AddComponent<SpriteRenderer>();
                    changed = true;
                }

                if (promptSprite != null && renderer.sprite != promptSprite)
                {
                    renderer.sprite = promptSprite;
                    changed = true;
                }

                renderer.sortingOrder = 50;
                if (prompt.gameObject.activeSelf)
                {
                    prompt.gameObject.SetActive(false);
                    changed = true;
                }

                var serializedPoint = new SerializedObject(pointView);
                var eventPromptProperty = serializedPoint.FindProperty("eventPrompt");
                if (eventPromptProperty != null && eventPromptProperty.objectReferenceValue != prompt)
                {
                    eventPromptProperty.objectReferenceValue = prompt;
                    serializedPoint.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("City point scene markers are present in BaseScene. Existing scene layout outside marker children was not rebuilt.");
        }

        private static Transform CreateMarker(Transform parent, GameObject markerPrefab)
        {
            GameObject markerObject;
            if (markerPrefab != null)
            {
                markerObject = (GameObject)PrefabUtility.InstantiatePrefab(markerPrefab, parent);
            }
            else
            {
                markerObject = new GameObject(BuildingActorPointName);
                markerObject.transform.SetParent(parent, false);
            }

            Undo.RegisterCreatedObjectUndo(markerObject, "Add city point marker");
            markerObject.name = BuildingActorPointName;
            markerObject.transform.localPosition = Vector3.zero;
            markerObject.transform.localRotation = Quaternion.identity;
            markerObject.transform.localScale = Vector3.one;
            markerObject.SetActive(true);
            return markerObject.transform;
        }

        private static bool EnsureMarkerModel(Transform marker, GameObject markerPrefab)
        {
            if (marker == null || marker.Find(LegacyManName) != null || markerPrefab == null)
            {
                return false;
            }

            var modelSource = markerPrefab.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate != null && candidate.name == LegacyManName);
            if (modelSource == null)
            {
                return false;
            }

            var modelObject = Object.Instantiate(modelSource.gameObject, marker);
            Undo.RegisterCreatedObjectUndo(modelObject, "Add city point marker model");
            modelObject.name = LegacyManName;
            modelObject.transform.localPosition = modelSource.localPosition;
            modelObject.transform.localRotation = modelSource.localRotation;
            modelObject.transform.localScale = modelSource.localScale;
            modelObject.SetActive(true);
            return true;
        }

        private static Sprite LoadPromptSprite(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            var prompt = prefab.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate != null && candidate.name == EventPromptName);
            return prompt != null && prompt.TryGetComponent<SpriteRenderer>(out var renderer)
                ? renderer.sprite
                : null;
        }

        private static void RemoveGeneratedPortraitChildren(Transform marker)
        {
            var children = marker.Cast<Transform>().ToArray();
            foreach (var child in children)
            {
                if (child == null || child.name != PointPortraitName)
                {
                    continue;
                }

                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static bool IsPointId(string value)
        {
            return value.Length == 5 && value[0] == 'P' && value.Skip(1).All(char.IsDigit);
        }
    }
}
