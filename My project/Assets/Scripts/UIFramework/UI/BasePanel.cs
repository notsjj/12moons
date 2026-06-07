using UnityEngine;

/// <summary>
/// 所有 UI 面板的父类，包含 UI 面板的状态信息。
/// </summary>
public class BasePanel
{
    public UIType UIType { get; private set; }

    public UITool UITool { get; set; }

    public PanelManager PanelManager { get; set; }

    public UIManager UIManager { get; set; }

    public BasePanel(UIType uiType)
    {
        UIType = uiType;
    }

    public void Initialize(UITool tool)
    {
        UITool = tool;
    }

    public void Initialize(PanelManager panelManager, UIManager uiManager)
    {
        PanelManager = panelManager;
        UIManager = uiManager;
    }

    /// <summary>
    /// UI 进入时执行的操作，只会执行一次。
    /// </summary>
    public virtual void OnEnter()
    {
    }

    /// <summary>
    /// UI 暂停时执行的操作。
    /// </summary>
    public virtual void OnPause()
    {
        if (UITool == null)
        {
            return;
        }

        UITool.GetOrAddComponent<CanvasGroup>().blocksRaycasts = false;
    }

    /// <summary>
    /// UI 继续时执行的操作。
    /// </summary>
    public virtual void OnResume()
    {
        if (UITool == null)
        {
            return;
        }

        UITool.GetOrAddComponent<CanvasGroup>().blocksRaycasts = true;
    }

    /// <summary>
    /// UI 退出时执行的操作。
    /// </summary>
    public virtual void OnExit()
    {
        if (UITool != null)
        {
            var buttonAnims = UITool.FindGameObjectsWithComponent<ButtonAnim>(true);
            foreach (var buttonAnim in buttonAnims)
            {
                buttonAnim.OnExit();
            }
        }

        UIManager?.DestroyUI(UIType);
    }

    public void Push(BasePanel panel)
    {
        PanelManager?.Push(panel);
    }

    public void Pop()
    {
        PanelManager?.Pop();
    }

    public void PopAll()
    {
        PanelManager?.PopAll();
    }
}