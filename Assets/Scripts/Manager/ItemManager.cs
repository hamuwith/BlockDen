using UnityEngine;
using System.Collections.Generic;
using static Item;

public class ItemManager : MonoBehaviour
{
    [SerializeField] Item[] startItems;
    [SerializeField] ItemList[] itemLists;
    [SerializeField] Material highlightMaterial;
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
    public Item[] InstantiateFirstItems(Vector3 pos, Transform parent = null)
    {
        var items = new Item[startItems.Length];
        for (int i = 0; i < startItems.Length; i++)
        {
            items[i] = InstantiateItem(startItems[i], pos, parent);
        }
        return items;
    }
    public Item InstantiateFirstBag(Vector3 pos, Transform parent = null)
    {
        var bag = InstantiateItem(itemLists[(int)ItemCategory.Bag].Items[0], pos, parent);
        return bag;
    }
    /// <summary>
    /// アイテムIDからアイテムを生成するメソッド
    /// </summary>
    /// <param name="id"></param>
    /// <param name="pos"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public Item InstantiateItem(ItemCategory category, int id, Vector3 pos, Transform parent = null)
    {
       var  item = InstantiateItem(itemLists[(int)category].Items[id], pos, parent);
        return item;
    }
    /// <summary>
    /// アイテムを生成するメソッド
    /// </summary>
    /// <param name="item"></param>
    /// <param name="pos"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public Item InstantiateItem(Item item, Vector3 pos, Transform parent = null)
    {
        var instantiateItem = Instantiate(item, pos, Quaternion.identity, parent);
        instantiateItem.Init(this);
        var isKinematic = parent != null;
        instantiateItem.SetItem(isKinematic);
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
        item.SetBlock(pos);
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
        if (item.HasValue)
        {
            var dropItem = InstantiateItem(item.Value.Category, item.Value.Id, dropPos);
            dropItem.Drop();
        }
        var dropItem100 = block.DropItem100();
        if(dropItem100 != null) InstantiateItem(dropItem100, dropPos).Drop();
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
