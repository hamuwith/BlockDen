using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponBaseDataSO))]
public class WeaponBaseDataSOEditor : BlockDataSOEditor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Load from CSV"))
            LoadCSV((WeaponBaseDataSO)target);
    }

    void LoadCSV(WeaponBaseDataSO so)
    {
        if (so.csvFile == null) { Debug.LogError("CSVファイルが設定されていません"); return; }
        string[] lines = so.csvFile.text.Split('\n');
        if (lines.Length < 2) return;

        var map = BuildHeaderMap(lines[0]);
        var list = new List<WeaponBaseData>();
        for (int li = 1; li < lines.Length; li++)
        {
            string line = lines[li].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] cols = line.Split(',');
            var block = ParseBlockData(cols, map);
            var data = new WeaponBaseData
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
                BoardSize = new Vector2Int(
                    int.Parse(Col(cols, map, "BoardSizeX")),
                    int.Parse(Col(cols, map, "BoardSizeY"))
                )
            };
            list.Add(data);
        }
        so.SetItemDatas(list.ToArray());
        AssetDatabase.SaveAssets();
        Debug.Log($"{list.Count}件のWeaponBaseDataを読み込みました");
    }
}
