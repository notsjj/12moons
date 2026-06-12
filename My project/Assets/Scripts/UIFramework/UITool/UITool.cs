using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// UI管理工具类，提供一些常用的UI管理功能，如打开/关闭UI界面、获取UI组件等。
/// </summary>
public class UITool
{
    GameObject activePanel; //当前活动面板

    public UITool(GameObject panel)
    {
        activePanel = panel;
    }

    /// <summary>
    /// 根据名称查找一个子对象
    /// </summary>
    /// <param name="name">子对象名称</param>
    /// <returns></returns>
    public GameObject FindChildGameobject(string name)
    {
        Transform[] trans = activePanel.GetComponentsInChildren<Transform>();

        foreach(Transform item in trans)
        {
            if(item.name == name)
            {
                return item.gameObject;
            }
        }

        Debug.LogWarning($"{activePanel.name}里找不到名为{name}的子对象");
        return null;
    }

    /// <summary>
    /// 查找当前活动面板及其所有子物体中，拥有指定组件的物体，并返回列表（自动去重）
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <param name="includeInactive">是否包含非激活状态的物体，默认为 true</param>
    /// <returns>拥有指定组件的物体列表</returns>
    public List<T> FindGameObjectsWithComponent<T>(bool includeInactive = true) where T : Component
    {
        List<T> results = new List<T>();
        results = activePanel.GetComponentsInChildren<T>(includeInactive).ToList();
        foreach (T comp in results)
        {
            if (!results.Contains(comp)) // 避免同一个物体因多个相同组件而重复添加
                results.Add(comp);
        }
        return results;
    }

    /// <summary>
    /// 给当前的活动面板获取或者添加一个组件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetOrAddComponent<T>() where T : Component
    {
        if (activePanel.GetComponent<T>() == null)
            activePanel.AddComponent<T>();
        return activePanel.GetComponent<T>();
    }

    /// <summary>
    /// 根据名称获取一个子对象的组件
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <param name="name">子对象名字</param>
    /// <returns></returns>
    public T GetOrAddComponentInChildren<T>(string name) where T : Component
    {
        GameObject child = FindChildGameobject(name);
        if (child)
        {
            if (child.GetComponent<T>() == null)
                child.AddComponent<T>();

            return child.GetComponent<T>();
        }

        return null;
    }

}
