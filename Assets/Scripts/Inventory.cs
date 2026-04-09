using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;

public class Inventory : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [SerializeField] Image[] inventoryButtons;
    [SerializeField] Image inventoryHighlight;
    [SerializeField] int openDuration = 1300;
    TextMeshProUGUI[] itemTexts;
    Material inventoryHighlightMaterial;
    Player player;
    int inventoryIndex;
    CancellationTokenSource cancellationTokenSource;
    readonly int sliceWidthId = Shader.PropertyToID("_SliceWidth");
    public int InventorySize => inventoryButtons.Length;
    /// <summary>
    /// インベントリの初期化を行う
    /// </summary>
    /// <param name="player"></param>
    public void Init(Player player)
    {
        this.player = player;
        itemTexts = new TextMeshProUGUI[inventoryButtons.Length];
        for (int i = 0; i < inventoryButtons.Length; i++)
        {
            itemTexts[i] = inventoryButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            itemTexts[i].text = "";
        }
        transform.parent = null;
        canvas.enabled = false;
        inventoryHighlightMaterial = new Material(inventoryHighlight.material);
        inventoryHighlight.material = inventoryHighlightMaterial;
        inventoryHighlightMaterial.SetFloat(sliceWidthId, 0.1f);
    }
    /// <summary>
    /// インベントリのUIを開き、選択したアイテムをプレイヤーのバッグに反映させる
    /// </summary>
    /// <param name="left"></param>
    public void SelectItem(bool left)
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = new CancellationTokenSource();
        _SelectItem(left, cancellationTokenSource.Token).Forget();
    }
    async UniTaskVoid _SelectItem(bool left, CancellationToken cancellationToken)
    {
        OpenUI();
        UpdateInventory();
        Select(left);
        player.BagIndex = inventoryIndex;
        await UniTask.Delay(openDuration, cancellationToken: cancellationToken);
        CloseUI();
    }
    /// <summary>
    /// インベントリのUIを更新する
    /// </summary>
    public void UpdateInventory()
    {
        if(!canvas.enabled) return;
        inventoryIndex = player.BagIndex;
        if (player.Bag.Length == 0)
        {
            inventoryHighlight.enabled = false;
        }
        else
        {
            inventoryHighlight.enabled = true;
        }
        for (int i = 0; i < InventorySize; i++)
        {
            if (player.Bag[i] != null)
            {
                inventoryButtons[i].sprite = player.Bag[i].ItemState.Icon;
                itemTexts[i].text = player.Bag[i].Num > 1 ? player.Bag[i].Num.ToString() : "";
            }
            else
            {
                inventoryButtons[i].sprite = null;
                itemTexts[i].text = "";
            }
        }
    }
    void OpenUI()
    {
        canvas.enabled = true;
        inventoryIndex = player.BagIndex;
    }
    void CloseUI()
    {
        canvas.enabled = false;
    }
    public void Select(bool left)
    {
        _Select(left);
        _Cursor();
    }
    void _Cursor()
    {
        if (player.Bag.Length != 0)
        {
            inventoryHighlight.transform.position = inventoryButtons[inventoryIndex].transform.position;
        }
    }
    void _Select(bool left)
    {
        if (left)
        {
            if (inventoryIndex <= 0)
            {
                inventoryIndex = player.Bag.Length - 1;
            }
            else
            {
                inventoryIndex--;
            }
        }
        else if (inventoryIndex + 1 >= player.Bag.Length)
        {
            inventoryIndex = 0;
        }
        else
        {
            inventoryIndex++;
        }
        if (player.Bag[inventoryIndex] == null)
        {
            _Select(left);
        }
    }
}
