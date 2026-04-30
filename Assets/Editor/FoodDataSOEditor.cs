using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using static Item;

[CustomEditor(typeof(FoodDataSO))]
public class FoodDataSOEditor : Editor
{
    const int GridCols = 3;
    const int GridRows = 3;
    const float CellWidth = 52f;

    bool[] foldouts;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var so = (FoodDataSO)target;

        GUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load from CSV")) LoadCSV(so);
        if (GUILayout.Button("Save to CSV"))  SaveCSV(so);
        EditorGUILayout.EndHorizontal();

        if (so.ItemDatas == null || so.ItemDatas.Length == 0) return;

        if (foldouts == null || foldouts.Length != so.ItemDatas.Length)
            foldouts = new bool[so.ItemDatas.Length];

        GUILayout.Space(8);
        GUILayout.Label("Recipe Editor (3×3)", EditorStyles.boldLabel);

        for (int i = 0; i < so.ItemDatas.Length; i++)
        {
            var item = so.ItemDatas[i];
            if (item == null) continue;
            foldouts[i] = EditorGUILayout.Foldout(foldouts[i], item.Name, true);
            if (!foldouts[i]) continue;
            EditorGUI.indentLevel++;
            DrawRecipeGrid(so, item);
            EditorGUI.indentLevel--;
        }
    }

    void DrawRecipeGrid(FoodDataSO so, FoodData item)
    {
        EnsureSlots(so, item);

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
                var slot = item.RecipeSlots[slotIdx];

                EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(CellWidth));
                var newCat = (ItemCategory)EditorGUILayout.EnumPopup(slot.Category, GUILayout.Width(CellWidth - 4));
                var newId  = EditorGUILayout.IntField(slot.Id,  GUILayout.Width(CellWidth - 4));
                var newNum = EditorGUILayout.IntField(slot.Num, GUILayout.Width(CellWidth - 4));

                if (newCat != slot.Category || newId != slot.Id || newNum != slot.Num)
                {
                    item.RecipeSlots[slotIdx] = new ItemAccess { Category = newCat, Id = newId, Num = newNum };
                    EditorUtility.SetDirty(so);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    void EnsureSlots(FoodDataSO so, FoodData item)
    {
        int needed = CraftItemData.SmallRecipeSlotCount;
        if (item.RecipeSlots != null && item.RecipeSlots.Length == needed) return;
        var newSlots = new ItemAccess[needed];
        int copy = item.RecipeSlots != null ? Mathf.Min(item.RecipeSlots.Length, needed) : 0;
        for (int i = 0; i < copy; i++) newSlots[i] = item.RecipeSlots[i];
        for (int i = copy; i < needed; i++)
            newSlots[i] = new ItemAccess { Category = ItemCategory.Material, Id = -1, Num = 0 };
        item.RecipeSlots = newSlots;
        EditorUtility.SetDirty(so);
    }

    // -----------------------------------------------------------------------
    // Save to CSV
    // -----------------------------------------------------------------------

    void SaveCSV(FoodDataSO so)
    {
        if (so.csvFile == null) { Debug.LogError("CSVファイルが設定されていません"); return; }
        string path = AssetDatabase.GetAssetPath(so.csvFile);
        if (string.IsNullOrEmpty(path)) { Debug.LogError("CSVファイルのパスが取得できません"); return; }

        var sb = new StringBuilder();
        sb.Append("Name,Category,Id,UnitNum,MaxNum,Duration,MoveSpeed,Power,Damage");
        for (int m = 0; m < CraftItemData.SmallRecipeSlotCount; m++)
            sb.Append($",Mat{m}_Category,Mat{m}_Id,Mat{m}_Num");
        sb.AppendLine();

        foreach (var d in so.ItemDatas)
        {
            if (d == null) continue;
            sb.Append($"{d.Name},{(int)d.ItemAccess.Category},{d.ItemAccess.Id},{d.UnitNum},{d.MaxNum}");
            sb.Append($",{d.Duration},{d.MoveSpeed},{d.Power},{d.Damage}");
            AppendRecipeSlots(sb, d.RecipeSlots);
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"FoodDataをCSVに保存しました: {path}");
    }

    void AppendRecipeSlots(StringBuilder sb, ItemAccess[] slots)
    {
        for (int m = 0; m < CraftItemData.SmallRecipeSlotCount; m++)
        {
            if (slots != null && m < slots.Length && slots[m].Id >= 0)
            {
                var s = slots[m];
                sb.Append($",{(int)s.Category},{s.Id},{s.Num}");
            }
            else
            {
                sb.Append(",-1,-1,-1");
            }
        }
    }

    // -----------------------------------------------------------------------
    // Load from CSV
    // -----------------------------------------------------------------------

    void LoadCSV(FoodDataSO so)
    {
        if (so.csvFile == null) { Debug.LogError("CSVファイルが設定されていません"); return; }
        string[] lines = so.csvFile.text.Split('\n');
        if (lines.Length < 2) return;

        var map = new Dictionary<string, int>();
        string[] headers = lines[0].Trim().Split(',');
        for (int i = 0; i < headers.Length; i++) map[headers[i].Trim()] = i;

        var list = new List<FoodData>();
        for (int li = 1; li < lines.Length; li++)
        {
            string line = lines[li].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] cols = line.Split(',');
            string name = Col(cols, map, "Name");
            ItemAccess[] recipeSlots = ParseRecipeSlots(cols, map);
            list.Add(new FoodData
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
                Duration  = int.Parse(Col(cols, map, "Duration")),
                MoveSpeed = float.Parse(Col(cols, map, "MoveSpeed")),
                Power     = int.Parse(Col(cols, map, "Power")),
                Damage    = int.Parse(Col(cols, map, "Damage")),
                RecipeSlots = recipeSlots
            });
        }
        so.SetItemDatas(list.ToArray());
        AssetDatabase.SaveAssets();
        Debug.Log($"{list.Count}件のFoodDataを読み込みました");
    }

    ItemAccess[] ParseRecipeSlots(string[] cols, Dictionary<string, int> map)
    {
        var slots = new ItemAccess[CraftItemData.SmallRecipeSlotCount];
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
