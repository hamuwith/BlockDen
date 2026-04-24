using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Item;
using static Player;

[CustomEditor(typeof(BoxDataSO))]
public class BoxDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Load from CSV"))
            LoadCSV((BoxDataSO)target);
    }

    void LoadCSV(BoxDataSO so)
    {
        if (so.csvFile == null) { Debug.LogError("CSVç¹è¼”ãƒç¹§ï½¤ç¹ï½«ç¸ºç‘šï½¨ï½­èž³å£¹ï¼†ç¹§å¾Œâ€»ç¸ºãƒ»âˆªç¸ºå¸™ï½“"); return; }
        string[] lines = so.csvFile.text.Split('\n');
        if (lines.Length < 2) return;

        var map = new Dictionary<string, int>();
        string[] headers = lines[0].Trim().Split(',');
        for (int i = 0; i < headers.Length; i++) map[headers[i].Trim()] = i;

        var list = new List<BoxData>();
        for (int li = 1; li < lines.Length; li++)
        {
            string line = lines[li].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] cols = line.Split(',');
            string name = Col(cols, map, "Name");
            list.Add(new BoxData
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
                InventoryType = (InventoryType)int.Parse(Col(cols, map, "InventoryType")),
                ItemMaterials = ParseMaterials(cols, map)
            });
        }
        so.SetItemDatas(list.ToArray());
        AssetDatabase.SaveAssets();
        Debug.Log($"{list.Count}èŽ‰ï½¶ç¸ºï½®BoxDataç¹§å®šï½ªï½­ç¸ºï½¿éœŽï½¼ç¸ºï½¿ç¸ºï½¾ç¸ºåŠ±â—†");
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
