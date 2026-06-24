using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class SettlementPanelView : MonoBehaviour
    {
        private const string EmptyBuildingOutputText = "本回合没有可领取的建筑产出";
        private const string EmptyDocumentRewardText = "本回合公文无直接奖励";

        [Header("结算面板：上一回合收益文本")]
        [Tooltip("显示上一回合自动结算的城区建筑收入；运行时会自动查找名为“建筑产出”的 TMP 文本。")]
        [SerializeField] private TMP_Text buildingOutputText;

        [Tooltip("显示上一回合公文选项带来的直接奖励；运行时会自动查找名为“公文奖励”的 TMP 文本。")]
        [SerializeField] private TMP_Text documentRewardText;

        [Header("结算面板：返回按钮")]
        [Tooltip("点击后关闭结算面板；运行时会自动查找名为“返回按钮”的 Button。")]
        [SerializeField] private Button returnButton;

        [Header("只读快照：结算面板内容")]
        [Tooltip("运行时只读；显示最近一次写入建筑产出栏的文本，方便在 Inspector 中检查结算结果。")]
        [SerializeField] private string inspectorLastBuildingOutputText;

        [Tooltip("运行时只读；显示最近一次写入公文奖励栏的文本，方便在 Inspector 中检查结算结果。")]
        [SerializeField] private string inspectorLastDocumentRewardText;

        private void Awake()
        {
            ResolveReferences();
            BindReturnButton();
        }

        private void OnEnable()
        {
            ResolveReferences();
            BindReturnButton();
        }

        private void OnDisable()
        {
            if (returnButton != null)
            {
                returnButton.onClick.RemoveListener(Hide);
            }
        }

        public void Show(string buildingOutput, string documentReward)
        {
            ResolveReferences();
            BindReturnButton();

            inspectorLastBuildingOutputText = string.IsNullOrWhiteSpace(buildingOutput)
                ? EmptyBuildingOutputText
                : buildingOutput.Trim();
            inspectorLastDocumentRewardText = string.IsNullOrWhiteSpace(documentReward)
                ? EmptyDocumentRewardText
                : documentReward.Trim();

            if (buildingOutputText != null)
            {
                buildingOutputText.text = inspectorLastBuildingOutputText;
            }

            if (documentRewardText != null)
            {
                documentRewardText.text = inspectorLastDocumentRewardText;
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ResolveReferences()
        {
            if (buildingOutputText == null)
            {
                buildingOutputText = FindTextByName("建筑产出");
            }

            if (documentRewardText == null)
            {
                documentRewardText = FindTextByName("公文奖励");
            }

            if (returnButton == null)
            {
                returnButton = GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(button => button != null && button.name == "返回按钮") ??
                    GetComponentsInChildren<Button>(true)
                        .FirstOrDefault(button => button != null && button.name.Contains("返回"));
            }
        }

        private TMP_Text FindTextByName(string objectName)
        {
            return GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(text => text != null && text.name == objectName);
        }

        private void BindReturnButton()
        {
            if (returnButton == null)
            {
                return;
            }

            returnButton.onClick.RemoveListener(Hide);
            returnButton.onClick.AddListener(Hide);
        }
    }
}
