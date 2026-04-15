using UnityEngine;
using System.Collections.Generic;
using static Item;
using static UnityEngine.Splines.SplineInstantiate;
using static UnityEditor.Progress;

public class ItemManager : MonoBehaviour
{
    [SerializeField] Item[] startItems;
    [SerializeField] ItemList[] itemLists;
    [SerializeField] Material highlightMaterial;
    [SerializeField] DropItemPool dropItemPool;
    public List<Item> Items { get; set; }
    public Material HighlightMaterial => highlightMaterial;
    MapManager mapManager;
    public MainManager MainManager { get; private set; }
    int[][] itemsNum;
    /// <summary>
    /// アイテムマネージャーの初期化を行う
    /// </summary>
    /// <param name="mainManager"></param>
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
        dropItemPool.Init(this);
    }
    public int GetItemNum(ItemCategory category)
    {
        if(itemsNum == null)
        {
            itemsNum = new int[itemLists.Length][];
            for (int i = 0; i < itemLists.Length; i++)
            {
                itemsNum[i] = new int[itemLists[i].Items.Length];
            }
        }
        return itemsNum[(int)category].Length;
    }
    /// <summary>
    /// 最初のアイテムを生成するメソッド
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public Item[] InstantiateFirstItems(Vector3 pos)
    {
        var items = new Item[startItems.Length];
        for (int i = 0; i < startItems.Length; i++)
        {
            items[i] = Instantiate(startItems[i], pos, Quaternion.identity);
            items[i].Init(this);
            items[i].SetItem(false);
        }
        return items;
    }
    public Item InstantiateBag(Vector3 pos)
    {
        var bag = Instantiate(itemLists[(int)ItemCategory.Bag].Items[0], pos, Quaternion.identity);
        bag.Init(this);
        bag.SetItem(false);
        return bag;
    }
    public Item InstantiateItem(ItemState itemState, Vector3 pos)
    {
        var item = Instantiate(itemLists[(int)itemState.ItemType].Items[itemState.Id], pos, Quaternion.identity);
        item.Init(this);
        item.SetItem(false);
        return item;
    }
    /// <summary>
    /// アイテムIDからアイテムを生成するメソッド
    /// </summary>
    public Item GetPoolItem(ItemState itemState, int num, Vector3 pos)
    {
        var instantiateItem = dropItemPool.GetItem();
        instantiateItem.SetItemState(itemState, 1, pos);
        instantiateItem.SetItem(true);
        return instantiateItem;
    }
    /// <summary>
    /// アイテムを生成するメソッド
    /// </summary>
    /// <returns></returns>
    public Item GetPoolItem(Item item, int num, Vector3 pos)
    {
        var instantiateItem = GetPoolItem(item.ItemState, num, pos);
        return instantiateItem;
    }
    /// <summary>
    /// アイテムを生成するメソッド
    /// </summary>
    /// <returns></returns>
    public Item GetPoolItem(ItemCategory itemCategory, int id, int num, Vector3 pos)
    {
        var instantiateItem = GetPoolItem(itemLists[(int)itemCategory].Items[id], num, pos);
        return instantiateItem;
    }
    /// <summary>
    /// アイテムIDからブロックを生成するメソッド
    /// </summary>
    /// <param name="id"></param>
    /// <param name="pos"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public Block InstantiateBlock(ItemCategory category, int id, Vector3Int pos, Transform parent = null)
    {
        var item = InstantiateBlock(itemLists[(int)category].Items[id] as Block, pos, parent);
        return item;
    }    /// <summary>
         /// アイテムIDからブロックを生成するメソッド
         /// </summary>
         /// <param name="id"></param>
         /// <param name="pos"></param>
         /// <param name="parent"></param>
         /// <returns></returns>
    public Block InstantiateBlock(Block block, Vector3Int pos, Transform parent = null)
    {
        var item = Instantiate(block, pos, Quaternion.identity, parent);
        item.Init(this);
        return item;
    }
    /// <summary>
    /// フィールド上のアイテムを管理するためのメソッド（追加）
    /// </summary>
    /// <param name="item"></param>
    public void AddFieldItem(Item item)
    {
        Items.Add(item);
    }
    /// <summary>
    /// フィールド上のアイテムを管理するためのメソッド（削除）
    /// </summary>
    /// <param name="item"></param>
    public void RemoveFieldItem(Item item)
    {
        Items.Remove(item);
    }
    /// <summary>
    /// ブロックが壊れたときのアイテムドロップの挙動を定義するメソッド
    /// </summary>
    /// <param name="block"></param>
    /// <param name="pos"></param>
    public void DropItem(Block block, Vector3Int pos)
    {
        var item = block.GetDropItem();
        var dropPos = pos + Vector3Int.up;
        int num = 1;
        if (item.HasValue)
        {
            var dropItem = GetPoolItem(item.Value.Category, item.Value.Id, num, dropPos);
            dropItem.Drop();
        }
        var dropItem100 = block.DropItem100();
        if(dropItem100 != null) GetPoolItem(dropItem100, num, dropPos).Drop();
    }
    /// <summary>
    /// ブロックが壊れたときのマップの更新を行うメソッド
    /// </summary>
    /// <param name="pos"></param>
    public void BreakBlock(Vector3Int pos)
    {
        mapManager.MapUpdate(pos, ItemCategory.Length, -1);
    }

    /// <summary>
    /// ボックスのアイテム数を更新するメソッド（追加）
    /// </summary>
    /// <param name="items"></param>
    public void BoxAdd(int[] itemNums)
    {
        for (int i = 0; i < itemNums.Length; i++)
        {
            itemsNum[(int)ItemCategory.Material][i] += itemNums[i];
        }
    }
    /// <summary>
    /// ボックスのアイテム数を更新するメソッド（削除）
    /// </summary>
    /// <param name="materials"></param>
    public void RemoveBoxItem(ItemMaterial[] materials)
    {
        foreach (ItemMaterial material in materials)
        {
            itemsNum[(int)ItemCategory.Material][material.Id] -= material.Num;
        }
    }
    public int[][] GetBoxItemNum()
    {
        return itemsNum;
    }
    public Sprite GetItemIcon(ItemCategory category, int id)
    {
        return itemLists[(int)category].Items[id].ItemState.Icon;
    }
    public Item[] GetMakableItems(ItemCategory category)
    {
        ItemList itemList = itemLists[(int)category];
        return itemList.Items;
    }
    public Item GetItem(ItemCategory category, int id)
    {
        return itemLists[(int)category].Items[id];
    }
}
[System.Serializable]
public class ItemList
{
    public Item[] Items;
}
