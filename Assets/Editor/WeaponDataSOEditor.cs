using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponDataSO))]
public class WeaponDataSOEditor : BlockDataSOEditor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Load from CSV"))
            LoadCSV((WeaponDataSO)target);
    }

    void LoadCSV(WeaponDataSO so)
    {
        if (so.csvFile == null) { Debug.LogError("CSVファイルが設定されていません"); return; }
        string[] lines = so.csvFile.text.Split('\n');
        if (lines.Length < 2) return;

        var map = BuildHeaderMap(lines[0]);
        var list = new List<WeaponData>();
        for (int li = 1; li < lines.Length; li++)
        {
            string line = lines[li].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] cols = line.Split(',');
            var block = ParseBlockData(cols, map);
            var data = new WeaponData
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
                ArrowId = int.Parse(Col(cols, map, "ArrowId")),
                Damage = int.Parse(Col(cols, map, "Damage")),
                AttackSpeed = int.Parse(Col(cols, map, "AttackSpeed")),
                Range = float.Parse(Col(cols, map, "Range"))
            };
            list.Add(data);
        }
        so.SetItemDatas(list.ToArray());
        AssetDatabase.SaveAssets();
        Debug.Log($"{list.Count}件のWeaponDataを読み込みました");
    }
}
