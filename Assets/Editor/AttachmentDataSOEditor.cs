using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Item;

[CustomEditor(typeof(AttachmentDataSO))]
public class AttachmentDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Load from CSV"))
            LoadCSV((AttachmentDataSO)target);
    }

    void LoadCSV(AttachmentDataSO so)
    {
        if (so.csvFile == null) { Debug.LogError("CSVファイルが設定されていません"); return; }
        string[] lines = so.csvFile.text.Split('\n');
        if (lines.Length < 2) return;

        var map = new Dictionary<string, int>();
        string[] headers = lines[0].Trim().Split(',');
        for (int i = 0; i < headers.Length; i++) map[headers[i].Trim()] = i;

        var list = new List<AttachmentData>();
        for (int li = 1; li < lines.Length; li++)
        {
            string line = lines[li].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] cols = line.Split(',');
            string name = Col(cols, map, "Name");

            var cells = new bool[9];
            for (int c = 0; c < 9; c++)
                cells[c] = Col(cols, map, $"Cell{c}") == "1";

            list.Add(new AttachmentData
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
                AttachmentStatus = new AttachmentStatus
                {
                    Damage = int.Parse(Col(cols, map, "Damage")),
                    AttackSpeed = int.Parse(Col(cols, map, "AttackSpeed")),
                    AttackRange = int.Parse(Col(cols, map, "AttackRange")),
                    Effection = int.Parse(Col(cols, map, "Effection")),
                    Ice = int.Parse(Col(cols, map, "Ice")),
                    Poison = int.Parse(Col(cols, map, "Poison")),
                    Lightning = int.Parse(Col(cols, map, "Lightning")),
                    Shining = int.Parse(Col(cols, map, "Shining")),
                    Dark = int.Parse(Col(cols, map, "Dark")),
                    Strong = int.Parse(Col(cols, map, "Strong"))
                },
                Shape = new AttachmentShape
                {
                    cells = cells,
                    width = int.Parse(Col(cols, map, "ShapeWidth")),
                    height = int.Parse(Col(cols, map, "ShapeHeight"))
                },
                ItemMaterials = ParseMaterials(cols, map)
            });
        }
        so.SetItemDatas(list.ToArray());
        AssetDatabase.SaveAssets();
        Debug.Log($"{list.Count}件のAttachmentDataを読み込みました");
    }

    List<ItemAccess> ParseMaterials(string[] cols, Dictionary<string, int> map)
    {
        var list = new List<ItemAccess>();
        for (int m = 0; m < 9; m++)
        {
            int matId = int.Parse(Col(cols, map, $"Mat{m}_Id"));
            if (matId < 0) continue;
            list.Add(new ItemAccess
            {
                Category = (ItemCategory)int.Parse(Col(cols, map, $"Mat{m}_Category")),
                Id = matId,
                Num = int.Parse(Col(cols, map, $"Mat{m}_Num"))
            });
        }
        return list;
    }

    string Col(string[] cols, Dictionary<string, int> map, string key)
        => map.TryGetValue(key, out int i) && i < cols.Length ? cols[i].Trim() : "0";
}
