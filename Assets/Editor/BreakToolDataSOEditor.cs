using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Block;

[CustomEditor(typeof(BreakToolDataSO))]
public class BreakToolDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Load from CSV"))
            LoadCSV((BreakToolDataSO)target);
    }

    void LoadCSV(BreakToolDataSO so)
    {
        if (so.csvFile == null)
        {
            Debug.LogError("CSVファイルが設定されていません");
            return;
        }

        string[] lines = so.csvFile.text.Split('\n');
        if (lines.Length < 2) return;

        var headerMap = new Dictionary<string, int>();
        string[] headers = lines[0].Trim().Split(',');
        for (int i = 0; i < headers.Length; i++)
            headerMap[headers[i].Trim()] = i;

        var list = new List<BreakToolData>();

        for (int li = 1; li < lines.Length; li++)
        {
            string line = lines[li].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = line.Split(',');
            string name = Col(cols, headerMap, "Name");

            var data = new BreakToolData
            {
                Name = name,
                ItemAccess = new ItemAccess
                {
                    Category = (Item.ItemCategory)int.Parse(Col(cols, headerMap, "Category")),
                    Id = int.Parse(Col(cols, headerMap, "Id")),
                    Num = 0
                },
                UnitNum = int.Parse(Col(cols, headerMap, "UnitNum")),
                MaxNum = int.Parse(Col(cols, headerMap, "MaxNum")),
                Texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Texture/{name}.png"),
                Icon = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Icon/{name}.png"),
                BlockType = (BlockTypeEnum)int.Parse(Col(cols, headerMap, "BlockType")),
                Lv = int.Parse(Col(cols, headerMap, "Lv")),
                BreakPower = int.Parse(Col(cols, headerMap, "BreakPower"))
            };

            list.Add(data);
        }

        so.SetItemDatas(list.ToArray());
        AssetDatabase.SaveAssets();
        Debug.Log($"{list.Count}件のBreakToolデータを読み込みました");
    }

    string Col(string[] cols, Dictionary<string, int> map, string key)
    {
        return map.TryGetValue(key, out int i) && i < cols.Length ? cols[i].Trim() : "0";
    }
}
