using TMPro;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class ToolUI : MonoBehaviour
{
    [SerializeField] protected Image[] buttons;
    [SerializeField] Image highlight;
    Material highlightMaterial;
    protected Canvas canvas;
    protected TextMeshProUGUI[] itemTexts;
    protected Player player;
    protected Tool tool;
    protected ItemManager itemManager;
    protected int index;
    protected virtual int MaxIndex => buttons.Length;

    readonly protected int sliceWidthId = Shader.PropertyToID("_SliceWidth");
    readonly int rawSize = 8;
    /// <summary>
    /// ベクトルの方向を8方向に変換するための列挙体
    /// </summary>
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
    /// <summary>
    /// ツールのUIで選択が変わったかを表す列挙体
    /// </summary>
    public enum SelectState
    {
        NoChange,
        UpOuterChange,
        DownOuterChange,
        SelectIn,
        SelectOut,
    }    
    /// <summary>
    /// ベクトルから8方向のどれに近いかを返すメソッド
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static Direction8 ToDirection8(Vector2 input)
    {
        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        int index = Mathf.RoundToInt(angle / 45f) % 8;

        return (Direction8)index;
    }
    public virtual void Init(ItemManager itemManager, Tool tool)
    {
        canvas = GetComponent<Canvas>();
        canvas.enabled = false;
        this.tool = tool;
        this.itemManager = itemManager;
        highlightMaterial = new Material(highlight.material);
        highlight.material = highlightMaterial;
        itemTexts = new TextMeshProUGUI[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            itemTexts[i] = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
            itemTexts[i].text = "";
        }
    }
    /// <summary>
    /// ツールのUIを開く際の初期化を行うメソッド
    /// </summary>
    /// <param name="player"></param>
    public virtual void OpenUI(Player player, int index)
    {
        canvas.enabled = true;
        this.index = index;
        this.player = player;
        _Cursor();
        UpdateAction();
    }
    /// <summary>
    /// ツールのUIで外から内に選択を移す際の初期化を行うメソッド
    /// </summary>
    public virtual void SelectIn(Item item = null)
    {
        var type = player.GetInventoryType(item);
        index = (int)type;
        if(type == Player.InventoryType.Null)
        {
            index = 0;
        }
        else if (type == Player.InventoryType.Tool)
        {
            player.ChangeTool((item as BreakTool).BlockType);
        }
        _HighLight(SelectState.SelectIn);
        _Cursor();
    }

    public virtual void CloseUI()
    {
        canvas.enabled = false;
        player.BagIndex = index;
    }
    /// <summary>
    /// ツールのアクションを実行するメソッド
    /// </summary>
    public virtual Item Action()
    {
        SelectOut();
        return player.Bag[index];
    }
    /// <summary>
    /// ツールのアクションを実行するメソッド
    /// </summary>
    public virtual void SelectOut()
    {
        SelectState change = SelectState.SelectOut;
        _HighLight(change);
    }
    /// <summary>
    /// ツールのUIでボタンを選択するメソッド
    /// </summary>
    /// <param name="vector"></param>
    public virtual SelectState Select(Vector2 vector)
    {
        var change = _GetSelect(vector);
        var isChange = tool.IsOuterChange(change);
        if (!isChange)
        {
            _Select(vector);
        }
        _HighLight(change, isChange);
        _Cursor();
        return change;
    }
    /// <summary>
    /// UIを更新する
    /// </summary>
    public virtual void UpdateAction()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (player.Bag[i] != null)
            {
                buttons[i].sprite = player.Bag[i].ItemState.Icon;
                itemTexts[i].text = player.Bag[i].Num > 1 ? player.Bag[i].Num.ToString() : "";
            }
            else
            {
                buttons[i].sprite = null;
                itemTexts[i].text = "";
            }
        }
    }
    protected void _Cursor()
    {
        highlight.transform.position = buttons[index].transform.position;
    }
    protected void _HighLight(SelectState selectState, bool isChange = true)
    {
        Material material = highlight.material;
        if (selectState == SelectState.NoChange) return;
        else if (selectState == SelectState.UpOuterChange || selectState == SelectState.DownOuterChange)
        {
            if (isChange)
            {
                material.SetFloat(sliceWidthId, 0.0f);
            }
        }
        else if (selectState == SelectState.SelectIn)
        {
            material.SetFloat(sliceWidthId, 0.1f);
        }
        else
        {
            material.SetFloat(sliceWidthId, 0f);
        }
    }
    protected void _Select(Vector2 vector)
    {
        if (vector.sqrMagnitude > 0.7f)
        {
            var derection = ToDirection8(vector);
            int preIndex = index;
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
            if (derection == Direction8.Up || derection == Direction8.UpRight || derection == Direction8.UpLeft)
            {
                if (preIndex + rawSize < MaxIndex)
                {
                    preIndex += rawSize;
                }
            }
            else if (derection == Direction8.Down || derection == Direction8.DownRight || derection == Direction8.DownLeft)
            {
                if (preIndex - rawSize >= 0)
                {
                    preIndex -= rawSize;
                }
            }
            index = preIndex;
        }
    }
    protected SelectState _GetSelect(Vector2 vector)
    {
        if (vector.sqrMagnitude <= 0.7f) return SelectState.NoChange;
        var derection = ToDirection8(vector);
        if (derection == Direction8.Right || derection == Direction8.Left)
        {
            return SelectState.NoChange;
        }
        else if (derection == Direction8.DownRight || derection == Direction8.Down || derection == Direction8.DownLeft)
        {
            if(index - rawSize >= 0)
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
