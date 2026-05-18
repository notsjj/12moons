using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class RoundDebugControls : MonoBehaviour
    {
        [SerializeField] private RoundService roundService;

        private void Awake()
        {
            if (roundService == null)
            {
                roundService = FindFirstObjectByType<RoundService>();
            }
        }

        public void NextRound()
        {
            if (roundService != null)
            {
                roundService.NextRound();
            }
        }

        public void Restart()
        {
            if (roundService != null)
            {
                roundService.RestartInitialDisaster();
            }
        }
    }
}
