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
    [SerializeField] float jumpForce;
    [SerializeField] float breakMoveSpeed;
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
    public ItemMaterial[] MaterialBag { get; set; }
    bool isMove;
    bool isBreak;
    Vector3 playerDirection;
    Vector3 gravityDirection;
    Transform moveTarget;
    bool jump;
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
    public enum BagStatus
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
        return GetInventoryType(item?.ItemState);
    }
    public InventoryType GetInventoryType(ItemState itemState)
    {
        if (itemState == null)
        {
            return InventoryType.Null;
        }
        else if (itemState.ItemType == ItemCategory.BreakTool)
        {
            return InventoryType.Tool;
        }
        else if (itemState.ItemType == ItemCategory.Food)
        {
            return InventoryType.Food;
        }
        else if (itemState.ItemType == ItemCategory.Material)
        {
            return InventoryType.Material;
        }
        else if (itemState.ItemType == ItemCategory.Bag)
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
        Inventory.Init(this, itemManager);
        playerUI.Init(mainManager.ItemManager);
        SetInputEvent();
        sqrRange = itemRange * itemRange;
        rigidbody = GetComponent<Rigidbody>();
        gravityDirection = Vector3.down;
        transform.rotation = Quaternion.LookRotation(Vector3.right, -gravityDirection);
        foodStatus = new FoodStatus {Duration = 0, MoveSpeed = 1f, Power = 0};
        Bag = new Item[Inventory.InventorySize];
        MaterialBag = new ItemMaterial[Inventory.BagSize];
        for (int i = 0; i < MaterialBag.Length; i++)
        {
            MaterialBag[i].Id = -1;
        }
        var firstItems = itemManager.InstantiateFirstItems(Vector3.zero);
        currentToolType = Block.BlockTypeEnum.Dirt;
        toolItems = new Item[firstItems.Length];
        foreach (var firstItem in firstItems)
        {
            var bagState = GetBagState(firstItem.ItemState);
            BagUpdate(bagState, firstItem);
            firstItem.transform.SetParent(hand);
        }
        var firstBag = itemManager.InstantiateBag(Vector3.zero);
        var bagItemState = GetBagState(firstBag.ItemState);
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
            if(HaveItem?.ItemState.ItemType == ItemCategory.BreakTool && mapManager.GetBlock(toolTargetPosition.Value).BlockType == Block.BlockTypeEnum.Water)
            {
                Break();
                return;
            }
            else if (mapManager.IsHouse(toolTargetPosition.Value))
            {
                currentTool = playerUI;
                itemManager.BoxAdd(MaterialBag);
            }
            else
            {
                currentTool = mapManager.GetBlock(toolTargetPosition.Value) as PlayerUI;
            }
            currentTool.OpenUI(this);
            isMove = false;
            return;
        }
        var haveItemCategory = HaveItem.ItemState.ItemType;
        if (haveItemCategory == ItemCategory.BreakTool)
        {
            Break();
        }
        else if (haveItemCategory == ItemCategory.UnnatureBlock || haveItemCategory == ItemCategory.Weapon || haveItemCategory == ItemCategory.Seed)
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
        isBreak = false;
        if (targetPosition.HasValue)
        {
            var block = mapManager.GetBlock(targetPosition.Value);
            block.ResetHardness();
        }
    }
    private void Break()
    {
        animator.SetBool("isBreaking", true);
        isBreak = true;
    }
    public void BreakAction()
    {
        if (!targetPosition.HasValue)
        {
            return;
        }
        Vector3Int targetPositionValue = targetPosition.Value;
        var block = mapManager.GetBlock(targetPositionValue);
        var power = 0;
        if (HaveItem.ItemState.ItemType == ItemCategory.BreakTool && block.BlockType == (HaveItem as BreakTool).BlockType)
        {
            power = (HaveItem as BreakTool).BreakPower;
            bool isWater = block.BlockType == Block.BlockTypeEnum.Water;
            if (isWater)
            {
                if (block is Seed)
                {
                    power = (HaveItem as WateringCar).UseWater();                    
                }
                else
                {
                    (HaveItem as WateringCar).GetWater();
                }
            }
        }
        var isBreak = block.Break(power + foodStatus.Power, targetPositionValue);
        if (isBreak)
        {
            targetPosition = null;
            toolTargetPosition = null;
        }
    }
    private void Put()
    {
        if (!putTargetPosition.HasValue)
        {
            return;
        }
        if (HaveItem.ItemState.ItemType != ItemCategory.UnnatureBlock && HaveItem.ItemState.ItemType != ItemCategory.Weapon && HaveItem.ItemState.ItemType != ItemCategory.Seed)
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
    private int BagUpdate(BagStatus bagStatus, Item item, bool isUnit = true)
    {
        var num = isUnit ? item.ItemState.UnitNum : item.Num;
        var intentoryType = GetInventoryType(item);
        if (bagStatus == BagStatus.Success)
        {
            Bag[(int)intentoryType] = item;
            InventoryUpdate();
        }
        else if(bagStatus == BagStatus.Add)
        {
            Bag[(int)intentoryType].Num += num;
            InventoryUpdate();
        }
        else if (bagStatus == BagStatus.MaterialAdd)
        {
            var nullNum = -1;
            for (int i = 0; i < MaterialBag.Length; i++)
            {
                if (MaterialBag[i].Id != -1)
                {
                    if (MaterialBag[i].Id == item.ItemState.Id)
                    {
                        MaterialBag[i].Num += num;
                        return num;
                    }
                    if (item.ItemState.Id == MaterialBag[i].Id)
                    {
                        MaterialBag[i].Num += num;
                        if (item.ItemState.MaxNum < Bag[(int)intentoryType].Num)
                        {
                            num -= MaterialBag[i].Num - item.ItemState.MaxNum;
                            MaterialBag[i].Num = item.ItemState.MaxNum;
                            return num;
                        }
                        else
                        {
                            return num;
                        }
                    }
                }
                else if (nullNum == -1)
                {
                    nullNum = i;
                }
            }
            if (nullNum != -1)
            {
                MaterialBag[nullNum].Num = num;
                MaterialBag[nullNum].Id = item.ItemState.Id;
                MaterialBag[nullNum].Category = item.ItemState.ItemType;
                return num;
            }
            return 0;
        }
        else if (bagStatus == BagStatus.ToolSuccess)
        {
            var breakToolType = (item as BreakTool).BlockType;
            if (toolItems[(int)breakToolType] != null) Destroy(toolItems[(int)breakToolType].gameObject);
            toolItems[(int)breakToolType] = item;
            ChangeTool(breakToolType, true);
            return 1;
        }
        else
        {
            Debug.LogError($"BagUpdate Error: {bagStatus}, Item: {item.ItemState.ItemType}, Num: {item.Num}");
            return -1;
        }
        return num;
    }
    private BagStatus GetBagState(ItemState itemState)
    {
        var intentoryType = GetInventoryType(itemState);
        if (intentoryType == InventoryType.Food || intentoryType == InventoryType.Carry || intentoryType == InventoryType.Bag)
        {
            if (Bag[(int)intentoryType] == null || itemState.Id != Bag[(int)intentoryType].ItemState.Id)
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
    public int BagUpdate(BoxItem boxItem, bool isUnit = true)
    {
        var bagStatus = GetBagState(boxItem.ItemState);
        var itemState = boxItem.ItemState;
        var num = isUnit ? Mathf.Min(boxItem.Num, itemState.UnitNum) : boxItem.Num;
        var intentoryType = GetInventoryType(itemState);
        if (bagStatus == BagStatus.Success)
        {
            var item = itemManager.GetPoolItem(itemState, num, Vector3.zero);
            Bag[(int)intentoryType] = item;
            InventoryUpdate();
        }
        else if (bagStatus == BagStatus.Add)
        {
            Bag[(int)intentoryType].Num += num;
            if(boxItem.ItemState.MaxNum < Bag[(int)intentoryType].Num)
            {
                num -= Bag[(int)intentoryType].Num - boxItem.ItemState.MaxNum;
                Bag[(int)intentoryType].Num = boxItem.ItemState.MaxNum;
            }
            InventoryUpdate();
        }
        else if (bagStatus == BagStatus.MaterialAdd)
        {
            var nullNum = -1;
            for (int i = 0; i < MaterialBag.Length; i++)
            {
                if (MaterialBag[i].Id != -1)
                {
                    if (itemState.Id == MaterialBag[i].Id)
                    {
                        MaterialBag[i].Num += num;
                        if (itemState.MaxNum < Bag[(int)intentoryType].Num)
                        {
                            num -= MaterialBag[i].Num - itemState.MaxNum;
                            MaterialBag[i].Num = itemState.MaxNum;
                            return num;
                        }
                        else
                        {
                            return num;
                        }
                    }
                }
                else if (nullNum == -1)
                {
                    nullNum = i;
                }
            }
            if(nullNum != -1)
            {
                MaterialBag[nullNum].Num = num;
                MaterialBag[nullNum].Id = itemState.Id;
                MaterialBag[nullNum].Category = itemState.ItemType;
                return num;
            }
        }
        else if (bagStatus == BagStatus.ToolSuccess)
        {
            var item = itemManager.InstantiateItem(itemState, Vector3.zero);
            var breakToolType = (item as BreakTool).BlockType;
            if (toolItems[(int)breakToolType] != null) Destroy(toolItems[(int)breakToolType].gameObject);
            toolItems[(int)breakToolType] = item;
            ChangeTool(breakToolType, true);
            return 1;
        }
        else
        {
            Debug.LogError($"BagUpdate Error: {bagStatus}, Item: {itemState.ItemType}, Num: {boxItem.Num}");
            return -1;
        }
        return num;
    }
    public BagStatus BagReduce(int num, int index)
    {
        Bag[index].Num -= num;
        if (Bag[index].Num <= 0)
        {
            Bag[index].Release();
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
        HaveItem.SetItem(true);
        HaveItem.PlayerDrop(transform.forward + transform.up);
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
                var status = GetBagState(item.ItemState);
                var num = BagUpdate(status, item, false);
                if (status == BagStatus.Filled) continue;
                else if (status == BagStatus.Add)
                {
                    item.Release();
                }
                else if(status == BagStatus.MaterialAdd && num >= item.Num)
                {
                    item.Release();
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
    public void Make(Item item, int num)
    {
        var status = GetBagState(item.ItemState);
        var makeItem = item;
        if (status == BagStatus.Success)
        {
            makeItem = itemManager.GetPoolItem(item, num, Vector3.zero);
            InBag(makeItem);
        }
        else if (status == BagStatus.ToolSuccess)
        {
            makeItem = itemManager.InstantiateItem(item.ItemState, Vector3.zero);
            InBag(makeItem);
        }
        BagUpdate(status, makeItem);
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
        item.SetItem(false);
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
        MovePlayer();
        if (moveTarget != null)
        {
            gravityDirection = -(transform.position - moveTarget.position).normalized;
        }
        rigidbody.AddForce(gravityDirection * 8f, ForceMode.Acceleration);
    }
    private void MovePlayer()
    {
        if (moveTarget == null)
        {
            return;
        }
        var gravityDirectionForward = GetClosestFaceNormal(transform.position, moveTarget.position);
        if(isBreak && gravityDirectionForward != gravityDirection)
        {
            return;
        }
        gravityDirection = gravityDirectionForward;
        if (gravityDirection == Vector3.up) gravityDirection = Vector3.down;
        Vector3 moveDirection = MoveDirection(gravityDirection);
        if (Vector3.Dot(moveDirection, Vector3.down) > 0.001f)
        {
            jump = true;
            gravityDirection = Vector3.down;
            moveDirection = MoveDirection(gravityDirection);
            transform.rotation = Quaternion.LookRotation(moveDirection, -gravityDirection);
            var forceDirection = moveDirection - gravityDirection;
            rigidbody.AddForce(forceDirection * jumpForce, ForceMode.Impulse);
            moveTarget = null;
        }
        else
        {
            if (!isMove) return;
            transform.rotation = Quaternion.LookRotation(moveDirection, -gravityDirection);
            rigidbody.MovePosition(transform.position + moveDirection * moveSpeed * Time.deltaTime * foodStatus.MoveSpeed * (isBreak ? breakMoveSpeed : 1f));
        }
    }
    Vector3 MoveDirection(Vector3 gravityDirection)
    {
        Vector3 moveDirection = playerDirection;
        moveDirection = Quaternion.Euler(-gravityDirection.z * 90f, 0f, gravityDirection.x * 90f) * playerDirection;
        return moveDirection;
    }
    void SetTarget(Vector3Int playerPos, Vector3 playerDirection)
    {
        toolTargetPosition = ToolTarget(playerPos, playerDirection);
        if(HaveItem == null) BagIndex = 0;
        ItemCategory haveItemCategory = HaveItem.ItemState.ItemType;
        Vector3Int? targetBlock = null;
        putTargetPosition = null;
        if (toolTargetPosition.HasValue && !(mapManager.GetBlock(toolTargetPosition.Value).BlockType == Block.BlockTypeEnum.Water))
        {
        }
        else if (haveItemCategory == ItemCategory.UnnatureBlock || haveItemCategory == ItemCategory.Weapon || haveItemCategory == ItemCategory.Seed)
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
        Vector3 targetForward = Quaternion.AngleAxis(45f, transform.right) * playerDirection;
        targetPosition = Vector3Int.RoundToInt(playerPos + targetForward);
        if (mapManager.IsBlock(targetPosition))
        {
            return targetPosition;
        }
        targetForward = Quaternion.AngleAxis(-45f, transform.right) * playerDirection;
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
        Vector3 targetForward = Quaternion.AngleAxis(45f, transform.right) * playerDirection;
        targetPosition = Vector3Int.RoundToInt(playerPos + targetForward);
        if (mapManager.IsTool(targetPosition))
        {
            return targetPosition;
        }
        targetForward = Quaternion.AngleAxis(-45f, transform.right) * playerDirection;
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
        else if (ay > ax && ay > az)
            return dir.y >= 0 ? Vector3.up : Vector3.down;
        else
            return dir.z >= 0 ? Vector3.forward : Vector3.back;
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("Block"))
        {
            var gravityDirection = GetClosestFaceNormal(transform.position, collision.transform.position);
            if (gravityDirection == Vector3.down) jump = false;
            if (!jump)
            {
                moveTarget = collision.transform;
            }
        }
    }
    void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
}
