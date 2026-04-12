using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

public class Arrow : MonoBehaviour
{
    [SerializeField] float speed;
    private ObjectPool<Arrow> pool;
    CancellationTokenSource cancellationTokenSource;
    /// <summary>
    /// ObjectPoolをセットします。
    /// </summary>
    /// <param name="pool"></param>
    public void SetPool(ObjectPool<Arrow> pool)
    {
        this.pool = pool;
    }
    /// <summary>
    /// Enemyに向かって矢を発射します。
    /// </summary>
    /// <param name="target"></param>
    /// <param name="damage"></param>
    /// <param name="attachment"></param>
    public void Fire(Enemy target, int damage, Attachment attachment, Transform weapon)
    {
        cancellationTokenSource = new CancellationTokenSource();
        transform.position = weapon.position;
        Attack(target, damage, attachment, cancellationTokenSource.Token).Forget();
    }
    private async UniTaskVoid Attack(Enemy target, int damage, Attachment attachment, CancellationToken cancellationToken)
    {
        Vector3 direction = (target.transform.position - transform.position).normalized;
        while (true)
        {
            if(target.IsDead())
            {
                pool.Release(this);
                return;
            }
            transform.position += direction * speed * Time.deltaTime;
            if(target == null)
            {
                pool.Release(this);
                return;
            }
            if ((transform.position - target.transform.position).sqrMagnitude < 0.25f)
            {
                target.TakeDamage(damage, false, attachment);
                pool.Release(this);
                return;
            }
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }
    private void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
}
