using UnityEngine;
using System.Collections.Generic;
using static Item;

public class ItemManager : MonoBehaviour
{
    const int MinWeaponCraftMaterialCount = 5;
    static readonly Vector2Int SmallRecipeSize = new Vector2Int(3, 3);
    static readonly Vector2Int WeaponRecipeSize = new Vector2Int(5, 5);

    [SerializeField] BlockDataSO natureBlockDataSO;
    [SerializeField] BlockDataSO unnaturalBlockDataSO;
    [SerializeField] BreakToolDataSO breakToolDataSO;
    [SerializeField] WeaponDataSO weaponDataSO;
    [SerializeField] AttachmentDataSO statusDataSO;
    [SerializeField] FoodDataSO foodDataSO;
    [SerializeField] SeedDataSO seedDataSO;
    [SerializeField] FertilizerDataSO fertilizerDataSO;
    [SerializeField] MaterialDataSO materialDataSO;
    [SerializeField] ItemDataSO bagDataSO;
    [SerializeField] ToolDataSO toolDataSO;
    [SerializeField] BoxDataSO boxDataSO;
    [SerializeField] WeaponBaseDataSO weaponBaseDataSO;
    [SerializeField] Item itemPrefab;
    [SerializeField] Block blockPrefab;
    [SerializeField] Seed seedPrefab;
    [SerializeField] Weapon weaponPrefab;
    [SerializeField] BreakTool breakToolPrefab;
    [SerializeField] WeaponBase weaponBasePrefab;
    [SerializeField] Tool toolPrefab;
    [SerializeField] Box boxPrefab;
    [SerializeField] Material highlightMaterial;
    [SerializeField] DropItemPool dropItemPool;
    [SerializeField] Material baseMaterial;
    public List<Item> Items { get; set; }
    MapManager mapManager;
    public MainManager MainManager { get; private set; }
    int[][] itemsNum;
    Sprite[][] itemIcons;
    Material[][] itemMaterials;
    ItemList[] itemLists;
    public void Init(MainManager mainManager)
    {
        itemLists = new ItemList[(int)ItemCategory.Length];
        itemLists[(int)ItemCategory.NatureBlock] = new ItemList { Items = natureBlockDataSO.ItemDatas };
        itemLists[(int)ItemCategory.UnnatureBlock] = new ItemList { Items = unnaturalBlockDataSO.ItemDatas };
        itemLists[(int)ItemCategory.BreakTool] = new ItemList { Items = breakToolDataSO.ItemDatas };
        itemLists[(int)ItemCategory.Weapon] = new ItemList { Items = weaponDataSO.ItemDatas };
        itemLists[(int)ItemCategory.Status] = new ItemList { Items = statusDataSO.ItemDatas };
        itemLists[(int)ItemCategory.Food] = new ItemList { Items = foodDataSO.ItemDatas };
        itemLists[(int)ItemCategory.Seed] = new ItemList { Items = seedDataSO.ItemDatas };
        itemLists[(int)ItemCategory.Fertilizer] = new ItemList { Items = fertilizerDataSO.ItemDatas };
        itemLists[(int)ItemCategory.Material] = new ItemList { Items = materialDataSO.ItemDatas };
        itemLists[(int)ItemCategory.Bag] = new ItemList { Items = bagDataSO.ItemDatas };
        itemLists[(int)ItemCategory.Tool] = new ItemList { Items = toolDataSO.ItemDatas };
        itemLists[(int)ItemCategory.Box] = new ItemList { Items = boxDataSO.ItemDatas };
        itemLists[(int)ItemCategory.WeaponBase] = new ItemList { Items = weaponBaseDataSO.ItemDatas };
        Items = new List<Item>();
        MainManager = mainManager;
        mapManager = mainManager.MapManager;
        if (itemsNum == null)
        {
            itemsNum = new int[itemLists.Length][];
            for (int i = 0; i < itemLists.Length; i++)
            {
                itemsNum[i] = new int[itemLists[i].Items.Length];
            }
        }
        SetSprite();
        SetMaterial();
        dropItemPool.Init(this);
    }
    void SetSprite()
    {
        itemIcons = new Sprite[itemLists.Length][];
        for (int i = 0; i < itemLists.Length; i++)
        {
            itemIcons[i] = new Sprite[itemLists[i].Items.Length];
            for (int j = 0; j < itemLists[i].Items.Length; j++)
            {
                itemIcons[i][j] = itemLists[i].Items[j]?.Icon;
            }
        }
    }
    void SetMaterial()
    {
        itemMaterials = new Material[itemLists.Length][];
        for (int i = 0; i < itemLists.Length; i++)
        {
            itemMaterials[i] = new Material[itemLists[i].Items.Length];
            for (int j = 0; j < itemLists[i].Items.Length; j++)
            {
                if (itemLists[i].Items[j] == null) continue;
                Material material = new Material(baseMaterial);
                material.SetTexture("_BaseMap", itemLists[i].Items[j].Texture2D);
                itemMaterials[i][j] = material;
            }
        }
    }
    public Sprite GetSprite(ItemAccess itemAccess)
    {
        return itemIcons[(int)itemAccess.Category][itemAccess.Id];
    }
    public Material GetMaterial(ItemAccess itemAccess)
    {
        if (itemAccess.Id < 0) return null;
        return itemMaterials[(int)itemAccess.Category][itemAccess.Id];
    }
    public int GetItemNum(ItemCategory category)
    {
        if (itemsNum == null)
        {
            itemsNum = new int[itemLists.Length][];
            for (int i = 0; i < itemLists.Length; i++)
            {
                itemsNum[i] = new int[itemLists[i].Items.Length];
            }
        }
        return itemsNum[(int)category].Length;
    }
    public Item InstantiateItem(ItemAccess itemAccess, Vector3 pos)
    {
        var item = Instantiate(itemPrefab, pos, Quaternion.identity);
        var material = GetMaterial(itemAccess);
        item.Init(this, material, itemAccess);
        item.SetItem(false);
        return item;
    }
    public Item InstantiateBreakTool(ItemAccess itemAccess, Vector3 pos)
    {
        var item = Instantiate(breakToolPrefab, pos, Quaternion.identity);
        var material = GetMaterial(itemAccess);
        item.Init(this, material, itemAccess);
        item.SetItem(false);
        return item;
    }
    public Item GetPoolItem(ItemAccess itemAccess, int num, Vector3 pos)
    {
        var instantiateItem = dropItemPool.GetItem();
        var material = GetMaterial(itemAccess);
        instantiateItem.SetItemAccess(itemAccess, 1, pos, material);
        instantiateItem.SetItem(true);
        return instantiateItem;
    }
    public Block InstantiateBlock(ItemCategory category, int id, Vector3Int pos, Transform parent = null)
    {
        var itemAccess = itemLists[(int)category].Items[id].ItemAccess;
        var item = InstantiateBlock(itemAccess, pos, parent);
        return item;
    }
    public Block InstantiateBlock(ItemAccess blockAccess, Vector3Int pos, Transform parent = null)
    {
        Block item = null;
        if (blockAccess.Category == ItemCategory.NatureBlock || blockAccess.Category == ItemCategory.UnnatureBlock)
        {
            item = Instantiate(blockPrefab, pos, Quaternion.identity, parent);
        }
        else if (blockAccess.Category == ItemCategory.Seed)
        {
            item = Instantiate(seedPrefab, pos, Quaternion.identity, parent);
        }
        else if (blockAccess.Category == ItemCategory.Weapon)
        {
            item = Instantiate(weaponPrefab, pos, Quaternion.identity, parent);
        }
        else if (blockAccess.Category == ItemCategory.WeaponBase)
        {
            item = Instantiate(weaponBasePrefab, pos, Quaternion.identity, parent);
        }
        else if (blockAccess.Category == ItemCategory.Tool)
        {
            item = Instantiate(toolPrefab, pos, Quaternion.identity, parent);
        }
        else if (blockAccess.Category == ItemCategory.Box)
        {
            item = Instantiate(boxPrefab, pos, Quaternion.identity, parent);
        }
        var material = GetMaterial(blockAccess);
        item.Init(this, material, blockAccess);
        return item;
    }
    public void AddFieldItem(Item item)
    {
        Items.Add(item);
    }
    public void RemoveFieldItem(Item item)
    {
        Items.Remove(item);
    }
    public void DropItem(Block block, Vector3Int pos)
    {
        var item = block.GetDropItem();
        var dropPos = pos + Vector3Int.up;
        int num = 1;
        if (item.HasValue)
        {
            var dropItem = GetPoolItem(item.Value.ItemAccess, num, dropPos);
            dropItem.Drop();
        }
        var dropItem100 = block.DropItem100();
        if (dropItem100.Id != -1) GetPoolItem(dropItem100, num, dropPos).Drop();
    }
    public void BreakBlock(Vector3Int pos)
    {
        mapManager.MapUpdate(pos);
    }
    public void BoxAdd(ItemAccess[] itemNums)
    {
        for (int i = 0; i < itemNums.Length; i++)
        {
            if (itemNums[i].Id == -1) continue;
            itemsNum[(int)ItemCategory.Material][itemNums[i].Id] += itemNums[i].Num;
        }
    }
    public void RemoveBoxItem(List<ItemAccess> materials)
    {
        foreach (ItemAccess material in materials)
        {
            itemsNum[(int)ItemCategory.Material][material.Id] -= material.Num;
        }
    }
    public int[][] GetBoxItemNum()
    {
        return itemsNum;
    }
    public Sprite GetItemIcon(ItemAccess itemAccess)
    {
        return itemIcons[(int)itemAccess.Category][itemAccess.Id];
    }
    public List<ItemData> GetMakableItems(ItemCategory category)
    {
        ItemList itemList = itemLists[(int)category];
        var makableItems = new List<ItemData>();
        foreach (var itemData in itemList.Items)
        {
            if (itemData is not ItemData makableItem) continue;
            if (makableItem.ItemMaterials.Count == 0) continue;
            makableItems.Add(makableItem);
        }
        return makableItems;
    }
    public ItemDataBase GetItem(ItemAccess itemAccess)
    {
        return itemLists[(int)itemAccess.Category].Items[itemAccess.Id];
    }
    public ItemAccess CraftToWeapon(ItemAccess[] craftSlots, Vector2Int boardSize)
    {
        if (CountFilledSlots(craftSlots) < MinWeaponCraftMaterialCount)
        {
            return EmptyItemAccess();
        }
        ItemAccess exactRecipe = FindExactCraftItemRecipe(craftSlots, boardSize, ItemCategory.Weapon, WeaponRecipeSize, true, true);
        Debug.Log($"Exact weapon recipe result: Id={exactRecipe.Id}, Category={exactRecipe.Category}");
        if (exactRecipe.Id != -1) return exactRecipe;

        if (weaponDataSO.ItemDatas == null || weaponDataSO.ItemDatas.Length == 0)
        {
            return EmptyItemAccess();
        }

        foreach (WeaponData weaponData in weaponDataSO.ItemDatas)
        {
            Debug.Log($"Checking weapon recipe: {weaponData.Name}");
            if (weaponData == null) continue;

            ItemAccess fallback = weaponData.ItemAccess;
            fallback.Num = 1;
            return fallback;
        }

        return EmptyItemAccess();
    }

    ItemAccess FindExactCraftItemRecipe(ItemAccess[] craftSlots, Vector2Int boardSize, ItemCategory category, Vector2Int recipeSize, bool allowFlip, bool trimEmptyEdges)
    {
        ItemList itemList = itemLists[(int)category];
        if (itemList?.Items == null) return EmptyItemAccess();

        foreach (ItemDataBase itemData in itemList.Items)
        {
            if (itemData is not CraftItemData craftItemData) continue;
            if (craftItemData.RecipeSlots == null || craftItemData.RecipeSlots.Length == 0) continue;
            if (CountFilledSlots(craftItemData.RecipeSlots) == 0) continue;

            bool matches = trimEmptyEdges
                ? MatchesTrimmedRecipe(craftSlots, boardSize, craftItemData, recipeSize, false)
                : MatchesExactRecipe(craftSlots, boardSize, craftItemData.RecipeSlots, recipeSize, false);
            if (!matches && allowFlip)
            {
                matches = trimEmptyEdges
                    ? MatchesTrimmedRecipe(craftSlots, boardSize, craftItemData, recipeSize, true)
                    : MatchesExactRecipe(craftSlots, boardSize, craftItemData.RecipeSlots, recipeSize, true);
            }

            if (matches)
            {
                ItemAccess itemAccess = craftItemData.ItemAccess;
                itemAccess.Num = 1;
                return itemAccess;
            }
        }

        return EmptyItemAccess();
    }

    bool MatchesTrimmedRecipe(ItemAccess[] craftSlots, Vector2Int craftSize, CraftItemData recipeData, Vector2Int recipeSize, bool flipX)
    {
        if (!TryGetUsedBounds(craftSlots, craftSize, out RectInt craftBounds)) return false;

        RectInt recipeBounds;
        if (recipeData.HasRecipeBounds)
            recipeBounds = recipeData.RecipeBounds;
        else if (!TryGetUsedBounds(recipeData.RecipeSlots, recipeSize, out recipeBounds))
            return false;

        if (craftBounds.width != recipeBounds.width || craftBounds.height != recipeBounds.height) return false;

        for (int y = 0; y < craftBounds.height; y++)
        {
            for (int x = 0; x < craftBounds.width; x++)
            {
                int craftIndex = (craftBounds.y + y) * craftSize.x + craftBounds.x + x;
                int recipeX = flipX ? recipeBounds.xMax - 1 - x : recipeBounds.x + x;
                int recipeIndex = (recipeBounds.y + y) * recipeSize.x + recipeX;

                if (!SameRecipeCell(craftSlots[craftIndex], recipeData.RecipeSlots[recipeIndex]))
                    return false;
            }
        }

        return true;
    }

    bool MatchesExactRecipe(ItemAccess[] craftSlots, Vector2Int craftSize, ItemAccess[] recipeSlots, Vector2Int recipeSize, bool flipX)
    {
        if (craftSize != recipeSize) return false;

        int cellCount = craftSize.x * craftSize.y;
        if (craftSlots.Length < cellCount || recipeSlots.Length < cellCount) return false;

        for (int y = 0; y < craftSize.y; y++)
        {
            for (int x = 0; x < craftSize.x; x++)
            {
                int craftIndex = y * craftSize.x + x;
                int recipeX = flipX ? recipeSize.x - 1 - x : x;
                int recipeIndex = y * recipeSize.x + recipeX;

                if (!SameRecipeCell(craftSlots[craftIndex], recipeSlots[recipeIndex]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    bool TryGetUsedBounds(ItemAccess[] slots, Vector2Int size, out RectInt bounds)
    {
        int width = size.x;
        int height = size.y;
        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;
        int cellCount = Mathf.Min(slots.Length, width * height);

        for (int i = 0; i < cellCount; i++)
        {
            if (IsEmptySlot(slots[i])) continue;

            int x = i % width;
            int y = i / width;
            minX = Mathf.Min(minX, x);
            minY = Mathf.Min(minY, y);
            maxX = Mathf.Max(maxX, x);
            maxY = Mathf.Max(maxY, y);
        }

        if (maxX < 0)
        {
            bounds = new RectInt();
            return false;
        }

        bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        return true;
    }

    int CountFilledSlots(ItemAccess[] slots)
    {
        if (slots == null) return 0;

        int count = 0;
        foreach (ItemAccess slot in slots)
        {
            if (!IsEmptySlot(slot)) count++;
        }
        return count;
    }

    bool SameRecipeCell(ItemAccess a, ItemAccess b)
    {
        if (IsEmptySlot(a) && IsEmptySlot(b)) return true;
        if (IsEmptySlot(a) || IsEmptySlot(b)) return false;
        return a.Category == b.Category && a.Id == b.Id;
    }

    bool IsEmptySlot(ItemAccess slot)
    {
        return slot.Id < 0;
    }

    ItemAccess EmptyItemAccess()
    {
        return new ItemAccess { Id = -1 };
    }
    public ItemAccess CraftToItem(ItemAccess[] craftSlots, Vector2Int boardSize, ItemCategory category)
    {
        if (CountFilledSlots(craftSlots) == 0)
        {
            return EmptyItemAccess();
        }

        return FindExactCraftItemRecipe(craftSlots, boardSize, category, SmallRecipeSize, false, false);
    }
}
[System.Serializable]
public class ItemList
{
    public ItemDataBase[] Items;
}
