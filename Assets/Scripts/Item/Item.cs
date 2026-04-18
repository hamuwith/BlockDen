using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using static Item;
using UnityEngine.Pool;

public class Item : MonoBehaviour
{
    protected ItemManager itemManager;
    BoxCollider boxCollider;
    protected MeshRenderer meshRenderer;
    CancellationTokenSource cancellationTokenSource;
    public int Num { get; set; }
    protected ItemAccess itemAccess;
    public ItemAccess ItemAccess => itemAccess;
    /// <summary>
    /// アイテムのカテゴリを表す列挙型
    /// </summary>
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
        Length,
    }
    /// <summary>
    /// アイテムのドロップ率を表す構造体
    /// </summary>
    [System.Serializable]
    public struct ItemPercent
    {
        public ItemAccess ItemAccess;
        public int Percent;
    }
    private ObjectPool<Item> pool;
    /// <summary>
    /// ObjectPoolをセットします。
    /// </summary>
    /// <param name="pool"></param>
    public void SetPool(ObjectPool<Item> pool)
    {
        this.pool = pool;
    }
    public void Release()
    {
        pool.Release(this);
    }
    /// <summary>
    /// アイテムの初期化を行う
    /// </summary>
    /// <param name="itemManager"></param>
    public virtual void Init(ItemManager itemManager, Material material, ItemAccess itemAccess)
    {
        this.itemManager = itemManager;
        this.itemAccess = itemAccess;
        boxCollider = GetComponent<BoxCollider>();
        Num = 1;
        meshRenderer = GetComponent<MeshRenderer>();
        if (material == null) Destroy(meshRenderer);
        else meshRenderer.sharedMaterial = material;
    }
    /// <summary>
    /// アイテムマネージャーをセットするメソッド
    /// </summary>
    /// <param name="itemManager"></param>
    public void SetItemManager(ItemManager itemManager)
    {
        this.itemManager = itemManager;
        meshRenderer = GetComponent<MeshRenderer>();
        boxCollider = GetComponent<BoxCollider>();
    }
    /// <summary>
    /// アイテムの状態をセットするメソッド
    /// </summary>
    /// <param name="itemState"></param>
    /// <param name="num"></param>
    public void SetItemAccess(ItemAccess itemAccess, int num, Vector3 vector3, Material material)
    {
        this.itemAccess = itemAccess;
        this.itemAccess.Num = num;
        meshRenderer.sharedMaterial = material;
        transform.position = vector3;
    }
    /// <summary>
    /// アイテムをドロップする際の挙動を定義するメソッド
    /// </summary>
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
        var rigidbody = GetComponent<Rigidbody>();
        rigidbody.isKinematic = false;
        rigidbody.AddForce(angle * 2f, ForceMode.Impulse);
    }

    /// <summary>
    /// アイテムをセットする際の挙動を定義するメソッド
    /// </summary>
    /// <param name="isKinematic"></param>
    public virtual void SetItem(bool isHit = false)
    {
        boxCollider.enabled = isHit;
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
/// アイテムの素材を表す構造体
/// </summary>
[System.Serializable]
public struct ItemAccess
{
    public ItemCategory Category;
    public int Id;
    public int Num;
}
