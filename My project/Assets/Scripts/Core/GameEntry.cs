using TwelveMoons.City;
using UnityEngine;

namespace TwelveMoons.Core
{
    public sealed class GameEntry : MonoBehaviour
    {
        [Header("场景根节点：桌面与城区显示切换")]
        [Tooltip("桌面界面根物体；ShowDesk 时显示，用于承载当前已实现的桌面流程。")]
        [SerializeField] private GameObject deskRoot;
        [Tooltip("城区界面根物体；ShowCity 时显示，用于切换到城区观察界面。")]
        [SerializeField] private GameObject cityRoot;

        [Header("启动设置：进入场景时默认显示")]
        [Tooltip("勾选后 Start 时默认显示桌面并隐藏城区，方便从桌面流程开始验证。")]
        [SerializeField] private bool showDeskOnStart = true;

        public GameObject DeskRoot => deskRoot;
        public GameObject CityRoot => cityRoot;

        private void Awake()
        {
            ResolveMissingReferences();
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
            CityPointView.SetAllPortraitsVisible(false);
            SetRootActive(deskRoot, true);
            SetRootActive(cityRoot, false);
        }

        public void ShowCity()
        {
            SetRootActive(deskRoot, false);
            SetRootActive(cityRoot, true);
            EnsureCityBuildingBinding();
            CityPointView.SetAllPortraitsVisible(true);
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

        private void ResolveMissingReferences()
        {
            if (deskRoot == null)
            {
                deskRoot = FindRootByName("DeskRoot");
            }

            if (cityRoot == null)
            {
                cityRoot = FindRootByName("CityRoot");
            }
        }

        private static GameObject FindRootByName(string objectName)
        {
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in transforms)
            {
                if (candidate != null && candidate.name == objectName)
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private static void SetRootActive(GameObject root, bool isActive)
        {
            if (root != null && root.activeSelf != isActive)
            {
                root.SetActive(isActive);
            }
        }

        private void EnsureCityBuildingBinding()
        {
            if (cityRoot == null)
            {
                return;
            }

            var service = cityRoot.GetComponentInChildren<CityBuildingService>(true);
            if (service == null)
            {
                service = cityRoot.AddComponent<CityBuildingService>();
            }

            var registry = cityRoot.GetComponentInChildren<CityBuildingRegistry>(true);
            if (registry == null)
            {
                registry = cityRoot.AddComponent<CityBuildingRegistry>();
            }

            service.Refresh();
            registry.RefreshAndBind();
        }
    }
}
