using UnityEngine;

public class Block : Item
{
    int currentLife;
    int currentHardness;
    private MaterialPropertyBlock materialBlock;
    readonly float HideRate = 0.35f;
    Color color;
    BlockData blockData;
    public BlockTypeEnum BlockType => blockData.BlockType;

    public enum BlockTypeEnum
    {
        Dirt,
        Stone,
        Wood,
        Water,
        Crops,
        Length,
    }
    /// <summary>
    /// ブロックの耐久値をリセットする
    /// </summary>
    public void ResetHardness()
    {
        currentHardness = blockData.Hardness;
    }
    public override void Init(ItemManager itemManager, Material material, ItemAccess itemAccess)
    {
        base.Init(itemManager, material, itemAccess);
        blockData = itemManager.GetItem(itemAccess) as BlockData;
        materialBlock = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(materialBlock);
        color = Color.white;
        currentLife = blockData.Life;
    }
    /// <summary>
    /// ブロックを壊す際の挙動を定義するメソッド
    /// </summary>
    /// <param name="power"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    public virtual bool Break(int power, Vector3Int pos)
    {
        while (true)
        {
            if (power < currentHardness)
            {
                currentHardness -= power;
                return false;
            }
            else
            {
                power -= currentHardness;
                currentHardness = blockData.Hardness;
                currentLife--;
                itemManager.DropItem(this, pos);
                if (currentLife == 0)
                {
                    itemManager.BreakBlock(pos);
                    return true;
                }
            }
        }
    }
    /// <summary>
    /// ブロックが壊れたときのアイテムドロップの挙動を定義するメソッド
    /// </summary>
    /// <returns></returns>
    public ItemPercent? GetDropItem()
    {
        if (blockData.ItemPercents == null)
        {
            return null;
        }
        var rand = Random.Range(0, 100);
        var sum = 0;
        foreach (var itemPercent in blockData.ItemPercents)
        {
            sum += itemPercent.Percent;
            if (rand < sum)
            {
                return itemPercent;
            }
        }
        return null;
    }
    /// <summary>
    /// ブロックが壊れたときの落とすブロックを定義するメソッド
    /// </summary>
    /// <returns></returns>
    public ItemAccess DropItem100()
    {
        return blockData.DropItem100;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Gaze"))
        {
            color.a = Mathf.Max(1f - HideRate * (Vector3.SqrMagnitude(transform.position - other.transform.position) - 1f), 0.1f);
            materialBlock.SetColor("_BaseColor", color);
            meshRenderer.SetPropertyBlock(materialBlock);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Gaze"))
        {
            materialBlock.SetColor("_BaseColor", Color.white);
            meshRenderer.SetPropertyBlock(materialBlock);
        }
    }
}
