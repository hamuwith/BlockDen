using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.Splines;
using System.Net.Mail;

public class Enemy : Character
{
    [SerializeField] int hp;
    [SerializeField] int damage;
    [SerializeField] int armor;
    [SerializeField] EnemyType enemyType;
    private ObjectPool<Enemy> pool;
    CancellationTokenSource cancellationTokenSource;
    CancellationTokenSource cancellationTokenSourceIce;
    CancellationTokenSource cancellationTokenSourceShining;
    CancellationTokenSource cancellationTokenSourcePoison;
    CancellationTokenSource cancellationTokenSourceDark;
    bool isAttacking;
    EnemyManager enemyManager;
    bool isArmorBroken;
    float moveMultiplier;
    public enum EnemyType
    {
        Fish,
        Fly,
        Walker,
    }
    /// <summary>
    /// ObjectPoolをセットします。
    /// </summary>
    /// <param name="pool"></param>
    public void SetPool(ObjectPool<Enemy> pool)
    {
        this.pool = pool;
    }
    /// <summary>
    /// Enemyを初期化します。
    /// </summary>
    /// <param name="enemyManager"></param>
    /// <param name="splineContainer"></param>
    public void Init(EnemyManager enemyManager, Spline spline, float length)
    {
        base.Init(enemyManager.MainManager);
        cancellationTokenSource = new CancellationTokenSource();
        this.enemyManager = enemyManager;
        isAttacking = false;
        isArmorBroken = false;
        moveMultiplier = 1f;
        Move(spline, length, cancellationTokenSource.Token).Forget();
    }
    async UniTaskVoid Move(Spline spline, float length, CancellationToken cancellationToken)
    {
        float t = 0f;
        while (t < 1f)
        {
            if (!isAttacking)
            {
                transform.position = spline.EvaluatePosition(t);
                t += moveSpeed * Time.deltaTime / length * moveMultiplier;
            }
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }
    /// <summary>
    /// ダメージを受けるときの処理を行います。
    /// </summary>
    /// <param name="damage"></param>
    /// <param name="attachments"></param>
    public void TakeDamage(int damage, bool trueDamage = false, AttachmentStatus? attachments = null)
    {
        damage = attachments?.Strong > 0 ? (int)(damage * enemyManager.StrongEffectMultiplier) : damage;
        if (isArmorBroken)
        {
            hp -= damage;
        }
        else
        {
            hp -= Mathf.Max(damage - armor, 1);
        }
        if (hp <= 0)
        {
            Died();
        }
        else if(attachments.HasValue)
        {
            if (attachments.Value.Ice > 0)
            {
                Ice();
            }
            else if (attachments.Value.Lightning > 0)
            {
                // 雷の効果を処理
            }
            else if (attachments.Value.Shining > 0)
            {
                Shining();
            }
            if (attachments.Value.Poison > 0)
            {
                Poison();
            }
            else if (attachments.Value.Dark > 0)
            {
                Dark();
            }
        }
    }
    public bool IsDead()
    {
        return hp <= 0;
    }
    private void Shining()
    {
        var duration = enemyManager.ShiningEffectDuration;
        var slowMultiplier = 0f;
        cancellationTokenSourceShining = new CancellationTokenSource();
        IceEffect(duration, slowMultiplier, cancellationTokenSourceShining.Token).Forget();
    }
    private void Ice()
    {
        var duration = enemyManager.IceEffectDuration;
        var slowMultiplier = enemyManager.IceEffectSlowMultiplier;
        cancellationTokenSourceIce = new CancellationTokenSource();
        IceEffect(duration, slowMultiplier, cancellationTokenSourceIce.Token).Forget();
    }
    async UniTaskVoid IceEffect(float duration, float slowMultiplier, CancellationToken cancellationToken)
    {
        moveMultiplier = slowMultiplier;
        await UniTask.Delay((int)(duration * 1000), cancellationToken: cancellationToken);
        moveMultiplier = 1f;
    }
    private void Poison()
    {
        var damage = enemyManager.PoisonEffectDamage;
        var duration = enemyManager.PoisonEffectDuration;
        var interval = enemyManager.PoisonEffectInterval;
        cancellationTokenSourcePoison = new CancellationTokenSource();
        PoisonEffect(damage, duration, interval, cancellationTokenSourcePoison.Token).Forget();
    }
    async UniTaskVoid PoisonEffect(int damage, float duration, float interval, CancellationToken cancellationToken)
    {
        float t = 0f;
        while (t < duration)
        {
            await UniTask.Delay((int)(interval * 1000), cancellationToken: cancellationToken);
            TakeDamage(damage, true);
            t += interval;
        }
    }
    private void Dark()
    {
        var duration = enemyManager.DarkEffectDuration;
        cancellationTokenSourceDark = new CancellationTokenSource();
        DarkEffect(duration, cancellationTokenSourceDark.Token).Forget();
    }
    async UniTaskVoid DarkEffect(float duration, CancellationToken cancellationToken)
    {
        isArmorBroken = true;
        await UniTask.Delay((int)(duration * 1000), cancellationToken: cancellationToken);
        isArmorBroken = false;
    }
    void Died()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSourcePoison?.Cancel();
        cancellationTokenSourcePoison?.Dispose();
        cancellationTokenSourceDark?.Cancel();
        cancellationTokenSourceDark?.Dispose();
        cancellationTokenSourceIce?.Cancel();
        cancellationTokenSourceIce?.Dispose();
        cancellationTokenSourceShining?.Cancel();
        cancellationTokenSourceShining?.Dispose();
        pool.Release(this);
    }
    private void OnDestroy()
    {
        if(IsDead()) return;
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSourcePoison?.Cancel();
        cancellationTokenSourcePoison?.Dispose();
        cancellationTokenSourceDark?.Cancel();
        cancellationTokenSourceDark?.Dispose();
        cancellationTokenSourceIce?.Cancel();
        cancellationTokenSourceIce?.Dispose();
        cancellationTokenSourceShining?.Cancel();
        cancellationTokenSourceShining?.Dispose();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("House"))
        {
            isAttacking = true;
            animator.SetBool("Attack", true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("House"))
        {
            isAttacking = false;
            animator.SetBool("Attack", false);
        }
    }
    /// <summary>
    /// Houseに攻撃を行います。
    /// </summary>
    public void AttackHouse()
    {
        mapManager.TakeHouseDamage(damage);
    }
}
