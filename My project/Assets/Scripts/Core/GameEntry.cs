using UnityEngine;

namespace TwelveMoons.Core
{
    public sealed class GameEntry : MonoBehaviour
    {
        [Header("Scene Roots")]
        [SerializeField] private GameObject deskRoot;
        [SerializeField] private GameObject cityRoot;

        [Header("Startup")]
        [SerializeField] private bool showDeskOnStart = true;

        public GameObject DeskRoot => deskRoot;
        public GameObject CityRoot => cityRoot;

        private void Awake()
        {
            ValidateReferences();
        }

        private void Start()
        {
            if (showDeskOnStart)
            {
                ShowDesk();
            }
        }

        public void ShowDesk()
        {
            SetRootActive(deskRoot, true);
            SetRootActive(cityRoot, false);
        }

        public void ShowCity()
        {
            SetRootActive(deskRoot, false);
            SetRootActive(cityRoot, true);
        }

        public void ValidateReferences()
        {
            if (deskRoot == null)
            {
                Debug.LogWarning($"{nameof(GameEntry)} missing DeskRoot reference.", this);
            }

            if (cityRoot == null)
            {
                Debug.LogWarning($"{nameof(GameEntry)} missing CityRoot reference.", this);
            }
        }

        private static void SetRootActive(GameObject root, bool isActive)
        {
            if (root != null && root.activeSelf != isActive)
            {
                root.SetActive(isActive);
            }
        }
    }
}
