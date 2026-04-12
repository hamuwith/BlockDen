using UnityEngine;
using UnityEngine.Pool;

public class EnemyPool : MonoBehaviour
{
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private int defaultCapacity;
    [SerializeField] private int maxSize;

    private ObjectPool<Enemy> pool;
    EnemyManager enemyManager;

    public void Init(EnemyManager enemyManager)
    {
        this.enemyManager = enemyManager;
        pool = new ObjectPool<Enemy>(
            createFunc: CreateEnemy,
            actionOnGet: OnGetEnemy,
            actionOnRelease: OnReleaseEnemy,
            actionOnDestroy: OnDestroyEnemy,
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }

    private Enemy CreateEnemy()
    {
        Enemy enemy = Instantiate(enemyPrefab, transform);
        enemy.SetPool(pool);
        return enemy;
    }

    private void OnGetEnemy(Enemy enemy)
    {
        enemy.gameObject.SetActive(true);
        enemyManager.AddEnemy(enemy);
    }

    private void OnReleaseEnemy(Enemy enemy)
    {
        enemy.gameObject.SetActive(false);
        enemyManager.RemoveEnemy(enemy);
    }

    private void OnDestroyEnemy(Enemy enemy)
    {
        Destroy(enemy.gameObject);
    }

    public Enemy GetEnemy()
    {
        return pool.Get();
    }
}

