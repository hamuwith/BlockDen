using UnityEngine;
using System.Collections.Generic;
using static Item;

public class ItemManager : MonoBehaviour
{
    [SerializeField] ItemList[] itemLists;
    [SerializeField] Item itemPrefab;
    [SerializeField] Block blockPrefab;
    [SerializeField] Seed seedPrefab;
    [SerializeField] Weapon weaponPrefab;
    [SerializeField] BreakTool breakToolPrefab;
    [SerializeField] WeaponBase weaponBasePrefab;
    [SerializeField] Tool toolPrefab;
    [SerializeField] Material highlightMaterial;
    [SerializeField] DropItemPool dropItemPool;
    [SerializeField] Material baseMaterial;
    public List<Item> Items { get; set; }
    MapManager mapManager;
    public MainManager MainManager { get; private set; }
    int[][] itemsNum;
    Sprite[][] itemIcons;
    Material[][] itemMaterials;
    public void Init(MainManager mainManager)
    {
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
    public void RemoveBoxItem(ItemAccess[] materials)
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
    public ItemData[] GetMakableItems(ItemCategory category)
    {
        ItemList itemList = itemLists[(int)category];
        return itemList.Items;
    }
    public ItemData GetItem(ItemAccess itemAccess)
    {
        return itemLists[(int)itemAccess.Category].Items[itemAccess.Id];
    }
    public ItemAccess CraftToWeapon(ItemAccess[] craftSlots)
    {
        // var makableItems = GetMakableItems(ItemCategory.Weapon);
        var item = new ItemAccess
        {
            Category = ItemCategory.Weapon,
            Id = 0
        };
        return item;
    }
    public ItemAccess CraftToBreakTool(ItemAccess[] craftSlots)
    {
        var item = new ItemAccess
        {
            Category = ItemCategory.BreakTool,
            Id = 0
        };
        return item;
    }
}
[System.Serializable]
public class ItemList
{
    public ItemData[] Items;
}
