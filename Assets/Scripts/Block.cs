using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

public class Block : Item
{
    [SerializeField] int hardness;
    [SerializeField] ItemPercent[] itemPercents;
    [SerializeField] Item dropItem100;
    [SerializeField] int life;
    [SerializeField] BlockTypeEnum blockType;
    int currentHardness;
    Material[] highlightMaterials;
    Material[] normalmaterials;
    MeshRenderer meshRenderer;
    private MaterialPropertyBlock materialBlock;
    public BlockTypeEnum BlockType => blockType;
    readonly Color color = new Color(1, 1, 1, 0.1f);
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
        currentHardness = hardness;
    }
    public override void Init(ItemManager itemManager)
    {
        base.Init(itemManager);
        meshRenderer = GetComponent<MeshRenderer>();
        highlightMaterials = new Material[2];
        highlightMaterials[0] = meshRenderer.material;
        highlightMaterials[1] = itemManager.HighlightMaterial;
        normalmaterials = new Material[1];
        normalmaterials[0] = highlightMaterials[0];
        materialBlock = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(materialBlock);
    }
    /// <summary>
    /// ブロックを壊す際の挙動を定義するメソッド
    /// </summary>
    /// <param name="power"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool Break(int power, Vector3Int pos)
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
                currentHardness = hardness;
                life--;
                itemManager.DropItem(this, pos);
                if (life <= 0)
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
        if (itemPercents == null)
        {
            return null;
        }
        var rand = Random.Range(0, 100);
        var sum = 0;
        foreach (var itemPercent in itemPercents)
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
    /// ブロックを設置する際の挙動を定義するメソッド
    /// </summary>
    /// <param name="pos"></param>
    public virtual void SetBlock(Vector3Int pos)
    {
        transform.position = pos;
        GetComponent<Rigidbody>().isKinematic = true;
        gameObject.layer = LayerMask.NameToLayer("Block");
        transform.localScale = Vector3.one;
    }
    /// <summary>
    /// ブロックをアイテムとして設置する際の挙動を定義するメソッド
    /// </summary>
    /// <param name="isKinematic"></param>
    public override void SetItem(bool isKinematic = false)
    {
        base.SetItem(isKinematic);
        GetComponent<Rigidbody>().isKinematic = isKinematic;
        gameObject.layer = LayerMask.NameToLayer("Item");
        transform.localScale = Vector3.one * 0.3f;
    }
    /// <summary>
    /// ブロックが壊れたときの落とすブロックを定義するメソッド
    /// </summary>
    /// <returns></returns>
    public Item DropItem100()
    {
        return dropItem100;
    }
    /// <summary>
    /// ブロックをハイライトする際の挙動を定義するメソッド
    /// </summary>
    /// <param name="isHighlight"></param>
    public void Highlight(bool isHighlight)
    {
        if (isHighlight)
        {
            meshRenderer.sharedMaterials = highlightMaterials;
        }
        else
        {
            meshRenderer.sharedMaterials = normalmaterials;
        }
    }
    public override void Drop()
    {
        Highlight(false);
        base.Drop();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Gaze"))
        {            
            materialBlock.SetColor("_BaseColor", color);
            meshRenderer.SetPropertyBlock(materialBlock, 0);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Gaze"))
        {
            materialBlock.SetColor("_BaseColor", Color.white);
            meshRenderer.SetPropertyBlock(materialBlock, 0);
        }
    }
}
