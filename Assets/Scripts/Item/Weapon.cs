using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

public class Weapon : PlayerUI
{
    [SerializeField] Status status;
    [SerializeField] AttachUI attachUI;
    Attachment attachment;
    bool isAttack;
    CancellationTokenSource cancellationTokenSource;
    ArrowPool arrowPool;
    EnemyManager enemyManager;
    public Attachment Attachment { get; set; }
    [System.Serializable]
    struct Status
    {
        public int damage;
        public int attackSpeed;
        public float range;
    }
    public override void Init(ItemManager itemManager)
    {
        BaseInit(itemManager);
        isAttack = true;
        cancellationTokenSource = new CancellationTokenSource();
        arrowPool = itemManager.MainManager.WeaponManager.ArrowPool;
        enemyManager = itemManager.MainManager.EnemyManager;
        attachUI.Init(itemManager);
        Attack(cancellationTokenSource.Token).Forget();
    }
    public override void OpenUI(Player player)
    {
        this.player = player;
        attachUI.OpenUI(player);
    }
    public override void CloseUI()
    {
        attachUI.CloseUI();
        var attachedItem = attachUI.GetAttachedItem();
    }
    public override void Select(Vector2 vector)
    {
    }
    public override void Action()
    {
        attachUI.Action();
    }
    public override void Cancel()
    {
        var close = attachUI.Cancel();
        if (close)
        {
            CloseUI();
        }
    }
    public override void UpdateAction()
    {
        attachUI.UpdateAction();
    }
    private async UniTaskVoid Attack(CancellationToken cancellationToken)
    {
        while (true)
        {
            await UniTask.Delay(status.attackSpeed, cancellationToken: cancellationToken);
            await UniTask.WaitUntil(() => isAttack, cancellationToken: cancellationToken);
            Enemy enemy = null;
            while (enemy == null)
            {
                await UniTask.Delay(100, cancellationToken: cancellationToken);
                enemy = enemyManager.NearestEnemy(transform.position);
            }
            if (enemy != null)
            {
                var arrow = arrowPool.GetArrow();
                arrow.Fire(enemy, status.damage, attachment, transform);
            }
        }
    }
    private void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
}
