using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Item;

[CustomEditor(typeof(MaterialDataSO))]
public class MaterialDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Load from CSV"))
            LoadCSV((MaterialDataSO)target);
    }

    void LoadCSV(MaterialDataSO so)
    {
        if (so.csvFile == null) { Debug.LogError("CSVファイルが設定されていません"); return; }
        string[] lines = so.csvFile.text.Split('\n');
        if (lines.Length < 2) return;

        var map = new Dictionary<string, int>();
        string[] headers = lines[0].Trim().Split(',');
        for (int i = 0; i < headers.Length; i++) map[headers[i].Trim()] = i;

        var list = new List<MaterialData>();
        for (int li = 1; li < lines.Length; li++)
        {
            string line = lines[li].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] cols = line.Split(',');
            string name = Col(cols, map, "Name");
            list.Add(new MaterialData
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
                MaterialType = (MaterialType)int.Parse(Col(cols, map, "MaterialType")),
                ItemMaterials = ParseMaterials(cols, map)
            });
        }
        so.SetItemDatas(list.ToArray());
        AssetDatabase.SaveAssets();
        Debug.Log($"{list.Count}件のMaterialDataを読み込みました");
    }

    List<ItemAccess> ParseMaterials(string[] cols, Dictionary<string, int> map)
    {
        var list = new List<ItemAccess>();
        for (int m = 0; m < 4; m++)
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
