using System;

namespace TwelveMoons.Core.Runtime
{
    [Serializable]
    public sealed class RuntimeFactionState
    {
        public RuntimeFactionState(string factionId, int suspicion)
        {
            FactionId = factionId;
            Suspicion = Math.Max(0, suspicion);
            LowSuspicionLetterGranted = false;
        }

        public string FactionId { get; private set; }

        public int Suspicion { get; private set; }

        public bool LowSuspicionLetterGranted { get; private set; }

        public void SetSuspicion(int value, int maxSuspicion)
        {
            Suspicion = Math.Max(0, Math.Min(value, Math.Max(0, maxSuspicion)));
        }

        public void AddSuspicion(int delta, int maxSuspicion)
        {
            SetSuspicion(Suspicion + delta, maxSuspicion);
        }

        public void MarkLowSuspicionLetterGranted()
        {
            LowSuspicionLetterGranted = true;
        }

    }
}
