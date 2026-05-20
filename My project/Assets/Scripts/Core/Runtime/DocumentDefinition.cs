using TwelveMoons.Core.Config;

namespace TwelveMoons.Core.Runtime
{
    public sealed class DocumentDefinition
    {
        public DocumentDefinition(ConfigRow row)
        {
            DocumentId = row.GetString("DocumentId");
            Title = row.GetString("Title");
            BodyText = row.GetString("BodyText");
            ProposerCharacterId = row.GetString("ProposerCharacterId");
            DocumentType = row.GetString("DocumentType");
            DisasterId = row.GetString("DisasterId");
            DisasterStageId = row.GetString("DisasterStageId");
            TaskId = row.GetString("TaskId");
            TaskStageId = row.GetString("TaskStageId");
            IsRepeatable = row.GetBool("IsRepeatable");
            Remark = row.GetString("Remark");
            OptionA = new DocumentOptionDefinition(row, "OptionA");
            OptionB = new DocumentOptionDefinition(row, "OptionB");
        }

        public string DocumentId { get; }

        public string Title { get; }

        public string BodyText { get; }

        public string ProposerCharacterId { get; }

        public string DocumentType { get; }

        public string DisasterId { get; }

        public string DisasterStageId { get; }

        public string TaskId { get; }

        public string TaskStageId { get; }

        public bool IsRepeatable { get; }

        public string Remark { get; }

        public DocumentOptionDefinition OptionA { get; }

        public DocumentOptionDefinition OptionB { get; }

        public DocumentOptionDefinition GetOption(DocumentOptionType optionType)
        {
            return optionType == DocumentOptionType.A ? OptionA : OptionB;
        }
    }
}
