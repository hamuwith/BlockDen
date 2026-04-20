using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Item;

[CustomEditor(typeof(SeedDataSO))]
public class SeedDataSOEditor : BlockDataSOEditor
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

        var map = BuildHeaderMap(lines[0]);
        var list = new List<SeedData>();
        for (int li = 1; li < lines.Length; li++)
        {
            string line = lines[li].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] cols = line.Split(',');
            var block = ParseBlockData(cols, map);
            var data = new SeedData
            {
                Name = block.Name,
                ItemAccess = block.ItemAccess,
                UnitNum = block.UnitNum,
                MaxNum = block.MaxNum,
                Texture2D = block.Texture2D,
                Icon = block.Icon,
                BlockType = block.BlockType,
                Hardness = block.Hardness,
                Life = block.Life,
                DropItem100 = block.DropItem100,
                ItemPercents = block.ItemPercents,
                GrowNum = int.Parse(Col(cols, map, "GrowNum")),
                GrowBlock = new ItemAccess
                {
                    Category = (ItemCategory)int.Parse(Col(cols, map, "GrowBlock_Category")),
                    Id = int.Parse(Col(cols, map, "GrowBlock_Id")),
                    Num = 0
                }
            };
            list.Add(data);
        }
        so.SetItemDatas(list.ToArray());
        AssetDatabase.SaveAssets();
        Debug.Log($"{list.Count}件のSeedDataを読み込みました");
    }
}
