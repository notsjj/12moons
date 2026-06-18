using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools
{
    public sealed class CityModelChineseNamePostprocessor : AssetPostprocessor
    {
        private const string CityModelAssetPath = "Assets/Resources/Art/Map_01.fbx";
        private const string CityModelRootName = "城区地图_01";

        private void OnPostprocessModel(GameObject root)
        {
            if (root == null || assetPath.Replace('\\', '/') != CityModelAssetPath)
            {
                return;
            }

            RenameRecursively(root.transform, 0);
        }

        private static void RenameRecursively(Transform target, int depth)
        {
            if (target == null)
            {
                return;
            }

            target.name = depth == 0 ? CityModelRootName : TranslateOriginalName(target.name);
            for (var childIndex = 0; childIndex < target.childCount; childIndex++)
            {
                RenameRecursively(target.GetChild(childIndex), depth + 1);
            }
        }

        private static string TranslateOriginalName(string originalName)
        {
            if (string.IsNullOrWhiteSpace(originalName))
            {
                return "未命名模型";
            }

            var translated = originalName
                .Replace("Poor", "贫民")
                .Replace("poor", "贫民")
                .Replace("Desert", "沙地")
                .Replace("desert", "沙地")
                .Replace("Elevator", "升降梯")
                .Replace("elevator", "升降梯")
                .Replace("Gongfang", "工坊")
                .Replace("gongfang", "工坊")
                .Replace("Ground", "地面")
                .Replace("ground", "地面")
                .Replace("Hospital", "医院")
                .Replace("hospital", "医院")
                .Replace("Liangcang", "粮仓")
                .Replace("liangcang", "粮仓")
                .Replace("Library", "图书馆")
                .Replace("library", "图书馆")
                .Replace("Palace", "宫殿")
                .Replace("palace", "宫殿")
                .Replace("Pipe", "管道")
                .Replace("pipe", "管道")
                .Replace("City", "城区")
                .Replace("city", "城区")
                .Replace("Map", "地图")
                .Replace("map", "地图")
                .Replace("Building", "建筑")
                .Replace("building", "建筑")
                .Replace("House", "房屋")
                .Replace("house", "房屋")
                .Replace("Church", "教会")
                .Replace("church", "教会")
                .Replace("Academy", "学院")
                .Replace("academy", "学院")
                .Replace("Royal", "王室")
                .Replace("royal", "王室")
                .Replace("Market", "市场")
                .Replace("market", "市场")
                .Replace("Harbor", "港口")
                .Replace("harbor", "港口")
                .Replace("Gate", "大门")
                .Replace("gate", "大门")
                .Replace("Wall", "城墙")
                .Replace("wall", "城墙")
                .Replace("Road", "道路")
                .Replace("road", "道路")
                .Replace("Street", "街道")
                .Replace("street", "街道")
                .Replace("Bridge", "桥")
                .Replace("bridge", "桥")
                .Replace("Default", "默认")
                .Replace("default", "默认")
                .Replace("Source", "源")
                .Replace("source", "源")
                .Replace("Object", "对象")
                .Replace("object", "对象")
                .Replace("Tower", "塔")
                .Replace("tower", "塔")
                .Replace("Tree", "树")
                .Replace("tree", "树")
                .Replace("Camera", "摄像机")
                .Replace("camera", "摄像机")
                .Replace("Light", "灯光")
                .Replace("light", "灯光")
                .Replace("Point", "点位")
                .Replace("point", "点位")
                .Replace("Lower", "低城区")
                .Replace("lower", "低城区")
                .Replace("Upper", "上城区")
                .Replace("upper", "上城区")
                .Replace("Global", "全局")
                .Replace("global", "全局")
                .Replace("Mesh", "网格")
                .Replace("mesh", "网格")
                .Replace("Cube", "立方体")
                .Replace("cube", "立方体")
                .Replace("Plane", "平面")
                .Replace("plane", "平面");

            return translated;
        }
    }
}
