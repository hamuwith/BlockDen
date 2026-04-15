using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.Pool;

public class DropItemPool : MonoBehaviour
{
    [SerializeField] private Item itemPrefab;
    [SerializeField] private int defaultCapacity;
    [SerializeField] private int maxSize;
    ItemManager itemManager;

    private ObjectPool<Item> pool;

    public void Init(ItemManager itemManager)
    {
        this.itemManager = itemManager;
        pool = new ObjectPool<Item>(
            createFunc: CreateItem,
            actionOnGet: OnGetItem,
            actionOnRelease: OnReleaseItem,
            actionOnDestroy: OnDestroyItem,
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }

    private Item CreateItem()
    {
        Item item = Instantiate(itemPrefab, transform);
        item.Init(itemManager);
        item.SetPool(pool);
        return item;
    }

    private void OnGetItem(Item item)
    {
        item.gameObject.SetActive(true);
    }

    private void OnReleaseItem(Item item)
    {
        item.gameObject.SetActive(false);
    }

    private void OnDestroyItem(Item item)
    {
        Destroy(item.gameObject);
    }

    public Item GetItem()
    {
        return pool.Get();
    }

    public void ReleaseItem(Item item)
    {
        pool.Release(item);
    }
}
