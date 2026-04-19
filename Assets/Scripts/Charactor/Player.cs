using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using static Item;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
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
    [SerializeField] GameObject highlightPrefab;
    [SerializeField] ItemAccess[] firstItems;
    Transform highlight;
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
    public ItemAccess[] MaterialBag { get; set; }
    bool isMove;
    bool isBreak;
    Vector3 playerDirection;
    Vector3 gravityDirection;
    Transform moveTarget;
    bool jump;
    new Rigidbody rigidbody;
    int bagIndex;
    Block.BlockTypeEnum currentToolType;
    readonly Vector3 downVector = Vector3.down;
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
    float upSpeed;
    int upPower;
    public enum BagStatus
    {
        Filled,
        Empty,
        Success,
        ToolSuccess,
        Add,
        MaterialAdd,
    }
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
        return GetInventoryType(item?.ItemAccess);
    }
    public InventoryType GetInventoryType(ItemAccess? itemState)
    {
        if (itemState == null)
        {
            return InventoryType.Null;
        }
        else if (itemState.Value.Category == ItemCategory.BreakTool)
        {
            return InventoryType.Tool;
        }
        else if (itemState.Value.Category == ItemCategory.Food)
        {
            return InventoryType.Food;
        }
        else if (itemState.Value.Category == ItemCategory.Material)
        {
            return InventoryType.Material;
        }
        else if (itemState.Value.Category == ItemCategory.Bag)
        {
            return InventoryType.Bag;
        }
        else
        {
            return InventoryType.Carry;
        }
    }
    public override void Init(MainManager mainManager)
    {
        base.Init(mainManager);
        highlight = Instantiate(highlightPrefab, Vector3.down * 100f, Quaternion.identity).transform;
        HideTransform.parent = null;
        Inventory.Init(this, itemManager);
        playerUI.Init(mainManager.ItemManager);
        SetInputEvent();
        sqrRange = itemRange * itemRange;
        rigidbody = GetComponent<Rigidbody>();
        gravityDirection = Vector3.down;
        transform.rotation = Quaternion.LookRotation(Vector3.right, -gravityDirection);
        upSpeed = 1f;
        Bag = new Item[Inventory.InventorySize];
        MaterialBag = new ItemAccess[Inventory.BagSize];
        for (int i = 0; i < MaterialBag.Length; i++)
        {
            MaterialBag[i].Id = -1;
        }
        currentToolType = Block.BlockTypeEnum.Dirt;
        toolItems = new Item[firstItems.Length];
        foreach (var firstItem in firstItems)
        {
            BagUpdate(firstItem);
        }
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
        if (currentTool != null)
        {
            currentTool.Action();
            return;
        }
        else if (toolTargetPosition.HasValue)
        {
            if (HaveItem?.ItemAccess.Category == ItemCategory.BreakTool && mapManager.GetBlock(toolTargetPosition.Value).BlockType == Block.BlockTypeEnum.Water)
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
        var haveItemCategory = HaveItem.ItemAccess.Category;
        if (haveItemCategory == ItemCategory.BreakTool)
        {
            Break();
        }
        else if (PutType(haveItemCategory))
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
        FoodData foodData = itemManager.GetItem(HaveItem.ItemAccess) as FoodData;
        upSpeed = foodData.MoveSpeed;
        upPower = foodData.Power;
        cancellationTokenSource = new CancellationTokenSource();
        var cancelToken = cancellationTokenSource.Token;
        Eatting(foodData.Duration, cancelToken).Forget();
        BagReduce(1, BagIndex);
    }
    private async UniTask Eatting(int ms, CancellationToken ct)
    {
        await UniTask.Delay(ms, cancellationToken: ct);
        upPower = 0;
        upSpeed = 1f;
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
        var breakToolData = itemManager.GetItem(HaveItem.ItemAccess) as BreakToolData;
        if (HaveItem.ItemAccess.Category == ItemCategory.BreakTool && block.BlockType == breakToolData.BlockType)
        {
            power = breakToolData.BreakPower;
            bool isWater = block.BlockType == Block.BlockTypeEnum.Water;
            if (isWater)
            {
                if (block is Seed)
                {
                    power = (HaveItem as BreakTool).UseWater();
                }
                else
                {
                    (HaveItem as BreakTool).GetWater();
                }
            }
        }
        var isBreak = block.Break(power + upPower, targetPositionValue);
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
        if (!PutType(HaveItem.ItemAccess.Category))
        {
            return;
        }
        Vector3Int targetPositionValue = putTargetPosition.Value;
        var item = mapManager.MapUpdate(targetPositionValue, HaveItem.ItemAccess);
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

    private BagStatus GetBagState(ItemAccess itemState)
    {
        var intentoryType = GetInventoryType(itemState);
        if (intentoryType == InventoryType.Food || intentoryType == InventoryType.Carry || intentoryType == InventoryType.Bag)
        {
            if (Bag[(int)intentoryType] == null || itemState.Id != Bag[(int)intentoryType].ItemAccess.Id)
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

    private int BagUpdate(ItemData itemData, bool isUnit = true, bool first = false)
    {
        var itemAccess = itemData.ItemAccess;
        var bagStatus = GetBagState(itemAccess);
        var num = isUnit ? Mathf.Min(itemAccess.Num, itemData.UnitNum) : itemAccess.Num;
        var intentoryType = GetInventoryType(itemAccess);
        if (bagStatus == BagStatus.Success)
        {
            var item = itemManager.GetPoolItem(itemAccess, num, Vector3.zero);
            Bag[(int)intentoryType] = item;
            InventoryUpdate();
            InBag(item);
        }
        else if (bagStatus == BagStatus.Add)
        {
            Bag[(int)intentoryType].Num += num;
            if (itemData.MaxNum < Bag[(int)intentoryType].Num)
            {
                num -= Bag[(int)intentoryType].Num - itemData.MaxNum;
                Bag[(int)intentoryType].Num = itemData.MaxNum;
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
                    if (itemAccess.Id == MaterialBag[i].Id)
                    {
                        MaterialBag[i].Num += num;
                        if (itemData.MaxNum < MaterialBag[i].Num)
                        {
                            num -= MaterialBag[i].Num - itemData.MaxNum;
                            MaterialBag[i].Num = itemData.MaxNum;
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
                MaterialBag[nullNum].Id = itemAccess.Id;
                MaterialBag[nullNum].Category = itemAccess.Category;
                return num;
            }
        }
        else if (bagStatus == BagStatus.ToolSuccess)
        {
            var item = itemManager.InstantiateBreakTool(itemAccess, Vector3.zero);
            var breakToolData = itemData as BreakToolData;
            var breakToolType = breakToolData.BlockType;
            if (toolItems[(int)breakToolType] != null) Destroy(toolItems[(int)breakToolType].gameObject);
            toolItems[(int)breakToolType] = item;
            InBag(item);
            ChangeTool(breakToolType, true);
            return 1;
        }
        else
        {
            Debug.LogError($"BagUpdate Error: {bagStatus}, Item: {itemAccess.Category}, Num: {itemAccess.Num}");
            return -1;
        }
        return num;
    }
    public int BagUpdate(ItemAccess boxItem, bool isUnit = true, bool first = false)
    {
        var baseItem = itemManager.GetItem(boxItem);
        return BagUpdate(baseItem, isUnit, first);
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
        if (HaveItem.ItemAccess.Category == ItemCategory.BreakTool || HaveItem.ItemAccess.Category == ItemCategory.Bag) return;
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
                var status = GetBagState(item.ItemAccess);
                var num = BagUpdate(item.ItemAccess, false);
                if (status == BagStatus.Filled) continue;
                else if (status == BagStatus.Add)
                {
                    item.Release();
                }
                else if (status == BagStatus.MaterialAdd && num >= item.Num)
                {
                    item.Release();
                }
                else if (status == BagStatus.Success || status == BagStatus.ToolSuccess)
                {
                    item.Release();
                }
                getItems.Add(item);
            }
        }
        foreach (var getItem in getItems)
        {
            itemManager.RemoveFieldItem(getItem);
        }
    }
    public void Make(ItemData item, int num)
    {
        var itemAccess = item.ItemAccess;
        itemAccess.Num = num;
        BagUpdate(itemAccess);
    }
    private void InBag(Item item)
    {
        if (item.ItemAccess.Category == ItemCategory.Bag)
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
        if (currentTool != null)
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
        if (isBreak && gravityDirectionForward != gravityDirection)
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
            rigidbody.MovePosition(transform.position + moveDirection * moveSpeed * Time.deltaTime * upSpeed * (isBreak ? breakMoveSpeed : 1f));
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
        if (HaveItem == null) BagIndex = 0;
        ItemCategory haveItemCategory = HaveItem.ItemAccess.Category;
        Vector3Int? targetBlock = null;
        putTargetPosition = null;
        if (toolTargetPosition.HasValue && !(mapManager.GetBlock(toolTargetPosition.Value).BlockType == Block.BlockTypeEnum.Water))
        {
        }
        else if (PutType(haveItemCategory))
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
        highlight.position = targetBlock.HasValue ? targetBlock.Value : downVector * 100f;
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
            targetPosition -= Vector3Int.up;
            if (!mapManager.IsTool(targetPosition))
            {
                targetPosition += Vector3Int.up;
                return targetPosition;
            }
        }
        targetPosition = Vector3Int.RoundToInt(playerPos + playerDirection);
        if (!mapManager.IsBlock(targetPosition))
        {
            targetPosition -= Vector3Int.up;
            if (!mapManager.IsTool(targetPosition))
            {
                targetPosition += Vector3Int.up;
                return targetPosition;
            }
        }
        targetForward = Quaternion.AngleAxis(-45f, transform.right) * playerDirection;
        targetPosition = Vector3Int.RoundToInt(playerPos + targetForward);
        if (!mapManager.IsBlock(targetPosition))
        {
            targetPosition -= Vector3Int.up;
            if (!mapManager.IsTool(targetPosition))
            {
                targetPosition += Vector3Int.up;
                return targetPosition;
            }
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
        Vector3 dir = cubePos - spherePos;

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
    private bool PutType(ItemCategory category)
    {
        return category == ItemCategory.UnnatureBlock || category == ItemCategory.Weapon || category == ItemCategory.Seed || category == ItemCategory.WeaponBase || category == ItemCategory.Tool;
    }
    void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
}
