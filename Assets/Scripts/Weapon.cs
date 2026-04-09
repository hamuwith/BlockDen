using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

public class Weapon : Tool
{
    [SerializeField] Status status;
    Attachment attachment;
    bool isAttack;
    CancellationTokenSource cancellationTokenSource;
    ArrowPool arrowPool;
    EnemyManager enemyManager;
    public List<Attachment> Attachments { get; set; }
    [System.Serializable]
    struct Status
    {
        public int damage;
        public int attackSpeed;
        public float range;
    }
    public override void Init(ItemManager itemManager)
    {
        base.Init(itemManager);
        isAttack = false;
        cancellationTokenSource = new CancellationTokenSource();
        arrowPool = itemManager.MainManager.WeaponManager.ArrowPool;
        enemyManager = itemManager.MainManager.EnemyManager;
        Attachments = new List<Attachment>();
        Attack(cancellationTokenSource.Token).Forget(); 
        foreach (var tool in tools)
        {
            tool.Init(itemManager, this);
        }
    }
    public override void SetBlock(Vector3Int pos)
    {
        base.SetBlock(pos);
        isAttack = true;
    }
    public override void SetItem(bool isKinematic = false)
    {
        base.SetItem(isKinematic);
        isAttack = false;
    }
    public override void OpenUI(Player player)
    {
        this.player = player;
        toolIndex = 1;
        for (int i = 0; i < tools.Length; i++)
        {
            tools[i].OpenUI(player, i == 2 ? player.BagIndex : 0);
        }
    }
    /// <summary>
    /// UIÇï¬Ç∂ÇÈÉÅÉ\ÉbÉh
    /// </summary>
    protected override void CloseUI()
    {
        base.CloseUI();
        attachment = gameObject.AddComponent<Attachment>();
        Attachment.SumAttachment(ref attachment, Attachments);
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
