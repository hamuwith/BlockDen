using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using static Item;

public class Item : MonoBehaviour
{
    [SerializeField] ItemState itemState;
    protected ItemManager itemManager;
    BoxCollider boxCollider;
    CancellationTokenSource cancellationTokenSource;
    public int Num { get; set; }
    public ItemState ItemState => itemState;
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
        public ItemCategory Category;
        public int Id;
        public int Percent;
    }
    /// <summary>
    /// アイテムの素材を表す構造体
    /// </summary>
    [System.Serializable]
    public struct ItemMaterial
    {
        public ItemCategory Category;
        public int Id;
        public int Num;
    }
    /// <summary>
    /// アイテムの初期化を行う
    /// </summary>
    /// <param name="itemManager"></param>
    public virtual void Init(ItemManager itemManager)
    {
        this.itemManager = itemManager;
        boxCollider = GetComponent<BoxCollider>();
        Num = 1;
    }
    /// <summary>
    /// アイテムをドロップする際の挙動を定義するメソッド
    /// </summary>
    public virtual void Drop()
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
    public virtual void SetItem(bool isKinematic = false)
    {
        boxCollider.enabled = !isKinematic;
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
[System.Serializable]
public class ItemState
{
    public int Id;
    public ItemCategory ItemType;
    public Sprite Icon;
    public ItemMaterial[] Materials;
    public int UnitNum;
    public int MaxNum;
}
