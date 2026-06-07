using UnityEngine;

public sealed class UIHandle
{
    public UIHandle(UIType uiType, GameObject gameObject)
    {
        UIType = uiType;
        GameObject = gameObject;
    }

    public UIType UIType { get; }

    public GameObject GameObject { get; }

    public T GetComponent<T>() where T : Component
    {
        return GameObject == null ? null : GameObject.GetComponent<T>();
    }
}