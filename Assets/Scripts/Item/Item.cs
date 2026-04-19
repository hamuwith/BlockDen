using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using static Item;
using UnityEngine.Pool;

public class Item : MonoBehaviour
{
    protected ItemManager itemManager;
    BoxCollider boxCollider;
    Rigidbody rigid;
    protected MeshRenderer meshRenderer;
    CancellationTokenSource cancellationTokenSource;
    public int Num { get; set; }
    protected ItemAccess itemAccess;
    public ItemAccess ItemAccess => itemAccess;
    [System.Serializable]
    public enum ItemCategory
    {
        NatureBlock,
        UnnatureBlock,
        BreakTool,
        Weapon,
        Status,
        Food,
        Seed,
        Fertilizer,
        Material,
        Bag,
        Tool,
        WeaponBase,
        Length,
    }
    [System.Serializable]
    public struct ItemPercent
    {
        public ItemAccess ItemAccess;
        public int Percent;
    }
    private ObjectPool<Item> pool;
    public void SetPool(ObjectPool<Item> pool)
    {
        this.pool = pool;
    }
    public void Release()
    {
        pool.Release(this);
    }
    public virtual void Init(ItemManager itemManager, Material material, ItemAccess itemAccess)
    {
        this.itemManager = itemManager;
        this.itemAccess = itemAccess;
        boxCollider = GetComponent<BoxCollider>();
        Num = 1;
        meshRenderer = GetComponent<MeshRenderer>();
        rigid = GetComponent<Rigidbody>();
        if (material == null) Destroy(meshRenderer);
        else meshRenderer.sharedMaterial = material;
    }
    public void SetItemManager(ItemManager itemManager)
    {
        this.itemManager = itemManager;
        meshRenderer = GetComponent<MeshRenderer>();
        boxCollider = GetComponent<BoxCollider>();
        rigid = GetComponent<Rigidbody>();
    }
    public void SetItemAccess(ItemAccess itemAccess, int num, Vector3 vector3, Material material)
    {
        this.itemAccess = itemAccess;
        this.itemAccess.Num = num;
        meshRenderer.sharedMaterial = material;
        transform.position = vector3;
    }
    public void Drop()
    {
        Vector3 angle = new Vector3(Random.Range(-1f, 1f), 1, Random.Range(-1f, 1f));
        DropForce(angle);
    }
    public void PlayerDrop(Vector3 forward)
    {
        Vector3 angle = forward;
        DropForce(angle);
    }
    public void DropForce(Vector3 angle)
    {
        cancellationTokenSource = new CancellationTokenSource();
        CanGetItem(cancellationTokenSource.Token).Forget();
        rigid.isKinematic = false;
        rigid.AddForce(angle * 2f, ForceMode.Impulse);
    }
    public virtual void SetItem(bool isHit = false)
    {
        boxCollider.enabled = isHit;
        rigid.isKinematic = !isHit;
    }
    async UniTaskVoid CanGetItem(CancellationToken cancellationToken)
    {
        await UniTask.Delay(800, cancellationToken: cancellationToken);
        itemManager.AddFieldItem(this);
    }
    private void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
}
/// <summary>
/// �A�C�e���̑f�ނ�\���\����
/// </summary>
[System.Serializable]
public struct ItemAccess
{
    public ItemCategory Category;
    public int Id;
    public int Num;
}
