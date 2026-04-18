using UnityEngine;
using static Block;
using static Item;

[CreateAssetMenu(fileName = "BlockData", menuName = "Scriptable Objects/BlockData")]
public class BlockData : ItemData
{
    [SerializeField] int hardness;
    [SerializeField] ItemPercent[] itemPercents;
    [SerializeField] ItemAccess dropItem100;
    [SerializeField] int life;
    [SerializeField] BlockTypeEnum blockType;
    public int Hardness => hardness;
    public ItemPercent[] ItemPercents => itemPercents;
    public ItemAccess DropItem100 => dropItem100;
    public int Life => life;
    public BlockTypeEnum BlockType => blockType;
}
