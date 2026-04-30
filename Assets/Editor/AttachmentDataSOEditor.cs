using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using static Item;

[CustomEditor(typeof(AttachmentDataSO))]
public class AttachmentDataSOEditor : Editor
{
    const int GridCols = 3;
    const int GridRows = 3;
    const float CellWidth = 52f;

    bool[] foldouts;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var so = (AttachmentDataSO)target;

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
            DrawShapeGrid(so, item);
            DrawRecipeGrid(so, item);
            EditorGUI.indentLevel--;
        }
    }

    void DrawShapeGrid(AttachmentDataSO so, AttachmentData item)
    {
        var shape = item.Shape;
        if (shape.cells == null || shape.cells.Length < 9)
        {
            shape.cells = new bool[9];
            item.Shape = shape;
            EditorUtility.SetDirty(so);
        }

        GUILayout.Label("Shape", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("W:", GUILayout.Width(20));
        int newW = EditorGUILayout.IntField(shape.width,  GUILayout.Width(30));
        GUILayout.Label("H:", GUILayout.Width(20));
        int newH = EditorGUILayout.IntField(shape.height, GUILayout.Width(30));
        EditorGUILayout.EndHorizontal();

        if (newW != shape.width || newH != shape.height)
        {
            shape.width  = newW;
            shape.height = newH;
            item.Shape = shape;
            EditorUtility.SetDirty(so);
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(15);
        for (int col = 0; col < 3; col++)
            GUILayout.Label($"  {col}", GUILayout.Width(34));
        EditorGUILayout.EndHorizontal();

        for (int row = 2; row >= 0; row--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(row.ToString(), GUILayout.Width(15));
            for (int col = 0; col < 3; col++)
            {
                int cellIdx = row * 3 + col;
                bool cellVal = shape.cells[cellIdx];
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = cellVal ? Color.green : Color.gray;
                if (GUILayout.Button("", GUILayout.Width(34), GUILayout.Height(24)))
                {
                    shape.cells[cellIdx] = !cellVal;
                    item.Shape = shape;
                    EditorUtility.SetDirty(so);
                }
                GUI.backgroundColor = prev;
            }
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(4);
    }

    void DrawRecipeGrid(AttachmentDataSO so, AttachmentData item)
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

    void EnsureSlots(AttachmentDataSO so, AttachmentData item)
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

    void SaveCSV(AttachmentDataSO so)
    {
        if (so.csvFile == null) { Debug.LogError("CSVファイルが設定されていません"); return; }
        string path = AssetDatabase.GetAssetPath(so.csvFile);
        if (string.IsNullOrEmpty(path)) { Debug.LogError("CSVファイルのパスが取得できません"); return; }

        var sb = new StringBuilder();
        sb.Append("Name,Category,Id,UnitNum,MaxNum,Damage,AttackSpeed,AttackRange,Effection,Ice,Poison,Lightning,Shining,Dark,Strong,ShapeWidth,ShapeHeight");
        for (int c = 0; c < 9; c++) sb.Append($",Cell{c}");
        for (int m = 0; m < CraftItemData.SmallRecipeSlotCount; m++)
            sb.Append($",Mat{m}_Category,Mat{m}_Id,Mat{m}_Num");
        sb.AppendLine();

        foreach (var d in so.ItemDatas)
        {
            if (d == null) continue;
            var st = d.AttachmentStatus;
            var sh = d.Shape;
            sb.Append($"{d.Name},{(int)d.ItemAccess.Category},{d.ItemAccess.Id},{d.UnitNum},{d.MaxNum}");
            sb.Append($",{st.Damage},{st.AttackSpeed},{st.AttackRange},{st.Effection}");
            sb.Append($",{st.Ice},{st.Poison},{st.Lightning},{st.Shining},{st.Dark},{st.Strong}");
            sb.Append($",{sh.width},{sh.height}");
            for (int c = 0; c < 9; c++)
            {
                bool cell = sh.cells != null && c < sh.cells.Length && sh.cells[c];
                sb.Append($",{(cell ? 1 : 0)}");
            }
            AppendRecipeSlots(sb, d.RecipeSlots);
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"AttachmentDataをCSVに保存しました: {path}");
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
            ItemAccess[] recipeSlots = ParseRecipeSlots(cols, map);

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
                    Damage       = int.Parse(Col(cols, map, "Damage")),
                    AttackSpeed  = int.Parse(Col(cols, map, "AttackSpeed")),
                    AttackRange  = int.Parse(Col(cols, map, "AttackRange")),
                    Effection    = int.Parse(Col(cols, map, "Effection")),
                    Ice          = int.Parse(Col(cols, map, "Ice")),
                    Poison       = int.Parse(Col(cols, map, "Poison")),
                    Lightning    = int.Parse(Col(cols, map, "Lightning")),
                    Shining      = int.Parse(Col(cols, map, "Shining")),
                    Dark         = int.Parse(Col(cols, map, "Dark")),
                    Strong       = int.Parse(Col(cols, map, "Strong"))
                },
                Shape = new AttachmentShape
                {
                    cells  = cells,
                    width  = int.Parse(Col(cols, map, "ShapeWidth")),
                    height = int.Parse(Col(cols, map, "ShapeHeight"))
                },
                RecipeSlots = recipeSlots
            });
        }
        so.SetItemDatas(list.ToArray());
        AssetDatabase.SaveAssets();
        Debug.Log($"{list.Count}件のAttachmentDataを読み込みました");
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
