using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using static Player;

public class Inventory : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [SerializeField] Canvas bagCanvas;
    [SerializeField] Image[] inventoryButtons;
    [SerializeField] Image inventoryHighlight;
    [SerializeField] Image[] bagButtons;
    [SerializeField] int openDuration = 1300;
    TextMeshProUGUI[] itemTexts;
    TextMeshProUGUI[] bagItemTexts;
    Material inventoryHighlightMaterial;
    Player player;
    CancellationTokenSource cancellationTokenSource;
    ItemManager itemManager;
    readonly int sliceWidthId = Shader.PropertyToID("_SliceWidth");
    public int InventorySize => inventoryButtons.Length;
    public int BagSize => bagButtons.Length;
    /// <summary>
    /// インベントリの初期化を行う
    /// </summary>
    /// <param name="player"></param>
    public void Init(Player player, ItemManager itemManager)
    {
        this.player = player;
        this.itemManager = itemManager;
        itemTexts = new TextMeshProUGUI[inventoryButtons.Length];
        for (int i = 0; i < inventoryButtons.Length; i++)
        {
            itemTexts[i] = inventoryButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            itemTexts[i].text = "";
        }
        bagItemTexts = new TextMeshProUGUI[bagButtons.Length];
        for (int i = 0; i < bagButtons.Length; i++)
        {
            bagItemTexts[i] = bagButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            bagItemTexts[i].text = "";
        }
        transform.parent = null;
        canvas.enabled = false;
        bagCanvas.enabled = false;
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
        await UniTask.Delay(openDuration, cancellationToken: cancellationToken);
        CloseUI();
    }
    /// <summary>
    /// インベントリのUIを更新する
    /// </summary>
    public void UpdateInventory()
    {
        if(!canvas.enabled) return;
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
        for (int i = 0; i < BagSize; i++)
        {
            if (player.MaterialBag[i].Id != -1)
            {
                bagButtons[i].sprite = itemManager.GetItem(player.MaterialBag[i].Category, player.MaterialBag[i].Id).ItemState.Icon;
                bagItemTexts[i].text = player.MaterialBag[i].Num > 1 ? player.MaterialBag[i].Num.ToString() : "";
            }
            else
            {
                bagButtons[i].sprite = null;
                bagItemTexts[i].text = "";
            }
        }
    }
    void OpenUI()
    {
        canvas.enabled = true;
    }
    void CloseUI()
    {
        canvas.enabled = false;
        bagCanvas.enabled = false;
        if(player.BagIndex == (int)InventoryType.Bag)
        {
            player.BagIndex = 0;
        }
    }
    public void Select(bool left)
    {
        _Select(left);
        if(player.BagIndex == (int)InventoryType.Bag)
        {
            bagCanvas.enabled = true;
            inventoryHighlightMaterial.SetFloat(sliceWidthId, 0.0f);
        }
        else
        {
            bagCanvas.enabled = false;
            inventoryHighlightMaterial.SetFloat(sliceWidthId, 0.1f);
        }
        _Cursor();
    }
    void _Cursor()
    {
        if (player.Bag.Length != 0)
        {
            inventoryHighlight.transform.position = inventoryButtons[player.BagIndex].transform.position;
        }
    }
    void _Select(bool left)
    {
        if (left)
        {
            if (player.BagIndex <= 0)
            {
                player.BagIndex = player.Bag.Length - 1;
            }
            else
            {
                player.BagIndex--;
            }
        }
        else if (player.BagIndex >= player.Bag.Length - 1)
        {
            player.BagIndex = 0;
        }
        else
        {
            player.BagIndex++;
        }
        if (player.Bag[player.BagIndex] == null)
        {
            _Select(left);
        }
    }
}
