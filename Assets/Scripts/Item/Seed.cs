using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine;

public class Seed : PlayerUI
{
    [SerializeField] FertilizerUI fertilizerUI;
    int growCount;
    bool isWater;
    CancellationTokenSource cancellationTokenSource;
    readonly int WaterTime = 20000;
    async UniTaskVoid WaterWait(CancellationToken cancellationToken)
    {
        isWater = false;
        await UniTask.Delay(WaterTime, cancellationToken: cancellationToken);
        isWater = true;
    }
    private void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
    public override void Init(ItemManager itemManager, Material material, ItemAccess itemAccess)
    {
        base.Init(itemManager, material, itemAccess);
        fertilizerUI.Init(itemManager);
        growCount = 0;
        isWater = true;
        cancellationTokenSource = new CancellationTokenSource();
    }
    public override void OpenUI(Player player)
    {
        this.player = player;
        fertilizerUI.OpenUI(player);
    }
    public override void SelectTab(bool left)
    {
    }
    public override void CloseUI()
    {
        fertilizerUI.CloseUI();
    }
    public override void Select(Vector2 vector)
    {
    }
    public override void Action()
    {
        fertilizerUI.Action();
    }
    public override void Cancel()
    {
        var close = fertilizerUI.Cancel();
        if (close)
        {
            CloseUI();
        }
    }
    public override void UpdateAction()
    {
        fertilizerUI.UpdateAction();
    }
    public override bool Break(int power, Vector3Int pos)
    {
        if (power > 0 && isWater)
        {
            growCount++;
            var seedData = itemManager.GetItem(itemAccess) as SeedData;
            if (growCount >= seedData.GrowNum)
            {
                itemManager.BreakBlock(pos);
                itemManager.MainManager.MapManager.MapUpdate(pos, seedData.GrowBlock.ItemAccess);
                return true;
            }
            else
            {
                WaterWait(cancellationTokenSource.Token).Forget();
            }
        }
        return false;
    }
}
