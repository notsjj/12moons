using System;

namespace TwelveMoons.Core.Runtime
{
    [Serializable]
    public sealed class RuntimeLetterState
    {
        public RuntimeLetterState(string letterId, int receivedRound)
        {
            LetterId = letterId;
            ReceivedRound = Math.Max(1, receivedRound);
            IsRead = false;
        }

        public string LetterId { get; private set; }

        public int ReceivedRound { get; private set; }

        public bool IsRead { get; private set; }

        public void MarkRead()
        {
            IsRead = true;
        }
    }
}
