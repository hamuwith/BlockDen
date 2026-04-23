using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapDataHolder : MonoBehaviour, ISerializationCallbackReceiver
{
    public int Width => MapManager.Width;
    public int Height => MapManager.Height;
    public int Depth => MapManager.Depth;

    [SerializeField] public bool initialized;

    [Header("Block Data")]
    [SerializeField] private Block blockBasePrefab;
    [SerializeField] private BlockDataSO blockDataSO;

    [SerializeField, HideInInspector] private int[] serializedBlockIds;

    [HideInInspector] public int[,,] blockIds;

    [NonSerialized] private Material[] previewMaterials;

    public bool UsesBlockData =>
        blockBasePrefab != null
        && HasBlockDefinitions;

    public int BlockOptionCount => HasBlockDefinitions ? blockDataSO.ItemDatas.Length : 0;

    public bool HasBlockDefinitions =>
        blockDataSO != null
        && blockDataSO.ItemDatas != null
        && blockDataSO.ItemDatas.Length > 0;

    public string[] GetBlockOptions()
    {
        int count = BlockOptionCount;
        string[] options = new string[count];

        for (int i = 0; i < count; i++)
        {
            options[i] = $"{i}: {GetBlockDisplayName(i)}";
        }

        return options;
    }

    public string GetBlockDisplayName(int id)
    {
        if (!IsValidBlockId(id))
        {
            return "None";
        }

        if (HasBlockDefinitions)
        {
            BlockData data = blockDataSO.ItemDatas[id];
            if (data == null)
            {
                return "None";
            }

            return string.IsNullOrWhiteSpace(data.Name)
                ? $"Block {id}"
                : data.Name;
        }

        return "None";
    }

    public void InitializeBlockIds(int emptyValue = -1)
    {
        CreateBlockIds(emptyValue);
        initialized = true;
        SyncSerializedBlockIds();
    }

    public void SaveToJson(string path)
    {
        EnsureInitialized();

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
#if UNITY_EDITOR
        TryAutoAssignEditorReferences();
#endif

        if (HasValidBlockArray())
        {
#if UNITY_EDITOR
            if (transform.childCount == 0 && HasAnyBlock())
            {
                RebuildMap();
            }
            else if (UsesBlockData && previewMaterials == null && transform.childCount > 0)
            {
                RefreshOrRebuildPlacedBlocks();
            }
#endif
            return;
        }

        if (!TryRestoreBlockIdsFromSerialized())
        {
            CreateBlockIds(-1);
            TryRestoreBlockIdsFromChildren();
            SyncSerializedBlockIds();
        }

        initialized = true;

#if UNITY_EDITOR
        RefreshOrRebuildPlacedBlocks();
#endif
    }

    public bool IsInRange(Vector3Int pos)
    {
        return pos.x >= 0 && pos.x < Width
            && pos.y >= 0 && pos.y < Height
            && pos.z >= 0 && pos.z < Depth;
    }

    public bool IsEmpty(Vector3Int pos)
    {
        EnsureInitialized();
        return blockIds[pos.x, pos.y, pos.z] == -1;
    }

    public void SetBlockId(Vector3Int pos, int id)
    {
        EnsureInitialized();
        blockIds[pos.x, pos.y, pos.z] = id;
        SyncSerializedBlockIds();
    }

    public int GetBlockId(Vector3Int pos)
    {
        EnsureInitialized();
        return blockIds[pos.x, pos.y, pos.z];
    }

    public bool CanInstantiateBlock(int blockId)
    {
        if (!IsValidBlockId(blockId))
        {
            return false;
        }

        return UsesBlockData && blockDataSO.ItemDatas[blockId] != null;
    }

    public void ClearPlacedBlocks()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            DestroyObjectImmediate(child.gameObject);
        }
    }

    public MapSaveData ToSaveData()
    {
        EnsureInitialized();

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
        if (data == null)
        {
            Debug.LogError("Map data is null.");
            return;
        }

        if (data.width != Width || data.height != Height || data.depth != Depth)
        {
            Debug.LogError("Map size does not match.");
            return;
        }

        if (data.blocks == null || data.blocks.Length != Width * Height * Depth)
        {
            Debug.LogError("Map block data is invalid.");
            return;
        }

        CreateBlockIds(-1);

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

        initialized = true;
        SyncSerializedBlockIds();
    }

    public void RebuildMap()
    {
        if (!HasValidBlockArray())
        {
            EnsureInitialized();
        }

        ClearPlacedBlocks();

#if UNITY_EDITOR
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    int blockId = blockIds[x, y, z];
                    if (blockId < 0 || !CanInstantiateBlock(blockId))
                    {
                        continue;
                    }

                    CreatePlacedBlock(blockId, new Vector3Int(x, y, z));
                }
            }
        }
#endif
    }

#if UNITY_EDITOR
    public GameObject CreatePlacedBlock(int blockId, Vector3Int gridPos)
    {
        if (!CanInstantiateBlock(blockId))
        {
            return null;
        }

        if (blockBasePrefab == null)
        {
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(blockBasePrefab.gameObject, transform) as GameObject;
        if (instance == null)
        {
            return null;
        }

        ConfigurePlacedBlock(instance, blockId, gridPos);
        return instance;
    }
#endif

    public void OnBeforeSerialize()
    {
        if (HasValidBlockArray())
        {
            SyncSerializedBlockIds();
        }
    }

    public void OnAfterDeserialize()
    {
        if (serializedBlockIds == null || serializedBlockIds.Length != Width * Height * Depth)
        {
            return;
        }

        CreateBlockIds(-1);

        int index = 0;
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    blockIds[x, y, z] = serializedBlockIds[index];
                    index++;
                }
            }
        }

        initialized = true;
    }

    private void OnDisable()
    {
        ReleasePreviewMaterials();
    }

    private void OnDestroy()
    {
        ReleasePreviewMaterials();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        TryAutoAssignEditorReferences();
    }
#endif

    private void CreateBlockIds(int fillValue)
    {
        blockIds = new int[Width, Height, Depth];

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    blockIds[x, y, z] = fillValue;
                }
            }
        }
    }

    private bool HasValidBlockArray()
    {
        return blockIds != null
            && blockIds.GetLength(0) == Width
            && blockIds.GetLength(1) == Height
            && blockIds.GetLength(2) == Depth;
    }

    private bool IsValidBlockId(int blockId)
    {
        return blockId >= 0 && blockId < BlockOptionCount;
    }

    private void SyncSerializedBlockIds()
    {
        if (!HasValidBlockArray())
        {
            serializedBlockIds = null;
            return;
        }

        int total = Width * Height * Depth;
        if (serializedBlockIds == null || serializedBlockIds.Length != total)
        {
            serializedBlockIds = new int[total];
        }

        int index = 0;
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    serializedBlockIds[index] = blockIds[x, y, z];
                    index++;
                }
            }
        }
    }

    private bool TryRestoreBlockIdsFromSerialized()
    {
        if (serializedBlockIds == null || serializedBlockIds.Length != Width * Height * Depth)
        {
            return false;
        }

        CreateBlockIds(-1);

        int index = 0;
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    blockIds[x, y, z] = serializedBlockIds[index];
                    index++;
                }
            }
        }

        return true;
    }

    private bool TryRestoreBlockIdsFromChildren()
    {
        bool restoredAny = false;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (!TryReadChildBlock(child, out Vector3Int gridPos, out int blockId))
            {
                continue;
            }

            if (!IsInRange(gridPos) || !IsValidBlockId(blockId))
            {
                continue;
            }

            blockIds[gridPos.x, gridPos.y, gridPos.z] = blockId;
            restoredAny = true;
        }

        return restoredAny;
    }

    private bool TryReadChildBlock(Transform child, out Vector3Int gridPos, out int blockId)
    {
        gridPos = Vector3Int.RoundToInt(child.position);
        blockId = -1;

        string[] parts = child.name.Split('_');
        if (parts.Length != 5 || parts[0] != "Block")
        {
            return false;
        }

        if (!int.TryParse(parts[1], out blockId)
            || !int.TryParse(parts[2], out int x)
            || !int.TryParse(parts[3], out int y)
            || !int.TryParse(parts[4], out int z))
        {
            return false;
        }

        gridPos = new Vector3Int(x, y, z);
        return true;
    }

#if UNITY_EDITOR
    private void RefreshOrRebuildPlacedBlocks()
    {
        if (!HasAnyBlock())
        {
            return;
        }

        if (transform.childCount == 0)
        {
            RebuildMap();
            return;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (!TryReadChildBlock(child, out Vector3Int gridPos, out int blockId))
            {
                continue;
            }

            if (!IsInRange(gridPos) || !CanInstantiateBlock(blockId))
            {
                continue;
            }

            ConfigurePlacedBlock(child.gameObject, blockId, gridPos);
        }
    }

    private void ConfigurePlacedBlock(GameObject instance, int blockId, Vector3Int gridPos)
    {
        instance.transform.SetParent(transform);
        instance.transform.position = new Vector3(gridPos.x, gridPos.y, gridPos.z);
        instance.transform.rotation = Quaternion.identity;
        instance.name = $"Block_{blockId}_{gridPos.x}_{gridPos.y}_{gridPos.z}";

        if (!UsesBlockData)
        {
            return;
        }

        MeshRenderer renderer = instance.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            return;
        }

        Material previewMaterial = GetPreviewMaterial(blockId);
        if (previewMaterial != null)
        {
            renderer.sharedMaterial = previewMaterial;
        }
    }

    private void TryAutoAssignEditorReferences()
    {
        bool updated = false;

        if (blockBasePrefab == null)
        {
            blockBasePrefab = FindPreferredBlockBasePrefab();
            updated |= blockBasePrefab != null;
        }

        if (blockDataSO == null)
        {
            blockDataSO = FindPreferredBlockData();
            updated |= blockDataSO != null;
        }

        if (updated)
        {
            EditorUtility.SetDirty(this);
        }
    }

    private Block FindPreferredBlockBasePrefab()
    {
        string[] preferredGuids = AssetDatabase.FindAssets("_0_0_house t:Prefab", new[] { "Assets/Prefabs" });
        for (int i = 0; i < preferredGuids.Length; i++)
        {
            string preferredPath = AssetDatabase.GUIDToAssetPath(preferredGuids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(preferredPath);
            if (prefab == null)
            {
                continue;
            }

            Block block = prefab.GetComponent<Block>();
            if (block != null)
            {
                return block;
            }
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                continue;
            }

            Block block = prefab.GetComponent<Block>();
            if (block != null)
            {
                return block;
            }
        }

        return null;
    }

    private BlockDataSO FindPreferredBlockData()
    {
        string[] preferredGuids = AssetDatabase.FindAssets("NatureBlockDataSO t:BlockDataSO");
        if (preferredGuids.Length > 0)
        {
            string preferredPath = AssetDatabase.GUIDToAssetPath(preferredGuids[0]);
            return AssetDatabase.LoadAssetAtPath<BlockDataSO>(preferredPath);
        }

        string[] guids = AssetDatabase.FindAssets("t:BlockDataSO");
        if (guids.Length == 0)
        {
            return null;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<BlockDataSO>(path);
    }
#endif

    private bool HasAnyBlock()
    {
        if (!HasValidBlockArray())
        {
            return false;
        }

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    if (blockIds[x, y, z] >= 0)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private Material GetPreviewMaterial(int blockId)
    {
        if (!UsesBlockData || !IsValidBlockId(blockId))
        {
            return null;
        }

        if (previewMaterials == null || previewMaterials.Length != blockDataSO.ItemDatas.Length)
        {
            ReleasePreviewMaterials();
            previewMaterials = new Material[blockDataSO.ItemDatas.Length];
        }

        if (previewMaterials[blockId] != null)
        {
            return previewMaterials[blockId];
        }

        MeshRenderer prefabRenderer = blockBasePrefab != null
            ? blockBasePrefab.GetComponent<MeshRenderer>()
            : null;

        if (prefabRenderer == null || prefabRenderer.sharedMaterial == null)
        {
            return null;
        }

        Material material = new Material(prefabRenderer.sharedMaterial);
        BlockData data = blockDataSO.ItemDatas[blockId];

#if UNITY_EDITOR
        material.hideFlags = HideFlags.HideAndDontSave;
#endif

        if (data != null
            && data.Texture2D != null
            && material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", data.Texture2D);
        }

        previewMaterials[blockId] = material;
        return material;
    }

    private void ReleasePreviewMaterials()
    {
        if (previewMaterials == null)
        {
            return;
        }

        for (int i = 0; i < previewMaterials.Length; i++)
        {
            if (previewMaterials[i] == null)
            {
                continue;
            }

            DestroyObjectImmediate(previewMaterials[i]);
            previewMaterials[i] = null;
        }

        previewMaterials = null;
    }

    private void DestroyObjectImmediate(UnityEngine.Object obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(obj);
            return;
        }

        DestroyImmediate(obj);
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
