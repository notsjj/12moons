using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class CSVReader
{
    public static List<DialogueEntry> ReadDialogueCSV(string filePath)
    {
        List<DialogueEntry> dialogues = new List<DialogueEntry>();

        try
        {
            // 在Unity中，CSV文件应该放在Resources文件夹或StreamingAssets文件夹
            string[] csvTexts = File.ReadAllLines(Application.streamingAssetsPath + "/" + filePath + ".csv", Encoding.UTF8);

            if (csvTexts == null)
            {
                Debug.LogError("CSV文件未找到: " + filePath);
                return dialogues;
            }

            List<string> data = new List<string>();
            for (int i = 0; i < csvTexts.Length; i++)
            {
                if (string.IsNullOrEmpty(csvTexts[i].Trim())) 
                    continue;

                //处理CSV格式(考虑引号内的逗号)
                string[] fields = ParseCSVLine(csvTexts[i]);

                //如果是数字，那么创建新的data，并将上一个保存为dialogueEntry
                if (int.TryParse(fields[0], out int id))
                {
                    if (fields.Length > 0 && !string.IsNullOrEmpty(fields[0]))
                    {
                        if(data.Count > 0)
                        {
                            dialogues.Add(new DialogueEntry(data));
                            data.Clear();
                        }

                        data.Add(fields[0]);
                    }
                }
                //如果不是，那么直接添加到data中
                else
                {
                    data.Add(fields[0]);
                }
            }
            dialogues.Add(new DialogueEntry(data));

            Debug.Log($"成功加载 {dialogues.Count} 组对话");
        }
        catch (System.Exception e)
        {
            Debug.LogError("读取CSV文件时出错: " + e.Message);
        }

        return dialogues;
    }

    // 解析CSV行，处理引号内的逗号
    private static string[] ParseCSVLine(string line)
    {
        List<string> fields = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.Trim());
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        fields.Add(currentField.Trim());
        return fields.ToArray();
    }
}