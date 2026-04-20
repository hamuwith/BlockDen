using UnityEngine;
using static Block;
using static Item;

[CreateAssetMenu(fileName = "BlockDataSO", menuName = "Scriptable Objects/BlockDataSO")]
public class BlockDataSO : ScriptableObject
{
    [SerializeField] public TextAsset csvFile;
    [SerializeField] BlockData[] itemDatas;
    public BlockData[] ItemDatas => itemDatas;

#if UNITY_EDITOR
    public void SetItemDatas(BlockData[] datas)
    {
        itemDatas = datas;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

[System.Serializable]
public class BlockData : ItemDataBase
{
    public int Hardness;
    public ItemPercent[] ItemPercents;
    public ItemAccess DropItem100;
    public int Life;
    public BlockTypeEnum BlockType;
}
