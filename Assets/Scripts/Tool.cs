using UnityEngine;
using static ToolUI;

public class Tool : Block
{
    [SerializeField] protected ToolUI[] tools;
    protected int toolIndex;
    protected Player player;
    Item makeItem;
    bool isMove;
    int tabIndex;
    public override void Init(ItemManager itemManager)
    {
        base.Init(itemManager);
        foreach (var tool in tools)
        {
            tool.Init(itemManager, this);
        }
    }
    /// <summary>
    /// ツールのUIを開く際の初期化を行うメソッド
    /// </summary>
    /// <param name="player"></param>
    public virtual void OpenUI(Player player)
    {
        tabIndex = 0;
        isMove = false;
        this.player = player;
        toolIndex = 0;
        tools[0].OpenUI(player, 0);
        tools[2].OpenUI(player, player.BagIndex);
        tools[0].SelectIn();
        tools[2].SelectOut();
    }
    /// <summary>
    /// ツールのUIを更新するメソッド
    /// </summary>
    public void UpdateAction()
    {
        foreach (var tool in tools)
        {
            tool.UpdateAction();
        }
    }
    /// <summary>
    /// ツールのUIを閉じるメソッド
    /// </summary>
    protected virtual void CloseUI()
    {
        foreach (var tool in tools)
        {
            tool.CloseUI();
        }
        player.CloseToolUI();
    }
    public virtual void Cancel()
    {
        if (tabIndex == 0)
        {
            if (toolIndex == 0)
            {
                CloseUI();
            }
            else
            {
                tools[toolIndex].SelectOut();
                toolIndex = 0;
                tools[toolIndex].SelectIn(null);
            }
        }
    }
    /// <summary>
    /// ツールのアクションを実行するメソッド
    /// </summary>
    public void Action()
    {
        if (tabIndex == 0)
        {
            if (toolIndex == 0)
            {
                makeItem = tools[toolIndex].Action();
                if (makeItem != null)
                {
                    toolIndex = 2;
                }
                tools[toolIndex].SelectIn(makeItem);
            }
            else
            {
                itemManager.RemoveBoxItem(makeItem.ItemState.Materials);
                player.Make(makeItem);
                makeItem = null;
                CloseUI();
            }
        }
        else
        {
            if (isMove)
            {
                var box = tools[toolIndex] as Box;
                Item item = box.GetItem();
                box.SetItem(player.HaveItem);
                player.ChangeItem(item, player.HaveItem);
                isMove = false;
            }
            else
            {
                var item = tools[toolIndex].Action();
                if(item == null) return;
                isMove = true;
                toolIndex = toolIndex == 2 ? tabIndex : 2;
                tools[toolIndex].SelectIn(item);
            }
        }
    }
    /// <summary>
    /// ツールのUIでボタンを選択するメソッド
    /// </summary>
    /// <param name="vector"></param>
    public void Select(Vector2 vector)
    {
        if (makeItem != null) return;
        var outerChange = SelectState.NoChange;
        if (isMove && toolIndex == 2)
        {
        }
        else
        {
            outerChange = tools[toolIndex].Select(vector);
        }
        if (isMove) return;
        if (IsOuterChange(outerChange))
        {
            tools[toolIndex].SelectOut();
            if (outerChange == SelectState.UpOuterChange)
            {
                toolIndex = tabIndex;
            }
            else if (outerChange == SelectState.DownOuterChange)
            {
                toolIndex = 2;
            }
            else
            {
                toolIndex = toolIndex == 2 ? tabIndex : 2;
            }
            tools[toolIndex].SelectIn();
        }
    }
    public virtual void ChangeTab()
    {
        if (tabIndex == 0 && makeItem == null)
        {
            tabIndex = 1;
            tools[0].CloseUI();
            toolIndex = 1;
            tools[toolIndex].OpenUI(player, 0);
        }
        else if (tabIndex == 1 && !isMove)
        {
            tabIndex = 0;
            tools[1].CloseUI();
            toolIndex = 0;
            tools[toolIndex].OpenUI(player, 0);
        }
    }
    public bool IsOuterChange(SelectState selectState)
    {
        if(selectState == SelectState.NoChange)
        {
            return false;
        }
        else if (selectState == SelectState.UpOuterChange && toolIndex == 0)
        {
            return false;
        }
        else if (selectState == SelectState.UpOuterChange && toolIndex == 1)
        {
            return false;
        }
        else if (selectState == SelectState.DownOuterChange && toolIndex == tools.Length - 1)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}
