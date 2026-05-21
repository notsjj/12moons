using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class NewspaperPanelView : MonoBehaviour
    {
        [Header("依赖服务：读取上一回合报纸数据")]
        [Tooltip("运行时数据服务；用于读取已生成的上一回合报纸内容。")]
        [SerializeField] private RuntimeDataService runtimeDataService;

        [Header("报纸文本：显示标题、正文和空状态提示")]
        [Tooltip("报纸标题文本；显示报纸所属回合。")]
        [SerializeField] private TMP_Text titleText;
        [Tooltip("报纸正文文本；显示上一回合公文、任务和剧情结算摘要。")]
        [SerializeField] private TMP_Text bodyText;
        [Tooltip("没有上一回合报纸时显示的状态文本。")]
        [SerializeField] private TMP_Text emptyText;

        private void Awake()
        {
            ResolveDependencies();
            Hide();
        }

        public void ShowPreviousRound()
        {
            ResolveDependencies();
            if (runtimeDataService == null)
            {
                ShowEmpty("未找到运行时数据服务。");
                return;
            }

            var previousRound = runtimeDataService.Data.CurrentRound - 1;
            if (previousRound <= 0 ||
                !runtimeDataService.Data.TryGetNewspaper(previousRound, out var newspaper))
            {
                ShowEmpty("暂无上一回合报纸。");
                return;
            }

            SetText(titleText, newspaper.Title);
            SetText(bodyText, newspaper.BuildBodyText());
            SetText(emptyText, string.Empty);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ShowEmpty(string message)
        {
            SetText(titleText, "报纸");
            SetText(bodyText, string.Empty);
            SetText(emptyText, message);
            gameObject.SetActive(true);
        }

        private void ResolveDependencies()
        {
            if (runtimeDataService == null)
            {
                runtimeDataService = FindFirstObjectByType<RuntimeDataService>();
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }
    }
}
