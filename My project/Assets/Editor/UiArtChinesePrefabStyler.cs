using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools
{
    public static class UiArtChinesePrefabStyler
    {
        private const string UiArtRoot = "Assets/Resources/Art/Art/UI";

        private static readonly string[] TargetPrefabPaths =
        {
            "Assets/Resources/Prefabs/UI/DeskPanel.prefab",
            "Assets/Resources/Prefabs/UI/CityHudPanel.prefab",
            "Assets/Resources/Prefabs/UI/StoryPanel.prefab",
            "Assets/Resources/Prefabs/UI/DocumentPopupPanel.prefab",
            "Assets/Resources/Prefabs/UI/NewspaperPanel.prefab",
            "Assets/Resources/Prefabs/UI/LetterReaderPanel.prefab"
        };

        [MenuItem("Twelve Moons/UI/应用中文素材并更新UI Prefab")]
        public static void ApplyChineseUiArtAndRenamePrefabs()
        {
            EnsureUiArtImportedAsSprite();

            foreach (var prefabPath in TargetPrefabPaths)
            {
                if (!File.Exists(prefabPath))
                {
                    Debug.LogWarning($"跳过不存在的 Prefab：{prefabPath}");
                    continue;
                }

                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    ApplyVisualStyle(prefabPath, root);
                    RenameHierarchyToChinese(root.transform);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("已完成中文素材绑定与 UI 组件中文命名。");
        }

        private static void ApplyVisualStyle(string prefabPath, GameObject root)
        {
            if (prefabPath.EndsWith("DeskPanel.prefab", StringComparison.OrdinalIgnoreCase))
            {
                StyleDeskPanel(root);
                return;
            }

            if (prefabPath.EndsWith("CityHudPanel.prefab", StringComparison.OrdinalIgnoreCase))
            {
                StyleCityHudPanel(root);
                return;
            }

            if (prefabPath.EndsWith("StoryPanel.prefab", StringComparison.OrdinalIgnoreCase))
            {
                StyleStoryPanel(root);
                return;
            }

            if (prefabPath.EndsWith("DocumentPopupPanel.prefab", StringComparison.OrdinalIgnoreCase))
            {
                StyleDocumentPopup(root);
                return;
            }

            if (prefabPath.EndsWith("NewspaperPanel.prefab", StringComparison.OrdinalIgnoreCase))
            {
                StyleNewspaperPanel(root);
                return;
            }

            if (prefabPath.EndsWith("LetterReaderPanel.prefab", StringComparison.OrdinalIgnoreCase))
            {
                StyleLetterReaderPanel(root);
            }
        }

        private static void StyleDeskPanel(GameObject root)
        {
            SetFirstSprite(root.transform, PathInUi("Story/边框/左侧高框.png"), true, "SharedActorSlot");
            SetFirstSprite(root.transform, PathInUi("City/装饰/指针装饰.png"), true, "SuspicionPointerIcon");
            SetFirstSprite(root.transform, PathInUi("Document/纸张/纸张面板.png"), true, "ContentBackgroundImage");
            SetFirstSprite(root.transform, PathInUi("Document/装饰/横向页签栏.png"), true, "CardSlotImage");
            SetFirstSprite(root.transform, PathInUi("Document/装饰/竖向卷轴栏.png"), true, "SubmitCardSlot");
            SetFirstSprite(root.transform, PathInUi("Document/装饰/圆环装饰.png"), true, "LeftScrollEndImage");
            SetFirstSprite(root.transform, PathInUi("Document/装饰/圆环装饰.png"), true, "RightScrollEndImage");
            SetFirstSprite(root.transform, PathInUi("Letter/书信纸片.png"), true, "LetterReaderPanel");
            SetFirstSprite(root.transform, PathInUi("Document/纸张/纸张面板.png"), true, "NewspaperPanel");

            foreach (var buttonName in new[]
                     {
                         "StoryButton",
                         "DocumentButton",
                         "NewspaperButton",
                         "CityButton",
                         "EndRoundButton",
                         "OptionAButton",
                         "OptionBButton",
                         "CloseButton"
                     })
            {
                SetAllSprites(root.transform, PathInUi("Desk/按钮/黑底边框板.png"), true, buttonName);
            }

            SetFactionRowIcon(root.transform, "civilianSuspicionRow", PathInUi("Desk/徽章/新月徽章.png"));
            SetFactionRowIcon(root.transform, "academySuspicionRow", PathInUi("Desk/徽章/新月徽章.png"));
            SetFactionRowIcon(root.transform, "churchSuspicionRow", PathInUi("Desk/徽章/教会徽章.png"));
            SetFactionRowIcon(root.transform, "nobleSuspicionRow", PathInUi("Desk/徽章/王室徽章.png"));
        }

        private static void StyleCityHudPanel(GameObject root)
        {
            SetFirstSprite(root.transform, PathInUi("City/装饰/城区面板效果图.png"), true, "Background");

            foreach (var buttonName in new[]
                     {
                         "GlobalButton",
                         "RoyalButton",
                         "ChurchButton",
                         "AcademyButton",
                         "UpperCityButton",
                         "LowerCityButton"
                     })
            {
                SetFirstSprite(root.transform, PathInUi("Document/装饰/标题边框长.png"), true, buttonName);
            }

            SetFactionRowIcon(root.transform, "civilianCitySuspicionRow", PathInUi("Desk/徽章/新月徽章.png"));
            SetFactionRowIcon(root.transform, "academyCitySuspicionRow", PathInUi("Desk/徽章/新月徽章.png"));
            SetFactionRowIcon(root.transform, "churchCitySuspicionRow", PathInUi("Desk/徽章/教会徽章.png"));
            SetFactionRowIcon(root.transform, "nobleCitySuspicionRow", PathInUi("Desk/徽章/王室徽章.png"));
        }

        private static void StyleStoryPanel(GameObject root)
        {
            SetFirstSprite(root.transform, PathInUi("Story/边框/剧情竖向边框.png"), true, "DialoguePanel");
            SetFirstSprite(root.transform, PathInUi("Desk/效果图/桌面界面效果图.png"), true, "ImageStoryPanel");
            SetFirstSprite(root.transform, PathInUi("Story/边框/剧情立柱书页框.png"), true, "TextStoryPanel");
            SetFirstSprite(root.transform, PathInUi("Document/纸张/纸张面板.png"), true, "SubmissionPanel");
            SetFirstSprite(root.transform, PathInUi("Desk/按钮/底部横条.png"), true, "DialogueBar");
        }

        private static void StyleDocumentPopup(GameObject root)
        {
            SetFirstSprite(root.transform, PathInUi("Document/纸张/纸张面板.png"), true, "ContentBackgroundImage");
            SetFirstSprite(root.transform, PathInUi("Document/装饰/圆环装饰.png"), true, "LeftScrollEndImage");
            SetFirstSprite(root.transform, PathInUi("Document/装饰/圆环装饰.png"), true, "RightScrollEndImage");
            SetFirstSprite(root.transform, PathInUi("Document/装饰/横向页签栏.png"), true, "CardSlotImage");
            SetFirstSprite(root.transform, PathInUi("Document/装饰/竖向卷轴栏.png"), true, "SubmitCardSlot");
            SetFirstSprite(root.transform, PathInUi("Document/装饰/横向短条一.png"), true, "OptionAButton");
            SetFirstSprite(root.transform, PathInUi("Document/装饰/横向短条二.png"), true, "OptionBButton");
            SetFirstSprite(root.transform, PathInUi("Letter/便笺图标.png"), true, "StampImage");
        }

        private static void StyleNewspaperPanel(GameObject root)
        {
            SetFirstSprite(root.transform, PathInUi("Document/纸张/纸张面板.png"), true, "NewspaperPanel");
            SetFirstSprite(root.transform, PathInUi("Document/装饰/标题边框长.png"), true, "CloseButton");
        }

        private static void StyleLetterReaderPanel(GameObject root)
        {
            SetFirstSprite(root.transform, PathInUi("Letter/书信纸片.png"), true, "LetterReaderPanel");
            SetFirstSprite(root.transform, PathInUi("Document/装饰/标题边框长.png"), true, "CloseButton");
        }

        private static void SetFactionRowIcon(Transform root, string rowName, string assetPath)
        {
            var row = FindDeepChild(root, BuildNameAliases(rowName));
            if (row == null)
            {
                return;
            }

            SetFirstSprite(row, assetPath, true, "IconImage");
            SetFirstSprite(row, assetPath, true, "FactionIcon");
        }

        private static void SetFirstSprite(Transform root, string assetPath, bool preserveAspect, params string[] targetNames)
        {
            var target = FindDeepChild(root, ExpandAliases(targetNames));
            if (target == null)
            {
                return;
            }

            SetSpriteOnTarget(target, assetPath, preserveAspect);
        }

        private static void SetAllSprites(Transform root, string assetPath, bool preserveAspect, params string[] targetNames)
        {
            foreach (var target in FindAllDeepChildren(root, ExpandAliases(targetNames)))
            {
                SetSpriteOnTarget(target, assetPath, preserveAspect);
            }
        }

        private static void SetSpriteOnTarget(Transform target, string assetPath, bool preserveAspect)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Debug.LogWarning($"未找到素材：{assetPath}");
                return;
            }

            var image = target.GetComponent<Image>() ?? target.GetComponentInChildren<Image>(true);
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            EditorUtility.SetDirty(image);
        }

        private static Transform FindDeepChild(Transform root, IEnumerable<string> targetNames)
        {
            foreach (var child in FindAllDeepChildren(root, targetNames))
            {
                return child;
            }

            return null;
        }

        private static IEnumerable<Transform> FindAllDeepChildren(Transform root, IEnumerable<string> targetNames)
        {
            if (root == null)
            {
                yield break;
            }

            var targetNameSet = new HashSet<string>(targetNames, StringComparer.Ordinal);
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && targetNameSet.Contains(child.name))
                {
                    yield return child;
                }
            }
        }

        private static IEnumerable<string> ExpandAliases(IEnumerable<string> targetNames)
        {
            var aliases = new HashSet<string>(StringComparer.Ordinal);
            foreach (var targetName in targetNames)
            {
                foreach (var alias in BuildNameAliases(targetName))
                {
                    aliases.Add(alias);
                }
            }

            return aliases;
        }

        private static IEnumerable<string> BuildNameAliases(string originalName)
        {
            if (string.IsNullOrWhiteSpace(originalName))
            {
                yield break;
            }

            yield return originalName;

            var chineseName = ToChineseName(originalName);
            if (!string.Equals(chineseName, originalName, StringComparison.Ordinal))
            {
                yield return chineseName;
            }
        }

        private static void EnsureUiArtImportedAsSprite()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { UiArtRoot });
            var changed = false;

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }

                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                if (importer.textureType == TextureImporterType.Sprite &&
                    importer.spriteImportMode == SpriteImportMode.Single &&
                    !importer.mipmapEnabled &&
                    importer.alphaIsTransparency)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.isReadable = false;
                importer.SaveAndReimport();
                changed = true;
            }

            if (changed)
            {
                AssetDatabase.Refresh();
                Debug.Log("已将 UI 素材统一重导入为 Sprite。");
            }
        }

        private static string PathInUi(string relativePath)
        {
            return $"{UiArtRoot}/{relativePath}";
        }

        private static void RenameHierarchyToChinese(Transform root)
        {
            if (root == null)
            {
                return;
            }

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child == null || string.IsNullOrWhiteSpace(child.name))
                {
                    continue;
                }

                child.name = ToChineseName(child.name);
            }
        }

        private static string ToChineseName(string originalName)
        {
            if (ExactNameMap.TryGetValue(originalName, out var exactName))
            {
                return exactName;
            }

            var translated = originalName;
            foreach (var pair in TokenMap)
            {
                translated = translated.Replace(pair.Key, pair.Value);
            }

            translated = translated.Replace("__", "_").Trim('_');
            return ContainsAsciiLetter(translated) ? $"界面节点_{translated}" : translated;
        }

        private static bool ContainsAsciiLetter(string value)
        {
            foreach (var character in value)
            {
                if ((character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z'))
                {
                    return true;
                }
            }

            return false;
        }

        private static readonly Dictionary<string, string> ExactNameMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "DeskPanel", "桌面面板" },
            { "CityHudPanel", "城区界面面板" },
            { "StoryPanel", "剧情面板" },
            { "DocumentPopupPanel", "公文弹窗面板" },
            { "NewspaperPanel", "报纸面板" },
            { "LetterReaderPanel", "信件阅读面板" },
            { "SharedActorSlot", "共用角色槽位" },
            { "ActorRoot", "角色根节点" },
            { "ContentRoot", "内容根节点" },
            { "ContentViewport", "内容视口" },
            { "DialoguePanel", "对话面板" },
            { "ImageStoryPanel", "图片剧情面板" },
            { "TextStoryPanel", "文本剧情面板" },
            { "SubmissionPanel", "提交面板" },
            { "UpperCityButton", "上城区按钮" },
            { "LowerCityButton", "下城区按钮" }
        };

        private static readonly KeyValuePair<string, string>[] TokenMap =
        {
            new KeyValuePair<string, string>("Button", "按钮"),
            new KeyValuePair<string, string>("Panel", "面板"),
            new KeyValuePair<string, string>("Text", "文本"),
            new KeyValuePair<string, string>("Image", "图片"),
            new KeyValuePair<string, string>("Background", "背景"),
            new KeyValuePair<string, string>("Dialogue", "对话"),
            new KeyValuePair<string, string>("Dialog", "对话"),
            new KeyValuePair<string, string>("Story", "剧情"),
            new KeyValuePair<string, string>("Letter", "信件"),
            new KeyValuePair<string, string>("Document", "公文"),
            new KeyValuePair<string, string>("City", "城区"),
            new KeyValuePair<string, string>("Round", "回合"),
            new KeyValuePair<string, string>("Title", "标题"),
            new KeyValuePair<string, string>("Body", "正文"),
            new KeyValuePair<string, string>("Sender", "寄件人"),
            new KeyValuePair<string, string>("Close", "关闭"),
            new KeyValuePair<string, string>("OptionA", "选项甲"),
            new KeyValuePair<string, string>("OptionB", "选项乙"),
            new KeyValuePair<string, string>("Left", "左"),
            new KeyValuePair<string, string>("Right", "右"),
            new KeyValuePair<string, string>("Scroll", "卷轴"),
            new KeyValuePair<string, string>("Row", "行"),
            new KeyValuePair<string, string>("Actor", "角色"),
            new KeyValuePair<string, string>("Suspicion", "质疑"),
            new KeyValuePair<string, string>("Inventory", "物品"),
            new KeyValuePair<string, string>("Task", "任务"),
            new KeyValuePair<string, string>("Newspaper", "报纸"),
            new KeyValuePair<string, string>("Reader", "阅读"),
            new KeyValuePair<string, string>("Slot", "槽位"),
            new KeyValuePair<string, string>("Area", "区域"),
            new KeyValuePair<string, string>("Viewport", "视口"),
            new KeyValuePair<string, string>("Icon", "图标"),
            new KeyValuePair<string, string>("Name", "名称"),
            new KeyValuePair<string, string>("Feedback", "反馈"),
            new KeyValuePair<string, string>("Pointer", "指针"),
            new KeyValuePair<string, string>("Fill", "填充"),
            new KeyValuePair<string, string>("Empty", "空"),
            new KeyValuePair<string, string>("Status", "状态"),
            new KeyValuePair<string, string>("Header", "头部"),
            new KeyValuePair<string, string>("Value", "数值"),
            new KeyValuePair<string, string>("Speaker", "说话者"),
            new KeyValuePair<string, string>("Choice", "选项"),
            new KeyValuePair<string, string>("Continue", "继续"),
            new KeyValuePair<string, string>("Submit", "提交"),
            new KeyValuePair<string, string>("Start", "开始"),
            new KeyValuePair<string, string>("Exit", "退出"),
            new KeyValuePair<string, string>("Debug", "调试"),
            new KeyValuePair<string, string>("View", "视图"),
            new KeyValuePair<string, string>("Root", "根节点"),
            new KeyValuePair<string, string>("Main", "主"),
            new KeyValuePair<string, string>("Shared", "共用"),
            new KeyValuePair<string, string>("Camera", "镜头"),
            new KeyValuePair<string, string>("Overlay", "覆盖"),
            new KeyValuePair<string, string>("Grid", "网格"),
            new KeyValuePair<string, string>("Comic", "分镜"),
            new KeyValuePair<string, string>("Portrait", "立绘"),
            new KeyValuePair<string, string>("Expression", "表情"),
            new KeyValuePair<string, string>("Requirement", "需求"),
            new KeyValuePair<string, string>("Stamp", "印章"),
            new KeyValuePair<string, string>("Card", "卡牌"),
            new KeyValuePair<string, string>("Drop", "放入"),
            new KeyValuePair<string, string>("Proposer", "提出者"),
            new KeyValuePair<string, string>("Faction", "阵营"),
            new KeyValuePair<string, string>("Royal", "王室"),
            new KeyValuePair<string, string>("Church", "教会"),
            new KeyValuePair<string, string>("Upper", "上城"),
            new KeyValuePair<string, string>("Academy", "学院"),
            new KeyValuePair<string, string>("Lower", "下城"),
            new KeyValuePair<string, string>("Global", "全局"),
            new KeyValuePair<string, string>("Label", "标签"),
            new KeyValuePair<string, string>("List", "列表"),
            new KeyValuePair<string, string>("Hud", "界面"),
            new KeyValuePair<string, string>("Popup", "弹窗")
        };
    }
}
