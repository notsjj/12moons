using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 按 UI 层级创建、复用、查询和销毁 Resources UI Prefab。
/// </summary>
public class UIManager : Singleton<UIManager>
{
    private readonly Dictionary<UIType, UIHandle> uiInstances = new Dictionary<UIType, UIHandle>();
    private readonly Dictionary<UILayer, Transform> layerRoots = new Dictionary<UILayer, Transform>();

    [Header("UI 根画布")]
    [Tooltip("UIFramework 实例化 UI 时使用的主画布；为空时自动查找名为 Main Canvas 的对象。")]
    [SerializeField] private Canvas mainCanvas;

    protected override void Awake()
    {
        base.Awake();
        EnsureLayerRoots();
    }

    public void EnsureLayerRoots()
    {
        var canvas = ResolveMainCanvas();
        if (canvas == null)
        {
            Debug.LogError("缺少 Main Canvas，无法初始化 UIFramework 层级。");
            return;
        }

        EnsureLayerRoot(canvas.transform, UILayer.Persistent, "PersistentRoot", 0);
        EnsureLayerRoot(canvas.transform, UILayer.Panel, "PanelRoot", 1);
        EnsureLayerRoot(canvas.transform, UILayer.Popup, "PopupRoot", 2);
        EnsureLayerRoot(canvas.transform, UILayer.Overlay, "OverlayRoot", 3);
    }

    public GameObject GetSingleUI(UIType type)
    {
        return ShowUI(type)?.GameObject;
    }

    public UIHandle ShowUI(UIType type)
    {
        if (type == null)
        {
            Debug.LogError("请求显示的 UIType 为空。");
            return null;
        }

        EnsureLayerRoots();

        if (uiInstances.TryGetValue(type, out var existing) && existing.GameObject != null)
        {
            existing.GameObject.SetActive(true);
            return existing;
        }

        if (!layerRoots.TryGetValue(type.Layer, out var parent) || parent == null)
        {
            Debug.LogError($"缺少 UI 层级根节点：{type.Layer}");
            return null;
        }

        var prefab = Resources.Load<GameObject>(type.Path);
        if (prefab == null)
        {
            Debug.LogError($"找不到 UI Prefab：UI={type.Name}，Resources 路径={type.Path}");
            return null;
        }

        var ui = Instantiate(prefab, parent, false);
        ui.name = type.Name;

        var handle = new UIHandle(type, ui);
        uiInstances[type] = handle;
        return handle;
    }

    public bool TryGetUI<T>(UIType type, out T component) where T : Component
    {
        component = null;
        if (type == null || !uiInstances.TryGetValue(type, out var handle) || handle.GameObject == null)
        {
            return false;
        }

        component = handle.GameObject.GetComponent<T>();
        return component != null;
    }

    public void HideUI(UIType type)
    {
        if (type != null && uiInstances.TryGetValue(type, out var handle) && handle.GameObject != null)
        {
            handle.GameObject.SetActive(false);
        }
    }

    public void DestroyUI(UIType type)
    {
        if (type == null || !uiInstances.TryGetValue(type, out var handle))
        {
            return;
        }

        if (handle.GameObject != null)
        {
            Destroy(handle.GameObject);
        }

        uiInstances.Remove(type);
    }

    public Transform GetLayerRoot(UILayer layer)
    {
        EnsureLayerRoots();
        layerRoots.TryGetValue(layer, out var root);
        return root;
    }

    private Canvas ResolveMainCanvas()
    {
        if (mainCanvas != null)
        {
            return mainCanvas;
        }

        var canvasObject = GameObject.Find("Main Canvas");
        mainCanvas = canvasObject == null ? null : canvasObject.GetComponent<Canvas>();
        return mainCanvas;
    }

    private void EnsureLayerRoot(Transform canvasTransform, UILayer layer, string rootName, int siblingIndex)
    {
        if (layerRoots.TryGetValue(layer, out var existingRoot) && existingRoot != null)
        {
            existingRoot.SetSiblingIndex(siblingIndex);
            return;
        }

        var child = canvasTransform.Find(rootName);
        if (child == null)
        {
            var root = new GameObject(rootName, typeof(RectTransform), typeof(CanvasGroup));
            child = root.transform;
            child.SetParent(canvasTransform, false);

            var rect = (RectTransform)child;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        child.SetSiblingIndex(siblingIndex);
        layerRoots[layer] = child;
    }
}
