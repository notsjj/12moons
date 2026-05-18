using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class FactionDebugControls : MonoBehaviour
    {
        [SerializeField] private FactionService factionService;
        [SerializeField] private string lowTestFactionId = "civilian";
        [SerializeField] private string highTestFactionId = "noble";
        [SerializeField] private int lowTestDelta = -35;
        [SerializeField] private int highTestDelta = 45;

        private void Awake()
        {
            if (factionService == null)
            {
                factionService = FindFirstObjectByType<FactionService>();
            }
        }

        public void LowerTestFactionSuspicion()
        {
            if (factionService != null)
            {
                factionService.ChangeSuspicion(lowTestFactionId, lowTestDelta);
            }
        }

        public void RaiseTestFactionSuspicion()
        {
            if (factionService != null)
            {
                factionService.ChangeSuspicion(highTestFactionId, highTestDelta);
            }
        }

        public void RefreshFactions()
        {
            if (factionService != null)
            {
                factionService.Refresh();
            }
        }
    }
}
