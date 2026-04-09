using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static ToolUI;

public class Attach : ToolUI
{
    Weapon weapon;
    public override void Init(ItemManager itemManager, Tool tool)
    {
        base.Init(itemManager, tool);
        weapon = tool as Weapon;
    }    
    /// <summary>
    /// ツールのUIを開く際の初期化を行うメソッド
    /// </summary>
    /// <param name="player"></param>
    public override void OpenUI(Player player, int index)
    {
        this.player = player;
        this.index = weapon.Attachments.Count;
        UpdateAction();
    }

    public override void CloseUI()
    {
    }
    /// <summary>
    /// ツールのUIでボタンを選択するメソッド
    /// </summary>
    /// <param name="vector"></param>
    public override SelectState Select(Vector2 vector)
    {
        var change = _GetSelect(vector);
        var isChange = tool.IsOuterChange(change);
        _HighLight(change, isChange);
        return change;
    }
    /// <summary>
    /// アタッチメントの装着状況に応じてUIを更新するメソッド
    /// </summary>
    public override void UpdateAction()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].color = i >= index ? Color.white : Color.gray;
        }
    }
}
