using System;
using UnityEngine;

namespace TwelveMoons.Core.Runtime
{
    [CreateAssetMenu(fileName = "DocumentDefinitionAsset", menuName = "Twelve Moons/GameData/Document Definition")]
    public sealed class DocumentDefinitionAsset : ScriptableObject
    {
        [Serializable]
        public sealed class DocumentOptionSnapshot
        {
            [Header("选项文本与资源变化")]
            public string text;
            public int moneyChange;
            public int materialChange;
            public int foodChange;

            [Header("选项影响：阵营与任务")]
            public int nobleSuspicionChange;
            public int academySuspicionChange;
            public int churchSuspicionChange;
            public int civilianSuspicionChange;
            public int taskScoreChange;

            [Header("选项条件与产出")]
            public string requiredItemId;
            public int requiredItemCount;
            public bool consumeItem;
            public string addItemId;
            public int addItemCount;

            [Header("选项后续效果")]
            public string nextDocumentId;
            public int nextDocumentDelayRound;
            public string unlockBuildingId;
            [TextArea(2, 5)] public string resultText;
            [TextArea(2, 5)] public string proposerFeedbackText;
            public string feedbackFactionId;
            [TextArea(2, 5)] public string factionFeedbackText;

            public void Apply(DocumentOptionDefinition definition)
            {
                if (definition == null)
                {
                    return;
                }

                text = definition.Text;
                moneyChange = definition.MoneyChange;
                materialChange = definition.MaterialChange;
                foodChange = definition.FoodChange;
                nobleSuspicionChange = definition.NobleSuspicionChange;
                academySuspicionChange = definition.AcademySuspicionChange;
                churchSuspicionChange = definition.ChurchSuspicionChange;
                civilianSuspicionChange = definition.CivilianSuspicionChange;
                taskScoreChange = definition.TaskScoreChange;
                requiredItemId = definition.RequiredItemId;
                requiredItemCount = definition.RequiredItemCount;
                consumeItem = definition.ConsumeItem;
                addItemId = definition.AddItemId;
                addItemCount = definition.AddItemCount;
                nextDocumentId = definition.NextDocumentId;
                nextDocumentDelayRound = definition.NextDocumentDelayRound;
                unlockBuildingId = definition.UnlockBuildingId;
                resultText = definition.ResultText;
                proposerFeedbackText = definition.ProposerFeedbackText;
                feedbackFactionId = definition.FeedbackFactionId;
                factionFeedbackText = definition.FactionFeedbackText;
            }
        }

        [Header("公文基础信息")]
        [SerializeField] private string documentId;
        [SerializeField] private string title;
        [TextArea(3, 10)]
        [SerializeField] private string bodyText;
        [SerializeField] private string proposerCharacterId;
        [SerializeField] private string documentType;
        [Tooltip("公文显示的势力 logo 中文键；应与 Resources/Art/Art/UI/势力logo 下的图片文件名一致，例如“贵族”。")]
        [SerializeField] private string factionLogoName;
        [SerializeField] private string disasterId;
        [SerializeField] private string disasterStageId;
        [SerializeField] private string taskId;
        [SerializeField] private string taskStageId;
        [SerializeField] private bool isRepeatable;
        [SerializeField] private string remark;

        [Header("选项快照")]
        [SerializeField] private DocumentOptionSnapshot optionA = new DocumentOptionSnapshot();
        [SerializeField] private DocumentOptionSnapshot optionB = new DocumentOptionSnapshot();

        public string DocumentId => documentId;
        public string Title => title;
        public string BodyText => bodyText;
        public string ProposerCharacterId => proposerCharacterId;
        public string DocumentType => documentType;
        public string FactionLogoName => factionLogoName;
        public string DisasterId => disasterId;
        public string DisasterStageId => disasterStageId;
        public string TaskId => taskId;
        public string TaskStageId => taskStageId;
        public bool IsRepeatable => isRepeatable;
        public string Remark => remark;
        public DocumentOptionSnapshot OptionA => optionA;
        public DocumentOptionSnapshot OptionB => optionB;

        public void Apply(DocumentDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            documentId = definition.DocumentId;
            title = definition.Title;
            bodyText = definition.BodyText;
            proposerCharacterId = definition.ProposerCharacterId;
            documentType = definition.DocumentType;
            factionLogoName = definition.FactionLogoName;
            disasterId = definition.DisasterId;
            disasterStageId = definition.DisasterStageId;
            taskId = definition.TaskId;
            taskStageId = definition.TaskStageId;
            isRepeatable = definition.IsRepeatable;
            remark = definition.Remark;
            optionA.Apply(definition.OptionA);
            optionB.Apply(definition.OptionB);
        }
    }
}
