using UnityEngine;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;
using System.Threading;

public class Arrow : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] float height;
    [SerializeField] AnimationCurve heightCurve;
    private ObjectPool<Arrow> pool;
    float distance;
    Vector3 root;
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
        distance = Vector3.Distance(weapon.transform.position, target.transform.position);
        Attack(target, damage, attachment, cancellationTokenSource.Token).Forget();
    }
    private async UniTaskVoid Attack(Enemy target, int damage, Attachment attachment, CancellationToken cancellationToken)
    {
        Vector3 direction = Vector3.zero;
        root = transform.position;
        while (true)
        {
            direction = (target.transform.position - root).normalized;
            transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, direction);
            if (target.IsDead())
            {
                break;
            }
            root += direction * speed * Time.deltaTime;
            var y = heightCurve.Evaluate((root - target.transform.position).magnitude / distance);
            transform.position = new Vector3(root.x, root.y + y * height * distance, root.z);
            if (target == null)
            {
                break;
            }
            if ((root - target.transform.position).sqrMagnitude < 0.25f)
            {
                target.TakeDamage(damage, false, attachment);
                break;
            }
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
        pool.Release(this);
    }
    private void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
}
