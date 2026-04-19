using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseUI : MonoBehaviour
{
    [SerializeField] protected Image[] buttons;
    [SerializeField] protected Image highlight;
    [SerializeField] protected Image[] inventoryButtons;
    [SerializeField] protected Image inventoryHighlight;
    [SerializeField] protected Canvas canvas;
    [SerializeField] protected int buttonRowSize = 8;
    Material highlightMaterial;
    protected Material inventoryHighlightMaterial;
    protected TextMeshProUGUI[] itemTexts;
    protected TextMeshProUGUI[] inventoryItemTexts;
    protected Player player;
    protected int index;
    protected int inventoryIndex;
    protected bool isInventory;
    readonly protected int sliceWidthId = Shader.PropertyToID("_SliceWidth");
    protected ItemManager itemManager;
    protected virtual int MaxIndex => buttons.Length;
    protected int InventoryMaxIndex => inventoryButtons.Length;
    public virtual bool IsMakable => true;
    readonly protected int inventoryRowSize = 8;

    public enum Direction8
    {
        Right,
        UpRight,
        Up,
        UpLeft,
        Left,
        DownLeft,
        Down,
        DownRight
    }
    public enum SelectState
    {
        NoChange,
        UpOuterChange,
        DownOuterChange,
    }
    public static Direction8 ToDirection8(Vector2 input)
    {
        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        int index = Mathf.RoundToInt(angle / 45f) % 8;
        return (Direction8)index;
    }
    public virtual void Init(ItemManager itemManager) { }
    public virtual void OpenUI(Player player) { }
    public virtual void CloseUI() { }
    public virtual void Select(Vector2 vector) { }
    public virtual void Action() { }
    public virtual bool Cancel() => true;
    public virtual void UpdateAction() { }
    protected void InitBase(ItemManager itemManager)
    {
        canvas.enabled = false;
        this.itemManager = itemManager;
        highlightMaterial = new Material(highlight.material);
        highlight.material = highlightMaterial;
        inventoryHighlightMaterial = new Material(inventoryHighlight.material);
        inventoryHighlight.material = inventoryHighlightMaterial;
        if (buttons[0].GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            itemTexts = new TextMeshProUGUI[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                itemTexts[i] = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
                itemTexts[i].text = "";
            }
        }
        if (inventoryButtons[0].GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            inventoryItemTexts = new TextMeshProUGUI[inventoryButtons.Length];
            for (int i = 0; i < inventoryButtons.Length; i++)
            {
                inventoryItemTexts[i] = inventoryButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                inventoryItemTexts[i].text = "";
            }
        }
    }
    protected virtual void OpenUIBase(Player player)
    {
        this.player = player;
        index = 0;
        inventoryIndex = 0;
        isInventory = false;
        UpdateAction();
        _HighLight();
        _Cursor();
        canvas.enabled = true;
    }
    protected virtual void _Select(Vector2 vector)
    {
        var index = isInventory ? inventoryIndex : this.index;
        var MaxIndex = isInventory ? InventoryMaxIndex : this.MaxIndex;
        var rawSize = isInventory ? inventoryRowSize : buttonRowSize;
        if (vector.sqrMagnitude > 0.7f)
        {
            var derection = ToDirection8(vector);
            int preIndex = index;
            bool up = derection == Direction8.Up || derection == Direction8.UpRight || derection == Direction8.UpLeft;
            bool down = derection == Direction8.Down || derection == Direction8.DownRight || derection == Direction8.DownLeft;
            if (derection == Direction8.Right || derection == Direction8.UpRight || derection == Direction8.DownRight)
            {
                if ((index + 1) / rawSize > index / rawSize || index + 1 >= MaxIndex)
                {
                    preIndex = (index / rawSize) * rawSize;
                }
                else
                {
                    preIndex++;
                }
            }
            else if (derection == Direction8.Left || derection == Direction8.UpLeft || derection == Direction8.DownLeft)
            {
                if ((index - 1) / rawSize < index / rawSize || index <= 0)
                {
                    preIndex = index / rawSize + Mathf.Min(rawSize, MaxIndex) - 1;
                }
                else
                {
                    preIndex--;
                }
            }
            if (up)
            {
                if (preIndex + rawSize < MaxIndex)
                {
                    preIndex += rawSize;
                }
            }
            else if (down)
            {
                if (preIndex - rawSize >= 0)
                {
                    preIndex -= rawSize;
                }
            }
            if (isInventory)
            {
                inventoryIndex = preIndex;
            }
            else
            {
                this.index = preIndex;
            }
        }
    }
    protected virtual void _Cursor()
    {
        inventoryHighlight.transform.position = inventoryButtons[inventoryIndex].transform.position;
        highlight.transform.position = buttons[index].transform.position;
    }
    protected virtual void _HighLight()
    {
        Material stopMaterial = isInventory ? highlightMaterial : inventoryHighlightMaterial;
        Material startMaterial = isInventory ? inventoryHighlightMaterial : highlightMaterial;
        stopMaterial.SetFloat(sliceWidthId, 0.0f);
        startMaterial.SetFloat(sliceWidthId, 0.1f);
    }
    protected SelectState _GetSelect(Vector2 vector)
    {
        if (vector.sqrMagnitude <= 0.7f) return SelectState.NoChange;
        var index = isInventory ? inventoryIndex : this.index;
        var rawSize = isInventory ? inventoryRowSize : buttonRowSize;
        Image[] buttons = isInventory ? inventoryButtons : this.buttons;
        var derection = ToDirection8(vector);
        if (derection == Direction8.Right || derection == Direction8.Left)
        {
            return SelectState.NoChange;
        }
        else if (derection == Direction8.DownRight || derection == Direction8.Down || derection == Direction8.DownLeft)
        {
            if (index - rawSize >= 0)
            {
                return SelectState.NoChange;
            }
            return SelectState.DownOuterChange;
        }
        else
        {
            if (index + rawSize < buttons.Length)
            {
                return SelectState.NoChange;
            }
            return SelectState.UpOuterChange;
        }
    }
}
