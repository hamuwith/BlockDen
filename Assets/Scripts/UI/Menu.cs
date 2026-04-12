using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Item;

public class Menu : MonoBehaviour
{
    [SerializeField] Canvas menuCanvas;
    [SerializeField] Image[] menuImages;
    private TextMeshProUGUI[] menuTexts;
    ItemManager itemManager;
    public void Init(ItemManager itemManager)
    {
        this.itemManager = itemManager;
        menuTexts = new TextMeshProUGUI[menuImages.Length];
        for (int i = 0; i < menuImages.Length; i++)
        {
            menuTexts[i] = menuImages[i].GetComponentInChildren<TextMeshProUGUI>();
            menuTexts[i].text = "";
        }
        menuCanvas.enabled = false;
    }
    public void ShowMenu(Transform transform, ItemMaterial[] itemMaterials)
    {
        menuCanvas.enabled = true;
        this.transform.position = transform.position;
        for (int i = 0; i < itemMaterials.Length; i++)
        {
            menuTexts[i].text = itemMaterials[i].Num.ToString();
            menuImages[i].sprite = itemManager.GetItemIcon(itemMaterials[i].Category, itemMaterials[i].Id);
        }
    }
    public void HideMenu()
    {
        menuCanvas.enabled = false;
    }
}
