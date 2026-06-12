using UnityEngine;

public class StartUIManager : MonoBehaviour
{
    PanelManager panelManager;

    private void Awake()
    {
        panelManager = new PanelManager();
    }

    private void Start()
    {
        panelManager.Push(new StartPanel());
    }
}
