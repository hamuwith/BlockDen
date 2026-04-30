using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDataSO", menuName = "Scriptable Objects/ItemDataSO")]
public class ItemDataSO : ScriptableObject
{
    [SerializeField] public TextAsset csvFile;
    [SerializeField] protected ItemData[] itemDatas;
    public ItemData[] ItemDatas => itemDatas;

#if UNITY_EDITOR
    public void SetItemDatas(ItemData[] datas)
    {
        itemDatas = datas;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

[System.Serializable]
public class ItemData : ItemDataBase
{
    public List<ItemAccess> ItemMaterials;
}
[System.Serializable]
public class CraftItemData : ItemDataBase
{
    public const int SmallRecipeSlotCount = 9;
    public const int LargeRecipeSlotCount = 25;

    public ItemAccess[] RecipeSlots = new ItemAccess[SmallRecipeSlotCount];

    public bool HasRecipeBounds;
    public RectInt RecipeBounds;

    public static RectInt ComputeSlotBounds(ItemAccess[] slots, int gridWidth, int gridHeight)
    {
        int minX = gridWidth, minY = gridHeight, maxX = -1, maxY = -1;
        int cellCount = Mathf.Min(slots.Length, gridWidth * gridHeight);
        for (int i = 0; i < cellCount; i++)
        {
            if (slots[i].Id < 0) continue;
            int x = i % gridWidth, y = i / gridWidth;
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }
        return maxX < 0 ? new RectInt() : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }
}
[System.Serializable]
public class ItemDataBase
{
    public string Name;
    public ItemAccess ItemAccess;
    public int UnitNum;
    public int MaxNum;
    public Texture2D Texture2D;
    public Sprite Icon;
}
