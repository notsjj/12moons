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

        Path = path.Replace("\\", "/");
        Layer = layer;
        Name = Path.Substring(Path.LastIndexOf('/') + 1);
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
