using System;

/// <summary>
/// 存储单个 UI 的 Resources 路径、名称和显示层级。
/// </summary>
public sealed class UIType : IEquatable<UIType>
{
    public string Name { get; private set; }

    public string Path { get; private set; }

    public UILayer Layer { get; private set; }

    public UIType(string path, UILayer layer = UILayer.Panel)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("UI Resources 路径不能为空。", nameof(path));
        }

        var normalizedPath = path.Trim().Replace("\\", "/");
        if (normalizedPath.EndsWith("/", StringComparison.Ordinal))
        {
            throw new ArgumentException("UI Resources 路径不能以斜杠结尾。", nameof(path));
        }

        var nameStartIndex = normalizedPath.LastIndexOf('/') + 1;
        var uiName = normalizedPath.Substring(nameStartIndex);
        if (string.IsNullOrEmpty(uiName))
        {
            throw new ArgumentException("UI Resources 路径必须包含有效的 UI 名称。", nameof(path));
        }

        Path = normalizedPath;
        Layer = layer;
        Name = uiName;
    }

    public bool Equals(UIType other)
    {
        return other != null && Path == other.Path && Layer == other.Layer;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as UIType);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Path != null ? Path.GetHashCode() : 0) * 397) ^ (int)Layer;
        }
    }
}
