using System;
using System.IO;
using UnityEngine;

public class MapDataHolder : MonoBehaviour
{
    public int Width => MapManager.Width;
    public int Height => MapManager.Height;
    public int Depth => MapManager.Depth;
    [SerializeField] public bool initialized;

    [Header("Block Prefabs")]
    public GameObject[] blockPrefabs;

    [HideInInspector]
    public int[,,] blockIds;
    public void InitializeBlockIds(int emptyValue = -1)
    {
        blockIds = new int[Width, Height, Depth];
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    blockIds[x, y, z] = emptyValue;
                }
            }
        }
        Debug.Log(Width);
    }
    public void SaveToJson(string path)
    {
        MapSaveData data = ToSaveData();
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }
    public void LoadFromJson(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"File not found: {path}");
            return;
        }

        string json = File.ReadAllText(path);
        MapSaveData data = JsonUtility.FromJson<MapSaveData>(json);
        LoadFromSaveData(data);
        RebuildMap();
    }
    public void EnsureInitialized()
    {
        if (initialized)
            return;

        InitializeBlockIds(-1);
        initialized = true;
    }

    public bool IsInRange(Vector3Int pos)
    {
        return pos.x >= 0 && pos.x < Width
            && pos.y >= 0 && pos.y < Height
            && pos.z >= 0 && pos.z < Depth;
    }

    public bool IsEmpty(Vector3Int pos)
    {
        return blockIds[pos.x, pos.y, pos.z] == -1;
    }

    public void SetBlockId(Vector3Int pos, int id)
    {
        blockIds[pos.x, pos.y, pos.z] = id;
    }

    public int GetBlockId(Vector3Int pos)
    {
        return blockIds[pos.x, pos.y, pos.z];
    }
    public void ClearPlacedBlocks()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            DestroyImmediate(child.gameObject);
        }
    }
    public MapSaveData ToSaveData()
    {
        MapSaveData data = new MapSaveData
        {
            width = Width,
            height = Height,
            depth = Depth,
            blocks = new int[Width * Height * Depth]
        };

        int index = 0;
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    data.blocks[index] = blockIds[x, y, z];
                    index++;
                }
            }
        }

        return data;
    }
    public void LoadFromSaveData(MapSaveData data)
    {
        if (data.width != Width || data.height != Height || data.depth != Depth)
        {
            UnityEngine.Debug.LogError("Map size does not match.");
            return;
        }

        int index = 0;
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    blockIds[x, y, z] = data.blocks[index];
                    index++;
                }
            }
        }
    }
    public void RebuildMap()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    int blockId = blockIds[x, y, z];
                    if (blockId < 0)
                        continue;

                    if (blockPrefabs == null || blockId >= blockPrefabs.Length || blockPrefabs[blockId] == null)
                        continue;

                    GameObject instance = UnityEditor.PrefabUtility.InstantiatePrefab(blockPrefabs[blockId], transform) as GameObject;
                    instance.transform.position = new Vector3(x, y, z);
                    instance.name = $"{blockPrefabs[blockId].name}_{x}_{y}_{z}";
                }
            }
        }
    }
}

[Serializable]
public class MapSaveData
{
    public int width;
    public int height;
    public int depth;
    public int[] blocks;
}
