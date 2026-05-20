using TwelveMoons.Core.Config;

namespace TwelveMoons.Core.Runtime
{
    public sealed class DocumentOptionDefinition
    {
        public DocumentOptionDefinition(ConfigRow row, string prefix)
        {
            Text = row.GetString($"{prefix}_Text");
            MoneyChange = row.GetInt($"{prefix}_MoneyChange");
            MaterialChange = row.GetInt($"{prefix}_MaterialChange");
            FoodChange = row.GetInt($"{prefix}_FoodChange");
            NobleSuspicionChange = row.GetInt($"{prefix}_NobleSuspicionChange");
            AcademySuspicionChange = row.GetInt($"{prefix}_AcademySuspicionChange");
            ChurchSuspicionChange = row.GetInt($"{prefix}_ChurchSuspicionChange");
            CivilianSuspicionChange = row.GetInt($"{prefix}_CivilianSuspicionChange");
            TaskScoreChange = row.GetInt($"{prefix}_TaskScoreChange");
            RequiredItemId = row.GetString($"{prefix}_RequiredItemId");
            RequiredItemCount = row.GetInt($"{prefix}_RequiredItemCount");
            ConsumeItem = row.GetBool($"{prefix}_ConsumeItem");
            AddItemId = row.GetString($"{prefix}_AddItemId");
            AddItemCount = row.GetInt($"{prefix}_AddItemCount");
            NextDocumentId = row.GetString($"{prefix}_NextDocumentId");
            NextDocumentDelayRound = row.GetInt($"{prefix}_NextDocumentDelayRound");
            UnlockBuildingId = row.GetString($"{prefix}_UnlockBuildingId");
            ResultText = row.GetString($"{prefix}_ResultText");
            ProposerFeedbackText = row.GetString($"{prefix}_ProposerFeedbackText");
            FeedbackFactionId = row.GetString($"{prefix}_FeedbackFactionId");
            FactionFeedbackText = row.GetString($"{prefix}_FactionFeedbackText");
        }

        public string Text { get; }

        public int MoneyChange { get; }

        public int MaterialChange { get; }

        public int FoodChange { get; }

        public int NobleSuspicionChange { get; }

        public int AcademySuspicionChange { get; }

        public int ChurchSuspicionChange { get; }

        public int CivilianSuspicionChange { get; }

        public int TaskScoreChange { get; }

        public string RequiredItemId { get; }

        public int RequiredItemCount { get; }

        public bool ConsumeItem { get; }

        public string AddItemId { get; }

        public int AddItemCount { get; }

        public string NextDocumentId { get; }

        public int NextDocumentDelayRound { get; }

        public string UnlockBuildingId { get; }

        public string ResultText { get; }

        public string ProposerFeedbackText { get; }

        public string FeedbackFactionId { get; }

        public string FactionFeedbackText { get; }
    }
}
