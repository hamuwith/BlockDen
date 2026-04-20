using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Item;

[CustomEditor(typeof(FertilizerDataSO))]
public class FertilizerDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Load from CSV"))
            LoadCSV((FertilizerDataSO)target);
    }

    void LoadCSV(FertilizerDataSO so)
    {
        if (so.csvFile == null) { Debug.LogError("CSVファイルが設定されていません"); return; }
        string[] lines = so.csvFile.text.Split('\n');
        if (lines.Length < 2) return;

        var map = new Dictionary<string, int>();
        string[] headers = lines[0].Trim().Split(',');
        for (int i = 0; i < headers.Length; i++) map[headers[i].Trim()] = i;

        var list = new List<FertilizerData>();
        for (int li = 1; li < lines.Length; li++)
        {
            string line = lines[li].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] cols = line.Split(',');
            string name = Col(cols, map, "Name");
            var cells = new bool[9];
            for (int c = 0; c < 9; c++) cells[c] = Col(cols, map, $"Cell{c}") == "1";
            list.Add(new FertilizerData
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
                FertilizerStatus = new FertilizerStatus
                {
                    Rate = int.Parse(Col(cols, map, "Rate")),
                    PlusNum = int.Parse(Col(cols, map, "PlusNum")),
                    Speed = int.Parse(Col(cols, map, "Speed"))
                },
                Shape = new AttachmentShape
                {
                    cells = cells,
                    width = int.Parse(Col(cols, map, "ShapeWidth")),
                    height = int.Parse(Col(cols, map, "ShapeHeight"))
                }
            });
        }
        so.SetItemDatas(list.ToArray());
        AssetDatabase.SaveAssets();
        Debug.Log($"{list.Count}件のFertilizerDataを読み込みました");
    }

    string Col(string[] cols, Dictionary<string, int> map, string key)
        => map.TryGetValue(key, out int i) && i < cols.Length ? cols[i].Trim() : "0";
}
