using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializableVector3
{
    public float x, y, z;

    public SerializableVector3(Vector3 pos)
    {
        this.x = pos.x;
        this.y = pos.y;
        this.z = pos.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }

    public Vector2Int ToVector2Int()
    {
        return new Vector2Int((int)x, (int)y);
    }
}

[System.Serializable]
public class DialogueEntry
{
    public string id;           // 对话ID
    public List<string> text;         // 对话内容

    public DialogueEntry(List<string> data)
    {
        text = new List<string>();

        //data的第一个元素是id，后续元素是text内容
        if (data.Count >= 1) id = data[0];
        for (int i = 1; i < data.Count; i++)
        {
            text.Add(data[i]);
        }
    }
}


