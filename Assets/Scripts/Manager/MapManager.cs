using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using static Item;

public class MapManager : MonoBehaviour
{
    public readonly static int Width = 40;
    public readonly static int Height = 15;
    public readonly static int Depth = 20;
    int[,,] maps = new int[Width, Height, Depth];
    Vector3 playerPos;
    [SerializeField] Player playerPrefab;
    [SerializeField] string mapName;
    [SerializeField] int hp;
    ItemManager itemManager;
    public Block[,,] Blocks { get; private set; }

    public void LoadFromResources(string mapName)
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>(mapName);
        if (jsonAsset == null)
        {
            Debug.LogError($"Map json not found in Resources/{mapName}.json");
            return;
        }

        MapSaveData data = JsonUtility.FromJson<MapSaveData>(jsonAsset.text);

        int index = 0;
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    maps[x, y, z] = data.blocks[index];
                    index++;
                }
            }
        }
    }
    public void Init(MainManager mainManager)
    {
        itemManager = mainManager.ItemManager;
        playerPos = new Vector3(10, 5, 4);
        string path = System.IO.Path.Combine(mainManager.ResourceFolder, mapName);
        LoadFromResources(path);
        var x = Width;
        var y = Height;
        var z = Depth;
        Blocks = new Block[x, y, z];
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y - 1; j++)
            {
                for (int k = 0; k < z; k++)
                {
                    if (maps[i, j, k] < 0) continue;
                    Blocks[i, j, k] = itemManager.InstantiateBlock(ItemCategory.NatureBlock, maps[i, j, k], new Vector3Int(i, j, k), transform);
                    if(maps[i, j + 1, k] >= 0)
                    {
                        Blocks[i, j, k].gameObject.SetActive(false);
                    }
                }
            }
        }
        var player = Instantiate(playerPrefab, playerPos, Quaternion.identity);
        player.Init(mainManager);
        mainManager.CameraManager.SetTargets(player.transform);
    }
    public bool IsHouse(Vector3Int pos)
    {
        if (pos.x < 0 || pos.x >= maps.GetLength(0) || pos.y < 0 || pos.y >= maps.GetLength(1) || pos.z < 0 || pos.z >= maps.GetLength(2))
        {
            return false;
        }
        bool isHouse = Blocks[pos.x, pos.y, pos.z]?.ItemState.ItemType == ItemCategory.NatureBlock;
        isHouse &= Blocks[pos.x, pos.y, pos.z]?.ItemState.Id == 0;
        return isHouse;
    }
    public bool IsTool(Vector3Int pos)
    {
        if (pos.x < 0 || pos.x >= maps.GetLength(0) || pos.y < 0 || pos.y >= maps.GetLength(1) || pos.z < 0 || pos.z >= maps.GetLength(2))
        {
            return false;
        }
        return Blocks[pos.x, pos.y, pos.z]?.ItemState.ItemType == ItemCategory.Weapon || IsHouse(pos)  || Blocks[pos.x, pos.y, pos.z]?.ItemState.ItemType == ItemCategory.Seed;
    }
    public bool IsBlock(Vector3Int pos)
    {
        if (pos.x < 0 || pos.x >= maps.GetLength(0) || pos.y < 0 || pos.y >= maps.GetLength(1) || pos.z < 0 || pos.z >= maps.GetLength(2))
        {
            return false;
        }
        return Blocks[pos.x, pos.y, pos.z]?.ItemState.ItemType == ItemCategory.NatureBlock || Blocks[pos.x, pos.y, pos.z]?.ItemState.ItemType == ItemCategory.UnnatureBlock || Blocks[pos.x, pos.y, pos.z]?.ItemState.ItemType == ItemCategory.Seed;
    }
    public bool IsSeed(Vector3Int pos)
    {
        if (pos.x < 0 || pos.x >= maps.GetLength(0) || pos.y < 0 || pos.y >= maps.GetLength(1) || pos.z < 0 || pos.z >= maps.GetLength(2))
        {
            return false;
        }
        return Blocks[pos.x, pos.y, pos.z]?.ItemState.ItemType == ItemCategory.Seed;
    }
    public Block GetBlock(Vector3Int pos)
    {
        return Blocks[pos.x, pos.y, pos.z];
    }
    public Item MapUpdate(Vector3Int pos, ItemCategory category, int id)
    {
        if (id < 0)
        {
            Destroy(Blocks[pos.x, pos.y, pos.z].gameObject);
            Blocks[pos.x, pos.y, pos.z] = null;
            if (pos.y > 0)
            {
                Vector3Int vector3Int = new Vector3Int(pos.x, pos.y - 1, pos.z);
                if (vector3Int.y >= 0) RemoveSetActive(vector3Int);
                vector3Int = new Vector3Int(pos.x - 1, pos.y, pos.z);
                if (vector3Int.x >= 0) RemoveSetActive(vector3Int);
                vector3Int = new Vector3Int(pos.x + 1, pos.y, pos.z);
                if (vector3Int.x < Width) RemoveSetActive(vector3Int);
                vector3Int = new Vector3Int(pos.x, pos.y, pos.z - 1);
                if (vector3Int.z >= 0) RemoveSetActive(vector3Int);
                vector3Int = new Vector3Int(pos.x, pos.y, pos.z + 1);
                if (vector3Int.z < Depth) RemoveSetActive(vector3Int);
            }
        }
        else
        {
            MapUpdate(pos, (Block)itemManager.GetItem(category, id));
            Vector3Int vector3Int = new Vector3Int(pos.x, pos.y - 1, pos.z);
            if(vector3Int.y >= 0) AddSetActive(vector3Int);
        }
        return Blocks[pos.x, pos.y, pos.z];
    }
    public Item MapUpdate(Vector3Int pos, Block block)
    {
        Blocks[pos.x, pos.y, pos.z] = itemManager.InstantiateBlock(block, new Vector3Int(pos.x, pos.y, pos.z), transform);
        return Blocks[pos.x, pos.y, pos.z];
    }
    public void TakeHouseDamage(int damage)
    {
        hp -= damage;
        if (hp <= 0)
        {
            Debug.Log("Game Over");
        }
    }
    private void RemoveSetActive(Vector3Int pos)
    {
        Block block = Blocks[pos.x, pos.y, pos.z];
        if (block != null && block.gameObject.activeSelf == false)
        {
            block.gameObject.SetActive(true);
        }
    }
    private void AddSetActive(Vector3Int pos)
    {
        Block block = Blocks[pos.x, pos.y, pos.z];
        bool isActive = false;
        if (pos.x > 0) isActive |= Blocks[pos.x - 1, pos.y, pos.z] == null;
        if (pos.x < Width - 1) isActive |= Blocks[pos.x + 1, pos.y, pos.z] == null;
        if (pos.z > 0) isActive |= Blocks[pos.x, pos.y, pos.z - 1] == null;
        if (pos.z < Depth - 1) isActive |= Blocks[pos.x, pos.y, pos.z + 1] == null;
        if (!isActive)
        {
            block.gameObject.SetActive(false);
        }
    }
}
