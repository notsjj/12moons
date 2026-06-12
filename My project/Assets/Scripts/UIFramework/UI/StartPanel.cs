using UnityEngine;
using UnityEngine.UI;

public class StartPanel : BasePanel
{
    static readonly string path = "Prefabs/UI/Start Panel";

    public StartPanel() : base(new UIType(path)) { }

    public override void OnEnter()
    {
        base.OnEnter();

        UITool.GetOrAddComponentInChildren<Button>("Start Button").onClick.AddListener(() =>
        {

        });

        UITool.GetOrAddComponentInChildren<Button>("Setting Button").onClick.AddListener(() =>
        {
        });

        UITool.GetOrAddComponentInChildren<Button>("Exit Button").onClick.AddListener(() =>
        {
            //点击事件可以加在这里
            Debug.Log("退出游戏");
            Application.Quit();
        });
    }

    public override void OnExit()
    {
        base.OnExit();
    }
}
