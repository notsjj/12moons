using System.IO;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;
namespace TwelveMoons.EditorTools.Runtime
{
    public static class UiLayerAndRoundTextSmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run UI Layer And Round Text Smoke Test")]
        public static void Run()
        {
            ValidateDocumentPopupComesInFrontOfOverlay();
            ValidateRoundPanelUsesDayLineBreak();
            Debug.Log("UI layer and round text smoke test passed. Document popup stays above overlay HUD, and the shared HUD round text uses Day line-break formatting.");
        }
        private static void ValidateDocumentPopupComesInFrontOfOverlay()
        {
            var canvasObject = new GameObject("Main Canvas", typeof(Canvas));
            var managerObject = new GameObject("UIManager_Test");
            var bootstrapObject = new GameObject("BaseSceneUIBootstrap_Test");
            try
            {
                var canvas = canvasObject.GetComponent<Canvas>();
                var uiManager = managerObject.AddComponent<UIManager>();
                var managerSerializedObject = new SerializedObject(uiManager);
                managerSerializedObject.FindProperty("mainCanvas").objectReferenceValue = canvas;
                managerSerializedObject.ApplyModifiedPropertiesWithoutUndo();
                var bootstrap = bootstrapObject.AddComponent<BaseSceneUIBootstrap>();
                var bootstrapSerializedObject = new SerializedObject(bootstrap);
                bootstrapSerializedObject.FindProperty("uiManager").objectReferenceValue = uiManager;
                bootstrapSerializedObject.ApplyModifiedPropertiesWithoutUndo();
                var popup = bootstrap.ShowDocumentPopup();
                if (popup == null)
                {
                    throw new InvalidDataException("BaseSceneUIBootstrap.ShowDocumentPopup ??????????????");
                }
                var popupRoot = uiManager.GetLayerRoot(UILayer.Popup);
                var overlayRoot = uiManager.GetLayerRoot(UILayer.Overlay);
                if (popupRoot == null || overlayRoot == null)
                {
                    throw new InvalidDataException("UIManager ???? PopupRoot ? OverlayRoot?");
                }
                if (popupRoot.GetSiblingIndex() <= overlayRoot.GetSiblingIndex())
                {
                    throw new InvalidDataException("????????PopupRoot ????? OverlayRoot ??????? HUD ??????");
                }
            }
            finally
            {
                Object.DestroyImmediate(bootstrapObject);
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(canvasObject);
            }
        }
        private static void ValidateRoundPanelUsesDayLineBreak()
        {
            const string sourcePath = "Assets/Scripts/UI/RoundPanelView.cs";
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("?? RoundPanelView.cs?", sourcePath);
            }
            var source = File.ReadAllText(sourcePath);
            if (!source.Contains("SetText(roundText, $\"Day\\n{roundService.CurrentRound}\");"))
            {
                throw new InvalidDataException("??HUD?????????????? Day ???????? Day\\n1?");
            }
        }
    }
}