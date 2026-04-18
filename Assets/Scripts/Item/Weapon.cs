using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

public class Weapon : PlayerUI
{
    [SerializeField] AttachUI attachUI;
    AttachmentStatus attachment;
    CancellationTokenSource cancellationTokenSource;
    ArrowPool arrowPool;
    EnemyManager enemyManager;
    WeaponData weaponData;
    public AttachmentStatus Attachment { get; set; }
    public override void Init(ItemManager itemManager, Material material, ItemAccess itemAccess)
    {
        Init(itemManager, material, itemAccess);
        cancellationTokenSource = new CancellationTokenSource();
        weaponData = itemManager.GetItem(itemAccess) as WeaponData;
        arrowPool = itemManager.MainManager.WeaponManager.ArrowPool[weaponData.ArrowId];
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
            await UniTask.Delay(weaponData.AttackSpeed, cancellationToken: cancellationToken);
            Enemy enemy = null;
            while (enemy == null)
            {
                await UniTask.Delay(100, cancellationToken: cancellationToken);
                enemy = enemyManager.NearestEnemy(transform.position);
            }
            if (enemy != null)
            {
                var arrow = arrowPool.GetArrow();
                arrow.Fire(enemy, weaponData.Damage, Attachment, transform);
            }
        }
    }
    private void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
}
