using System.IO;
using System.Reflection;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class DeskLoopControllerPresenceSmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run Desk Loop Controller Presence Smoke Test")]
        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/桌面面板.prefab");
            if (prefab == null)
            {
                throw new FileNotFoundException("找不到桌面面板 Prefab，无法验证桌面流程控制器。");
            }

            var instance = Object.Instantiate(prefab);
            try
            {
                if (instance.GetComponent<DeskLoopController>() != null)
                {
                    Object.DestroyImmediate(instance.GetComponent<DeskLoopController>());
                }

                var bootstrapObject = new GameObject("BaseSceneUIBootstrap_DeskLoopControllerPresenceSmokeTest");
                try
                {
                    var bootstrap = bootstrapObject.AddComponent<BaseSceneUIBootstrap>();
                    var method = typeof(BaseSceneUIBootstrap).GetMethod(
                        "EnsureDeskLoopController",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    if (method == null)
                    {
                        throw new InvalidDataException("BaseSceneUIBootstrap 缺少 EnsureDeskLoopController 兜底方法。");
                    }

                    method.Invoke(bootstrap, new object[] { instance });
                    if (instance.GetComponent<DeskLoopController>() == null)
                    {
                        throw new InvalidDataException("桌面面板运行时没有补上 DeskLoopController；剧情面板会显示，但没有对象启动回合剧情。");
                    }
                }
                finally
                {
                    Object.DestroyImmediate(bootstrapObject);
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }

            Debug.Log("桌面流程控制器存在性冒烟测试通过：桌面面板运行时会自动保证 DeskLoopController 存在。");
        }
    }
}
