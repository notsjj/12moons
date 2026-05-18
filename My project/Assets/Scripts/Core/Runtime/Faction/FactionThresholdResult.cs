namespace TwelveMoons.Core.Runtime
{
    public sealed class FactionThresholdResult
    {
        public FactionThresholdResult(string factionId, string lowSuspicionLetterId, string punishTaskId)
        {
            FactionId = factionId;
            LowSuspicionLetterId = lowSuspicionLetterId;
            PunishTaskId = punishTaskId;
        }

        public string FactionId { get; }

        public string LowSuspicionLetterId { get; }

        public string PunishTaskId { get; }

        public bool GrantedLowSuspicionLetter => !string.IsNullOrEmpty(LowSuspicionLetterId);

        public bool ActivatedPunishTask => !string.IsNullOrEmpty(PunishTaskId);
    }
}
