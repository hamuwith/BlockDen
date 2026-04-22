using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Block;
using static Item;

[CustomEditor(typeof(BlockDataSO))]
public class BlockDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Load from CSV"))
            LoadCSV((BlockDataSO)target);
    }

    void LoadCSV(BlockDataSO so)
    {
        if (so.csvFile == null) { Debug.LogError("CSVファイルが設定されていません"); return; }
        string[] lines = so.csvFile.text.Split('\n');
        if (lines.Length < 2) return;

        var map = BuildHeaderMap(lines[0]);
        var list = new List<BlockData>();
        for (int li = 1; li < lines.Length; li++)
        {
            string line = lines[li].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            list.Add(ParseBlockData(line.Split(','), map));
        }
        so.SetItemDatas(list.ToArray());
        AssetDatabase.SaveAssets();
        Debug.Log($"{list.Count}件のBlockDataを読み込みました");
    }

    protected static BlockData ParseBlockData(string[] cols, Dictionary<string, int> map)
    {
        string name = Col(cols, map, "Name");
        var data = new BlockData
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
            BlockType = (BlockTypeEnum)int.Parse(Col(cols, map, "BlockType")),
            Hardness = int.Parse(Col(cols, map, "Hardness")),
            Life = int.Parse(Col(cols, map, "Life")),
            DropItem100 = new ItemAccess
            {
                Category = (ItemCategory)int.Parse(Col(cols, map, "Drop100_Category")),
                Id = int.Parse(Col(cols, map, "Drop100_Id")),
                Num = 1
            },
            ItemPercents = ParseItemPercents(cols, map)
        };
        return data;
    }

    protected static ItemPercent[] ParseItemPercents(string[] cols, Dictionary<string, int> map)
    {
        var percents = new List<ItemPercent>();
        for (int p = 0; p < 3; p++)
        {
            int id = int.Parse(Col(cols, map, $"Pct{p}_Id"));
            if (id < 0) continue;
            percents.Add(new ItemPercent
            {
                ItemAccess = new ItemAccess
                {
                    Category = (ItemCategory)int.Parse(Col(cols, map, $"Pct{p}_Category")),
                    Id = id,
                    Num = int.Parse(Col(cols, map, $"Pct{p}_Num"))
                },
                Percent = int.Parse(Col(cols, map, $"Pct{p}_Percent"))
            });
        }
        return percents.ToArray();
    }

    protected static Dictionary<string, int> BuildHeaderMap(string headerLine)
    {
        var map = new Dictionary<string, int>();
        string[] headers = headerLine.Trim().Split(',');
        for (int i = 0; i < headers.Length; i++) map[headers[i].Trim()] = i;
        return map;
    }

    protected static string Col(string[] cols, Dictionary<string, int> map, string key)
        => map.TryGetValue(key, out int i) && i < cols.Length ? cols[i].Trim() : "0";
}
