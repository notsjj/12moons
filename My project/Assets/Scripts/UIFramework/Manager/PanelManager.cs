using System.Collections.Generic;
using UnityEngine;

public class PanelManager : Singleton<PanelManager>
{
    private Stack<BasePanel> stackPanel = new Stack<BasePanel>();
    private BasePanel panel;

    protected override void Awake()
    {
        base.Awake();
        stackPanel ??= new Stack<BasePanel>();
    }

    /// <summary>
    /// UI 的入栈操作，此操作会显示一个面板，暂停当前面板。
    /// </summary>
    public void Push(BasePanel nextPanel)
    {
        if (nextPanel == null)
        {
            Debug.LogError("要打开的 UI 面板为空。");
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogError("缺少 UIManager，无法打开面板。");
            return;
        }

        if (stackPanel.Count > 0)
        {
            panel = stackPanel.Peek();
            panel.OnPause();
        }

        var panelGo = UIManager.Instance.GetSingleUI(nextPanel.UIType);
        if (panelGo == null)
        {
            Debug.LogError($"无法创建面板：{nextPanel.UIType.Name}");
            if (stackPanel.Count > 0)
            {
                stackPanel.Peek().OnResume();
            }

            return;
        }

        stackPanel.Push(nextPanel);
        nextPanel.Initialize(new UITool(panelGo));
        nextPanel.Initialize(this, UIManager.Instance);
        nextPanel.OnEnter();
    }

    /// <summary>
    /// 执行面板的出栈操作，此操作会关闭当前面板，恢复上一个面板。
    /// </summary>
    public void Pop()
    {
        if (stackPanel.Count == 0)
        {
            Debug.LogError("栈中没有 UI 面板。");
            return;
        }

        stackPanel.Pop().OnExit();

        if (stackPanel.Count > 0)
        {
            stackPanel.Peek().OnResume();
        }
    }

    /// <summary>
    /// 执行所有面板的出栈操作，此操作会关闭所有面板。
    /// </summary>
    public void PopAll()
    {
        while (stackPanel.Count > 0)
        {
            stackPanel.Pop().OnExit();
        }
    }

    /// <summary>
    /// 检测栈中是否存在指定类型的面板。
    /// </summary>
    public bool IsPanelInStack<T>() where T : BasePanel
    {
        foreach (var stackItem in stackPanel)
        {
            if (stackItem is T)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检测栈是否为空。
    /// </summary>
    public bool IsStackEmpty()
    {
        return stackPanel.Count == 0;
    }
}