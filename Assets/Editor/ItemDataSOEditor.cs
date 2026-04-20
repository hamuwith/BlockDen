using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemDataSO))]
public class ItemDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Load from CSV"))
            LoadCSV((ItemDataSO)target);
    }

    void LoadCSV(ItemDataSO so)
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

        var list = new List<ItemData>();

        for (int li = 1; li < lines.Length; li++)
        {
            string line = lines[li].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = line.Split(',');
            var data = new ItemData
            {
                Name = Col(cols, headerMap, "Name"),
                ItemAccess = new ItemAccess
                {
                    Category = (Item.ItemCategory)int.Parse(Col(cols, headerMap, "Category")),
                    Id = int.Parse(Col(cols, headerMap, "Id")),
                    Num = 0
                },
                ItemMaterials = new List<ItemAccess>(),
                UnitNum = int.Parse(Col(cols, headerMap, "UnitNum")),
                MaxNum = int.Parse(Col(cols, headerMap, "MaxNum")),
                Texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Texture/{Col(cols, headerMap, "Name")}.png"),
                Icon = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Icon/{Col(cols, headerMap, "Name")}.png")
            };

            for (int m = 0; m < 4; m++)
            {
                int matId = int.Parse(Col(cols, headerMap, $"Mat{m}_Id"));
                if (matId < 0) continue;
                data.ItemMaterials.Add(new ItemAccess
                {
                    Category = (Item.ItemCategory)int.Parse(Col(cols, headerMap, $"Mat{m}_Category")),
                    Id = matId,
                    Num = int.Parse(Col(cols, headerMap, $"Mat{m}_Num"))
                });
            }

            list.Add(data);
        }

        so.SetItemDatas(list.ToArray());
        AssetDatabase.SaveAssets();
        Debug.Log($"{list.Count}件のアイテムを読み込みました");
    }

    string Col(string[] cols, Dictionary<string, int> map, string key)
    {
        return map.TryGetValue(key, out int i) && i < cols.Length ? cols[i].Trim() : "0";
    }
}
