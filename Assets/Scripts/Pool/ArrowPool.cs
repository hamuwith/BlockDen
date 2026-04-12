using UnityEngine;
using UnityEngine.Pool;

public class ArrowPool : MonoBehaviour
{
    [SerializeField] private Arrow arrowPrefab;
    [SerializeField] private int defaultCapacity;
    [SerializeField] private int maxSize;

    private ObjectPool<Arrow> pool;

    public void Init()
    {
        pool = new ObjectPool<Arrow>(
            createFunc: CreateArrow,
            actionOnGet: OnGetArrow,
            actionOnRelease: OnReleaseArrow,
            actionOnDestroy: OnDestroyArrow,
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }

    private Arrow CreateArrow()
    {
        Arrow arrow = Instantiate(arrowPrefab, transform);
        arrow.SetPool(pool);
        return arrow;
    }

    private void OnGetArrow(Arrow arrow)
    {
        arrow.gameObject.SetActive(true);
    }

    private void OnReleaseArrow(Arrow arrow)
    {
        arrow.gameObject.SetActive(false);
    }

    private void OnDestroyArrow(Arrow arrow)
    {
        Destroy(arrow.gameObject);
    }

    public Arrow GetArrow()
    {
        return pool.Get();
    }

    public void ReleaseArrow(Arrow arrow)
    {
        pool.Release(arrow);
    }
}
