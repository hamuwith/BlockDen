using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using static Item;

[CustomEditor(typeof(WeaponDataSO))]
public class WeaponDataSOEditor : Editor
{
    const int GridCols = 5;
    const int GridRows = 5;
    const float CellWidth = 52f;

    bool[] foldouts;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var so = (WeaponDataSO)target;

        GUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load from CSV")) LoadCSV(so);
        if (GUILayout.Button("Save to CSV"))  SaveCSV(so);
        EditorGUILayout.EndHorizontal();

        if (so.ItemDatas == null || so.ItemDatas.Length == 0) return;

        if (foldouts == null || foldouts.Length != so.ItemDatas.Length)
            foldouts = new bool[so.ItemDatas.Length];

        GUILayout.Space(8);
        GUILayout.Label("Recipe Editor (5×5)", EditorStyles.boldLabel);

        for (int i = 0; i < so.ItemDatas.Length; i++)
        {
            var weapon = so.ItemDatas[i];
            if (weapon == null) continue;

            foldouts[i] = EditorGUILayout.Foldout(foldouts[i], weapon.Name, true);
            if (!foldouts[i]) continue;

            EditorGUI.indentLevel++;
            DrawRecipeGrid(so, weapon);
            EditorGUI.indentLevel--;
        }
    }

    void DrawRecipeGrid(WeaponDataSO so, WeaponData weapon)
    {
        EnsureSlots(so, weapon);

        // Column header
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(15);
        for (int col = 0; col < GridCols; col++)
            GUILayout.Label($"  {col}", GUILayout.Width(CellWidth));
        EditorGUILayout.EndHorizontal();

        for (int row = GridRows - 1; row >= 0; row--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(row.ToString(), GUILayout.Width(15));

            for (int col = 0; col < GridCols; col++)
            {
                int slotIdx = row * GridCols + col;
                var slot = weapon.RecipeSlots[slotIdx];

                EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(CellWidth));

                var newCat = (ItemCategory)EditorGUILayout.EnumPopup(slot.Category, GUILayout.Width(CellWidth - 4));
                var newId  = EditorGUILayout.IntField(slot.Id,  GUILayout.Width(CellWidth - 4));
                var newNum = EditorGUILayout.IntField(slot.Num, GUILayout.Width(CellWidth - 4));

                if (newCat != slot.Category || newId != slot.Id || newNum != slot.Num)
                {
                    weapon.RecipeSlots[slotIdx] = new ItemAccess
                    {
                        Category = newCat,
                        Id       = newId,
                        Num      = newNum
                    };
                    weapon.RecipeBounds    = CraftItemData.ComputeSlotBounds(weapon.RecipeSlots, GridCols, GridRows);
                    weapon.HasRecipeBounds = true;
                    EditorUtility.SetDirty(so);
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    void EnsureSlots(WeaponDataSO so, WeaponData weapon)
    {
        int needed = CraftItemData.LargeRecipeSlotCount;
        if (weapon.RecipeSlots != null && weapon.RecipeSlots.Length == needed) return;

        var newSlots = new ItemAccess[needed];
        int copy = weapon.RecipeSlots != null ? Mathf.Min(weapon.RecipeSlots.Length, needed) : 0;
        for (int i = 0; i < copy; i++) newSlots[i] = weapon.RecipeSlots[i];
        for (int i = copy; i < needed; i++)
            newSlots[i] = new ItemAccess { Category = ItemCategory.Material, Id = -1, Num = 0 };
        weapon.RecipeSlots = newSlots;
        EditorUtility.SetDirty(so);
    }

    // -----------------------------------------------------------------------
    // Save to CSV
    // -----------------------------------------------------------------------

    void SaveCSV(WeaponDataSO so)
    {
        if (so.csvFile == null) { Debug.LogError("CSVファイルが設定されていません"); return; }

        string path = AssetDatabase.GetAssetPath(so.csvFile);
        if (string.IsNullOrEmpty(path)) { Debug.LogError("CSVファイルのパスが取得できません"); return; }

        var sb = new StringBuilder();

        // Header
        sb.Append("Name,Category,Id,UnitNum,MaxNum,ArrowId,Damage,AttackSpeed,Range");
        for (int m = 0; m < CraftItemData.LargeRecipeSlotCount; m++)
            sb.Append($",Mat{m}_Category,Mat{m}_Id,Mat{m}_Num");
        sb.AppendLine();

        // Rows
        foreach (var w in so.ItemDatas)
        {
            if (w == null) continue;
            sb.Append($"{w.Name},{(int)w.ItemAccess.Category},{w.ItemAccess.Id}");
            sb.Append($",{w.UnitNum},{w.MaxNum}");
            sb.Append($",{w.ArrowId},{w.Damage},{w.AttackSpeed},{w.Range}");

            for (int m = 0; m < CraftItemData.LargeRecipeSlotCount; m++)
            {
                if (w.RecipeSlots != null && m < w.RecipeSlots.Length && w.RecipeSlots[m].Id >= 0)
                {
                    var s = w.RecipeSlots[m];
                    sb.Append($",{(int)s.Category},{s.Id},{s.Num}");
                }
                else
                {
                    sb.Append(",-1,-1,-1");
                }
            }
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"WeaponDataをCSVに保存しました: {path}");
    }

    // -----------------------------------------------------------------------
    // Load from CSV
    // -----------------------------------------------------------------------

    void LoadCSV(WeaponDataSO so)
    {
        if (so.csvFile == null) { Debug.LogError("CSVファイルが設定されていません"); return; }
        string[] lines = so.csvFile.text.Split('\n');
        if (lines.Length < 2) return;

        var map = new Dictionary<string, int>();
        string[] headers = lines[0].Trim().Split(',');
        for (int i = 0; i < headers.Length; i++) map[headers[i].Trim()] = i;

        var list = new List<WeaponData>();
        for (int li = 1; li < lines.Length; li++)
        {
            string line = lines[li].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] cols = line.Split(',');
            string name = Col(cols, map, "Name");
            ItemAccess[] recipeSlots = ParseRecipeSlots(cols, map);
            list.Add(new WeaponData
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
                ArrowId = int.Parse(Col(cols, map, "ArrowId")),
                Damage = int.Parse(Col(cols, map, "Damage")),
                AttackSpeed = int.Parse(Col(cols, map, "AttackSpeed")),
                Range = float.Parse(Col(cols, map, "Range")),
                RecipeSlots = recipeSlots,
                HasRecipeBounds = true,
                RecipeBounds = CraftItemData.ComputeSlotBounds(recipeSlots, 5, 5)
            });
        }
        so.SetItemDatas(list.ToArray());
        AssetDatabase.SaveAssets();
        Debug.Log($"{list.Count}件のWeaponDataを読み込みました");
    }

    ItemAccess[] ParseRecipeSlots(string[] cols, Dictionary<string, int> map)
    {
        var slots = new ItemAccess[CraftItemData.LargeRecipeSlotCount];
        for (int m = 0; m < slots.Length; m++)
        {
            int matId = ReadInt(cols, map, $"Mat{m}_Id", -1);
            if (matId < 0)
            {
                slots[m] = new ItemAccess { Category = ItemCategory.Material, Id = -1, Num = 0 };
                continue;
            }
            slots[m] = new ItemAccess
            {
                Category = (ItemCategory)ReadInt(cols, map, $"Mat{m}_Category", (int)ItemCategory.Material),
                Id       = matId,
                Num      = ReadInt(cols, map, $"Mat{m}_Num", 1)
            };
        }
        return slots;
    }

    string Col(string[] cols, Dictionary<string, int> map, string key)
        => map.TryGetValue(key, out int i) && i < cols.Length ? cols[i].Trim() : "0";

    int ReadInt(string[] cols, Dictionary<string, int> map, string key, int defaultValue)
    {
        if (!map.TryGetValue(key, out int i) || i >= cols.Length) return defaultValue;
        return int.TryParse(cols[i].Trim(), out int value) ? value : defaultValue;
    }
}
