using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Item;

[CustomEditor(typeof(SeedDataSO))]
public class SeedDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Load from CSV"))
            LoadCSV((SeedDataSO)target);
    }

    void LoadCSV(SeedDataSO so)
    {
        if (so.csvFile == null) { Debug.LogError("CSVファイルが設定されていません"); return; }
        string[] lines = so.csvFile.text.Split('\n');
        if (lines.Length < 2) return;

        var map = new Dictionary<string, int>();
        string[] headers = lines[0].Trim().Split(',');
        for (int i = 0; i < headers.Length; i++) map[headers[i].Trim()] = i;

        var list = new List<SeedData>();
        for (int li = 1; li < lines.Length; li++)
        {
            string line = lines[li].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] cols = line.Split(',');
            string name = Col(cols, map, "Name");
            list.Add(new SeedData
            {
                Name = name,
                ItemAccess = new ItemAccess
                {
                    Category = (ItemCategory)int.Parse(Col(cols, map, "Category")),
                    Id = int.Parse(Col(cols, map, "Id")),
                    Num = 0
                },
                UnitNum = int.Parse(Col(cols, map, "UnitNum")),
                MaxNum = int.Parse(Col(cols, map, "MaxNum")),
                Texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Texture/{name}.png"),
                Icon = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Icon/{name}.png"),
                GrowNum = int.Parse(Col(cols, map, "GrowNum")),
                GrowBlock = new ItemAccess
                {
                    Category = (ItemCategory)int.Parse(Col(cols, map, "GrowBlock_Category")),
                    Id = int.Parse(Col(cols, map, "GrowBlock_Id")),
                    Num = 0
                }
            });
        }
        so.SetItemDatas(list.ToArray());
        AssetDatabase.SaveAssets();
        Debug.Log($"{list.Count}件のSeedDataを読み込みました");
    }

    string Col(string[] cols, Dictionary<string, int> map, string key)
        => map.TryGetValue(key, out int i) && i < cols.Length ? cols[i].Trim() : "0";
}
