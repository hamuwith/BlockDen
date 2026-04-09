using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using static Item;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using static Food;
using System.Threading;

public class Player : Character
{
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] float itemRange;
    [SerializeField] Transform hand;
    [SerializeField] Transform bagPosition;
    [SerializeField] Transform toolBagPosition;
    [SerializeField] Inventory inventory;
    [SerializeField] PlayerUI playerUI;
    [SerializeField] Transform HideTransform;
    Item[] toolItems;
    public Item HaveItem
    {
        get
        {
            return Bag[BagIndex];
        }
    }
    public Inventory Inventory => inventory;
    CancellationTokenSource cancellationTokenSource;
    Vector3Int? targetPosition;
    Vector3Int? putTargetPosition;
    Vector3Int? toolTargetPosition;
    float sqrRange;
    public Item[] Bag { get; set; }
    public int[] MaterialBag { get; set; }
    bool isMove;
    Vector3 playerDirection;
    Vector3 gravityDirection;
    Transform moveTarget;
    new Rigidbody rigidbody;
    int bagIndex;
    Block.BlockTypeEnum currentToolType;
    public int BagIndex
    {
        get
        {
            return bagIndex;
        }
        set
        {

            bagIndex = value;
            if (Bag[bagIndex] == null) bagIndex = 0;
            for (int i = 0; i < Bag.Length - 1; i++)
            {
                if (Bag[i] == null) continue;
                Bag[i].transform.parent = BagIndex == i ? hand : toolBagPosition;
                Bag[i].transform.localPosition = Vector3.zero;
            }
        }
    }
    PlayerUI currentTool;
    FoodStatus foodStatus;
    enum BagStatus
    {
        Filled,
        Empty,
        Success,
        ToolSuccess,
        Add,
        MaterialAdd,
    }
    /// <summary>
    /// プレイヤーのインベントリの種類を表す列挙型
    /// </summary>
    public enum InventoryType
    {
        Tool,
        Food,
        Carry,
        Bag,
        Material,
        Null,
    }
    public InventoryType GetInventoryType(Item item)
    {
        if(item == null)
        {
            return InventoryType.Null;
        }
        else if (item.ItemState.ItemType == ItemCategory.BreakTool)
        {
            return InventoryType.Tool;
        }
        else if (item.ItemState.ItemType == ItemCategory.Food)
        {
            return InventoryType.Food;
        }
        else if(item.ItemState.ItemType == ItemCategory.Material)
        {
            return InventoryType.Material;
        }
        else if (item.ItemState.ItemType == ItemCategory.Bag)
        {
            return InventoryType.Bag;
        }
        else
        {
            return InventoryType.Carry;
        }
    }
    /// <summary>
    /// プレイヤーの初期化を行うメソッド
    /// </summary>
    /// <param name="mainManager"></param>
    public override void Init(MainManager mainManager)
    {
        base.Init(mainManager);
        HideTransform.parent = null;
        Inventory.Init(this);
        playerUI.Init(mainManager.ItemManager);
        SetInputEvent();
        sqrRange = itemRange * itemRange;
        rigidbody = GetComponent<Rigidbody>();
        gravityDirection = Vector3.down;
        transform.rotation = Quaternion.LookRotation(Vector3.right, -gravityDirection);
        foodStatus = new FoodStatus {Duration = 0, MoveSpeed = 1f, Power = 0};
        Bag = new Item[Inventory.InventorySize];
        MaterialBag = new int[itemManager.GetItemNum(ItemCategory.Material)];
        var firstItems = itemManager.InstantiateFirstItems(Vector3.zero);
        currentToolType = Block.BlockTypeEnum.Dirt;
        toolItems = new Item[firstItems.Length];
        foreach (var firstItem in firstItems)
        {
            var bagState = GetBagState(firstItem);
            BagUpdate(bagState, firstItem);
            firstItem.transform.SetParent(hand);
        }
        var firstBag = itemManager.InstantiateFirstBag(Vector3.zero, null);
        var bagItemState = GetBagState(firstBag);
        BagUpdate(bagItemState, firstBag);
        BagIndex = 0;
    }
    public void CloseToolUI()
    {
        currentTool = null;
    }
    private void SetInputEvent()
    {
        var actionMaps = inputActions.actionMaps.ToDictionary(x => x.name, x => x);
        actionMaps.TryGetValue("Player", out var playerMap);
        var actionPlayer = playerMap.ToDictionary(x => x.name, x => x);
        actionPlayer.TryGetValue("Action", out var _action);
        actionPlayer.TryGetValue("Drop", out var _drop);
        actionPlayer.TryGetValue("Move", out var _move);
        actionPlayer.TryGetValue("Previous", out var _previous);
        actionPlayer.TryGetValue("Next", out var _next);
        _action.started += context => Action();
        _action.canceled += context => ActionStop();
        _drop.performed += context => Cancel();
        _move.performed += context => Move(context.ReadValue<Vector2>());
        _move.canceled += context => Stop();
        _previous.performed += context => SelectBagItem(true);
        _next.performed += context => SelectBagItem(false);
    }
    private void Action()
    {
        if(currentTool != null)
        {
            currentTool.Action();
            return;
        }
        else if (toolTargetPosition.HasValue)
        {
            itemManager.BoxAdd(MaterialBag);
            //currentTool = mapManager.GetBlock(toolTargetPosition.Value) as Tool;
            if (mapManager.IsHouse(toolTargetPosition.Value))
            {
                currentTool = playerUI;
            }
            else
            {
                currentTool = mapManager.GetBlock(toolTargetPosition.Value) as PlayerUI;
            }
            currentTool.OpenUI(this);
            return;
        }
        var haveItemCategory = HaveItem.ItemState.ItemType;
        if (haveItemCategory == ItemCategory.BreakTool)
        {
            Break();
        }
        else if (haveItemCategory == ItemCategory.UnnatureBlock || haveItemCategory == ItemCategory.Weapon)
        {
            Put();
        }
        else if (haveItemCategory == ItemCategory.Food)
        {
            Eat();
        }
    }
    void Eat()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        foodStatus = (HaveItem as Food).StatusUp;
        cancellationTokenSource = new CancellationTokenSource();
        var cancelToken = cancellationTokenSource.Token;
        Eatting(foodStatus.Duration,cancelToken).Forget();
        BagReduce(1, BagIndex);
    }
    private async UniTask Eatting(int ms, CancellationToken ct)
    {        
        await UniTask.Delay(ms, cancellationToken: ct);
        foodStatus.Power = 0;
        foodStatus.MoveSpeed = 1f;
    }
    private void ActionStop()
    {
        animator.SetBool("isBreaking", false);
        if(targetPosition.HasValue)
        {
            var block = mapManager.GetBlock(targetPosition.Value);
            block.ResetHardness();
        }
    }
    private void Break()
    {
        animator.SetBool("isBreaking", true);
    }
    public void BreakAction()
    {
        if (!targetPosition.HasValue)
        {
            return;
        }
        Vector3Int targetPositionValue = targetPosition.Value;
        var block = mapManager.GetBlock(targetPositionValue);
        var power = 1;
        if (HaveItem.ItemState.ItemType == ItemCategory.BreakTool && block.BlockType == (HaveItem as BreakTool).BlockType)
        {
            power = (HaveItem as BreakTool).BreakPower;
        }
        var isBreak = block.Break(power + foodStatus.Power, targetPositionValue);
        if (isBreak)
        {
            targetPosition = null;
        }
    }
    private void Put()
    {
        if (!putTargetPosition.HasValue)
        {
            return;
        }
        if (HaveItem.ItemState.ItemType != ItemCategory.UnnatureBlock && HaveItem.ItemState.ItemType != ItemCategory.Weapon)
        {
            return;
        }
        Vector3Int targetPositionValue = putTargetPosition.Value;
        var item = mapManager.MapUpdate(targetPositionValue, HaveItem.ItemState.ItemType, HaveItem.ItemState.Id);
        var empty = BagReduce(item.Num, BagIndex);
    }
    public void ChangeTool(Block.BlockTypeEnum blockType, bool surely = false)
    {
        if (currentToolType == blockType && !surely)
        {
            return;
        }
        Bag[(int)InventoryType.Tool] = toolItems[(int)blockType];
        currentToolType = blockType;
        InventoryUpdate();
        ChangeToolPosition();
    }
    private void ChangeToolPosition()
    {
        for (int i = 0; i < toolItems.Length; i++)
        {
            if (toolItems[i] == null) continue;
            toolItems[i].transform.parent = currentToolType == (Block.BlockTypeEnum)i ? hand : toolBagPosition;
            toolItems[i].transform.localPosition = Vector3.zero;
        }
    }
    private void BagUpdate(BagStatus bagStatus, Item item, Item prev = null)
    {
        var intentoryType = GetInventoryType(item);
        if (bagStatus == BagStatus.Success)
        {
            Bag[(int)intentoryType] = item;
            InventoryUpdate();
        }
        else if(bagStatus == BagStatus.Add)
        {
            Bag[(int)intentoryType].Num += item.Num;
            InventoryUpdate();
        }
        else if (bagStatus == BagStatus.MaterialAdd)
        {
            MaterialBag[item.ItemState.Id] += item.Num;
        }
        else if (bagStatus == BagStatus.ToolSuccess)
        {
            var breakToolType = (item as BreakTool).BlockType;
            if (toolItems[(int)breakToolType] != null) Destroy(toolItems[(int)breakToolType].gameObject);
            toolItems[(int)breakToolType] = item;
            ChangeTool(breakToolType, true);
        }
        else
        {
            intentoryType = GetInventoryType(prev);
            Bag[(int)intentoryType] = null;
            InventoryUpdate();
        }
    }
    private BagStatus GetBagState(Item item)
    {
        var intentoryType = GetInventoryType(item);
        if (intentoryType == InventoryType.Food || intentoryType == InventoryType.Carry || intentoryType == InventoryType.Bag)
        {
            if (Bag[(int)intentoryType] == null || item.ItemState.Id != Bag[(int)intentoryType].ItemState.Id)
            {
                return BagStatus.Success;
            }
            return BagStatus.Add;
        }
        else if (intentoryType == InventoryType.Material)
        {
            return BagStatus.MaterialAdd;
        }
        else if (intentoryType == InventoryType.Null)
        {
            return BagStatus.Empty;
        }
        else
        {
            return BagStatus.ToolSuccess;
        }
    }
    private BagStatus BagReduce(int num, int index)
    {
        Bag[index].Num -= num;
        if (Bag[index].Num == 0)
        {
            Destroy(Bag[index].gameObject);
            Bag[index] = null;
            BagIndex = 0;
            InventoryUpdate();
            return BagStatus.Empty;

        }
        InventoryUpdate();
        return BagStatus.Success;
    }
    void InventoryUpdate()
    {
        currentTool?.UpdateAction();
        Inventory.UpdateInventory();
    }
    private void Cancel()
    {
        if (currentTool != null)
        {
            currentTool.Cancel();
            return;
        }
        Drop();
    }
    private void Drop()
    {
        if (HaveItem == null) return;
        if (HaveItem.ItemState.ItemType == ItemCategory.BreakTool || HaveItem.ItemState.ItemType == ItemCategory.Bag) return;
        HaveItem.transform.SetParent(null);
        HaveItem.PlayerDrop(transform.position);
        Bag[BagIndex] = null;
        BagIndex = 0;
        InventoryUpdate();
    }
    private void GetItem()
    {
        List<Item> getItems = new List<Item>();
        foreach (var item in itemManager.Items)
        {
            if ((transform.position - item.transform.position).sqrMagnitude <= sqrRange)
            {
                var status = GetBagState(item);
                BagUpdate(status, item);
                if (status == BagStatus.Filled) continue;
                else if (status == BagStatus.Add || status == BagStatus.MaterialAdd)
                {
                    Destroy(item.gameObject);
                }
                else if (status == BagStatus.Success || status == BagStatus.ToolSuccess)
                {
                    InBag(item);
                }
                getItems.Add(item);
            }
        }
        foreach (var getItem in getItems)
        {
            itemManager.RemoveFieldItem(getItem);
        }
    }
    /// <summary>
    /// アイテムを作成するメソッド
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public void Make(Item item)
    {
        var status = GetBagState(item);
        var makeItem = item;
        if (status == BagStatus.Success || status == BagStatus.ToolSuccess)
        {
            makeItem = itemManager.InstantiateItem(item, Vector3.zero);
            BagUpdate(status, makeItem);
            InBag(makeItem);
            return;
        }
        BagUpdate(status, makeItem);
    }
    /// <summary>
    /// アイテムを変更するメソッド
    /// </summary>
    /// <param name="item"></param>
    public void ChangeItem(Item item, Item previous)
    {
        var status = GetBagState(item);
        BagUpdate(status, item, previous);
        if (status == BagStatus.Success || status == BagStatus.ToolSuccess)
        {
            InBag(item);
        }
    }
    private void InBag(Item item)
    {
        if (item.ItemState.ItemType == ItemCategory.Bag)
        {
            item.transform.SetParent(bagPosition);
            item.transform.localPosition = Vector3.zero;
        }
        else
        {
            var intentoryType = GetInventoryType(item);
            BagIndex = (int)intentoryType;
        }
        item.SetItem(true);
    }
    private void SelectBagItem(bool left)
    {
        if (currentTool != null)
        {
            currentTool.SelectTab(left);
        }
        else
        {
            Inventory.SelectItem(left);
        }
    }
    private void Move(Vector2 vector2)
    {
        if (currentTool != null)
        {
            currentTool.Select(vector2);
            return;
        }
        playerDirection = new Vector3(vector2.x, 0f, vector2.y);
        isMove = true;
    }
    private void Stop()
    {
        isMove = false;
    }
    void Update()
    {
        if(currentTool != null)
        {
            return;
        }
        Vector3Int playerPos = Vector3Int.RoundToInt(transform.position);
        GetItem();
        SetTarget(playerPos, transform.forward);
        Inventory.transform.position = transform.position;
        HideTransform.position = transform.position;
    }
    private void FixedUpdate()
    {
        var gravityDirection = Vector3.down;
        bool isHorizon = animator.GetBool("isBreaking");
        if (moveTarget != null && !isHorizon)
        {
            gravityDirection = -(transform.position - moveTarget.position).normalized;
        }
        MovePlayer(isHorizon);
        rigidbody.AddForce(gravityDirection * 8f, ForceMode.Acceleration);
    }
    void SetTarget(Vector3Int playerPos, Vector3 playerDirection)
    {
        toolTargetPosition = ToolTarget(playerPos, playerDirection);
        ItemCategory haveItemCategory = HaveItem.ItemState.ItemType;
        Vector3Int? targetBlock = null;
        if (toolTargetPosition.HasValue)
        {
            putTargetPosition = null;
        }
        else if (haveItemCategory == ItemCategory.UnnatureBlock || haveItemCategory == ItemCategory.Weapon)
        {
            putTargetPosition = PutTarget(playerPos, transform.forward);
        }
        else if (haveItemCategory == ItemCategory.BreakTool)
        {
            targetBlock = TargetBlock(playerPos, transform.forward);
        }
        if (targetBlock.HasValue)
        {
            var block = mapManager.GetBlock(targetBlock.Value);
            ChangeTool(block.BlockType);
        }
        TargetHighlight(targetBlock);
    }
    Vector3 MoveDirection()
    {
        Vector3 moveDirection = playerDirection;
        moveDirection = Quaternion.Euler(-gravityDirection.z * 90f, 0f, gravityDirection.x * 90f) * playerDirection;
        return moveDirection;
    }
    private void MovePlayer(bool isHorizon)
    {
        if (!isMove) return;
        if (moveTarget == null)
        {
            return;
        }
        if(isHorizon) gravityDirection = Vector3.down;
        else gravityDirection = GetClosestFaceNormal(transform.position, moveTarget.position);
        Vector3 moveDirection = MoveDirection();
        transform.rotation = Quaternion.LookRotation(moveDirection, -gravityDirection);
        rigidbody.MovePosition(transform.position + moveDirection * moveSpeed * Time.deltaTime * foodStatus.MoveSpeed);
    }
    private void TargetHighlight(Vector3Int? targetBlock)
    {
        if (targetBlock == targetPosition)
        {
            return;
        }
        if (targetPosition.HasValue)
        {
            mapManager.GetBlock(targetPosition.Value).Highlight(false);
        }
        if (targetBlock.HasValue)
        {
            mapManager.GetBlock(targetBlock.Value).Highlight(true);
        }
        targetPosition = targetBlock;
    }
    private Vector3Int? TargetBlock(Vector3Int playerPos, Vector3 playerDirection)
    {
        playerDirection *= 1.4f;
        Vector3Int targetPosition = Vector3Int.RoundToInt(playerPos + playerDirection);
        if (mapManager.IsBlock(targetPosition))
        {
            return targetPosition;
        }
        Vector3 targetForward = Quaternion.AngleAxis(-45f, transform.right) * playerDirection;
        targetPosition = Vector3Int.RoundToInt(playerPos + targetForward);
        if (mapManager.IsBlock(targetPosition))
        {
            return targetPosition;
        }
        targetForward = Quaternion.AngleAxis(45f, transform.right) * playerDirection;
        targetPosition = Vector3Int.RoundToInt(playerPos + targetForward);
        Vector3Int ridePosition = Vector3Int.RoundToInt(transform.position - transform.up);
        if (mapManager.IsBlock(targetPosition) && ridePosition != targetPosition)
        {
            return targetPosition;
        }
        return null;
    }
    private Vector3Int? PutTarget(Vector3Int playerPos, Vector3 playerDirection)
    {
        playerDirection *= 1.4f;
        Vector3 targetForward = Quaternion.AngleAxis(45f, transform.right) * playerDirection;
        Vector3Int targetPosition = Vector3Int.RoundToInt(playerPos + targetForward);
        if (!mapManager.IsBlock(targetPosition))
        {
            return targetPosition;
        }
        targetPosition = Vector3Int.RoundToInt(playerPos + playerDirection);
        if (!mapManager.IsBlock(targetPosition))
        {
            return targetPosition;
        }
        targetForward = Quaternion.AngleAxis(-45f, transform.right) * playerDirection;
        targetPosition = Vector3Int.RoundToInt(playerPos + targetForward);
        if (!mapManager.IsBlock(targetPosition))
        {
            return targetPosition;
        }
        return null;
    }
    private Vector3Int? ToolTarget(Vector3Int playerPos, Vector3 playerDirection)
    {
        playerDirection *= 1.4f;
        Vector3Int targetPosition = Vector3Int.RoundToInt(playerPos + playerDirection);
        if (mapManager.IsTool(targetPosition))
        {
            return targetPosition;
        }
        Vector3 targetForward = Quaternion.AngleAxis(-45f, transform.right) * playerDirection;
        targetPosition = Vector3Int.RoundToInt(playerPos + targetForward);
        if (mapManager.IsTool(targetPosition))
        {
            return targetPosition;
        }
        targetForward = Quaternion.AngleAxis(45f, transform.right) * playerDirection;
        targetPosition = Vector3Int.RoundToInt(playerPos + targetForward);
        Vector3Int ridePosition = Vector3Int.RoundToInt(transform.position - transform.up);
        if (mapManager.IsTool(targetPosition) && ridePosition != targetPosition)
        {
            return targetPosition;
        }
        return null;
    }
    Vector3 GetClosestFaceNormal(Vector3 spherePos, Vector3 cubePos)
    {
        Vector3 dir = cubePos - spherePos; // 立方体中心 -> 球中心

        float ax = Mathf.Abs(dir.x);
        float ay = Mathf.Abs(dir.y);
        float az = Mathf.Abs(dir.z);

        if (ax >= ay && ax >= az)
            return dir.x >= 0 ? Vector3.right : Vector3.left;
        else if (ay >= ax && ay >= az)
            return dir.y >= 0 ? Vector3.up : Vector3.down;
        else
            return dir.z >= 0 ? Vector3.forward : Vector3.back;
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("Block"))
        {
            moveTarget = collision.transform;
        }
    }
    void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
}
